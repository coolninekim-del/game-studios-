using System;
using System.Collections.Generic;

namespace KnightOrder.Battle
{
    /// <summary>
    /// Tick-based, input-free cavalry-charge auto-battle on a logical grid.
    /// Pure C# (no UnityEngine); positions are grid cells, never screen coordinates.
    /// Implements: design/game-brief.md (MVP 3, auto-battle charge collision), Story 001, Story 008,
    /// Story 003 (enemy arrow volleys).
    ///
    /// Each tick: (1) impact cooldowns count down, (2) a retreating unit backs away from the enemy it
    /// just hit, while every other living unit that has no enemy within ImpactRange advances toward its
    /// nearest enemy, (3) every living, non-retreating unit whose cooldown is 0 and has an enemy within
    /// ImpactRange impacts it.
    /// One unit per tile: a cell holds at most one living unit. Moves are planned from the tick-start
    /// positions and then resolved together: a move into a cell that a staying unit holds, or that a
    /// lower-index unit also moves into, is cancelled (repeated until no cancellation is left), and two units
    /// moving into each other's cell both stay. A cancelled unit waits this tick; a cancelled retreat ends the
    /// retreat (the unit charges again). So two units closing on each other stop on adjacent cells and
    /// collide there. Setups must start every unit on its own cell. All impact events of a tick are raised before any damage is applied, and
    /// all damage is applied simultaneously. Damage uses the attacker's current troop count through
    /// (troops / ReferenceTroops) ^ TroopExponent.
    /// Charge cycle (Story 008): each impact adds 1 to the attacker's ChargesDone. While ChargesDone is
    /// below ChargesBeforeMelee the attacker then retreats DisengageDistance cells (it cannot attack
    /// meanwhile but can be hit) and charges again; if no retreat direction exists (same cell and same
    /// start cell as its target) it skips the retreat and charges again at once. Afterwards it no longer
    /// retreats: as in Story 001 it stays engaged while an enemy is within ImpactRange, impacting every
    /// ImpactIntervalTicks, and advances on the next nearest enemy when none is in range (for example
    /// after its target dies). OnChargeImpact is raised for every impact.
    /// Arrows (Story 003): after movement, every living enemy unit whose arrow cooldown is 0 fires a volley
    /// at its nearest living player unit when that unit is Charging and its distance is above ImpactRange
    /// and at most ArrowRange (ArrowRange 0 = no arrows). The volley kills floor(ArrowDamage *
    /// DamageMultiplier) troops and the archer waits ArrowIntervalTicks. OnArrowVolley is raised for every
    /// volley; volley and impact events of a tick are all raised before any damage, and both kinds of
    /// damage are applied together, so an arrow and a charge on the same tick are resolved independently.
    /// The simulation uses no randomness: nearest enemy = lowest Chebyshev distance, ties to the lowest
    /// Manhattan distance (so a unit prefers the enemy straight ahead in its own row), then to the lowest
    /// unit index (player units first, in setup order); a contested cell also goes to the lowest index. The
    /// same setup always gives the same result.
    /// The battle ends when a side reaches 0 troops or, as a safety cap, after MaxTicks ticks
    /// (then <see cref="BattleResult.ReachedTickCap"/> is set and the winner is None).
    ///
    /// Usage:
    /// <code>
    /// var sim = new BattleSimulation(rules, setup);
    /// sim.OnChargeImpact += (s, e) => e.PowerMultiplier = 1.5f;
    /// BattleResult result = sim.Run();
    /// </code>
    /// </summary>
    public sealed class BattleSimulation
    {
        private readonly BattleRules _rules;
        private readonly List<BattleUnit> _units = new List<BattleUnit>();
        private readonly List<BattleUnit> _impactAttackers = new List<BattleUnit>();
        private readonly List<BattleUnit> _impactDefenders = new List<BattleUnit>();
        private readonly List<int> _impactDamages = new List<int>();
        private readonly List<BattleUnit> _volleyArchers = new List<BattleUnit>();
        private readonly List<BattleUnit> _volleyTargets = new List<BattleUnit>();
        private readonly List<int> _volleyDamages = new List<int>();
        private readonly BattleUnit[] _targets;
        private readonly int[] _stepX;
        private readonly int[] _stepY;
        private readonly bool[] _moving;

        /// <summary>
        /// Creates a battle. The rules and setup are read once; later changes to them have no effect.
        /// </summary>
        /// <param name="rules">Tunable battle numbers loaded from external data.</param>
        /// <param name="setup">Both orders' units and their starting grid cells.</param>
        public BattleSimulation(BattleRules rules, BattleSetup setup)
        {
            _rules = rules;
            foreach (UnitSetup unit in setup.PlayerUnits)
            {
                _units.Add(new BattleUnit(BattleSide.Player, unit));
            }

            foreach (UnitSetup unit in setup.EnemyUnits)
            {
                _units.Add(new BattleUnit(BattleSide.Enemy, unit));
            }

            _targets = new BattleUnit[_units.Count];
            _stepX = new int[_units.Count];
            _stepY = new int[_units.Count];
            _moving = new bool[_units.Count];
        }

        private BattleSimulation(BattleSimulation other)
        {
            _rules = other._rules;
            foreach (BattleUnit unit in other._units)
            {
                _units.Add(new BattleUnit(unit));
            }

            _targets = new BattleUnit[_units.Count];
            _stepX = new int[_units.Count];
            _stepY = new int[_units.Count];
            _moving = new bool[_units.Count];
            Tick = other.Tick;
        }

        /// <summary>
        /// Copies the current state into an independent simulation with no event subscribers.
        /// Stepping the copy predicts this battle's next impacts (the simulation is deterministic);
        /// unit indices in <see cref="Units"/> are the same in both. Used by Story 002 timing input.
        /// </summary>
        /// <returns>A copy at the same tick that shares only the (read-only) rules.</returns>
        public BattleSimulation Clone()
        {
            return new BattleSimulation(this);
        }

        /// <summary>
        /// Raised when two units collide, BEFORE the impact's damage is resolved.
        /// Handlers may set <see cref="ChargeImpactEventArgs.PowerMultiplier"/>.
        /// The sender is this simulation.
        /// </summary>
        public event EventHandler<ChargeImpactEventArgs> OnChargeImpact;

        /// <summary>
        /// Raised when an enemy unit fires an arrow volley at a charging player unit, BEFORE the volley's
        /// damage is resolved. Handlers may set <see cref="ArrowVolleyEventArgs.DamageMultiplier"/>.
        /// The sender is this simulation.
        /// </summary>
        public event EventHandler<ArrowVolleyEventArgs> OnArrowVolley;

        /// <summary>Number of ticks simulated so far.</summary>
        public int Tick { get; private set; }

        /// <summary>All units, player units first. Read-only view for presentation.</summary>
        public IReadOnlyList<BattleUnit> Units => _units;

        /// <summary>
        /// True once either side has 0 troops left, or once <see cref="Tick"/> reaches
        /// <see cref="BattleRules.MaxTicks"/>. A Step()-driven caller should stop here.
        /// </summary>
        public bool IsFinished =>
            Tick >= _rules.MaxTicks
            || GetTroops(BattleSide.Player) == 0
            || GetTroops(BattleSide.Enemy) == 0;

        /// <summary>
        /// Runs the battle from the current state until it finishes, without any input.
        /// </summary>
        /// <returns>The winner and each side's remaining troops.</returns>
        public BattleResult Run()
        {
            while (!IsFinished)
            {
                Step();
            }

            return GetResult();
        }

        /// <summary>
        /// Builds the result from the current state. Meaningful once <see cref="IsFinished"/> is true.
        /// </summary>
        /// <returns>The winner and each side's remaining troops.</returns>
        public BattleResult GetResult()
        {
            int playerTroops = GetTroops(BattleSide.Player);
            int enemyTroops = GetTroops(BattleSide.Enemy);
            BattleWinner winner = BattleWinner.None;
            if (playerTroops > 0 && enemyTroops == 0)
            {
                winner = BattleWinner.Player;
            }
            else if (enemyTroops > 0 && playerTroops == 0)
            {
                winner = BattleWinner.Enemy;
            }

            bool reachedTickCap = Tick >= _rules.MaxTicks && playerTroops > 0 && enemyTroops > 0;
            return new BattleResult(winner, playerTroops, enemyTroops, reachedTickCap);
        }

        /// <summary>
        /// Advances the battle by one tick. Does nothing useful once the battle is finished.
        /// </summary>
        public void Step()
        {
            Tick++;

            foreach (BattleUnit unit in _units)
            {
                if (unit.CooldownTicks > 0)
                {
                    unit.CooldownTicks--;
                }

                if (unit.ArrowCooldownTicks > 0)
                {
                    unit.ArrowCooldownTicks--;
                }
            }

            // Moves are planned from the tick-start positions, resolved for tile occupancy and applied
            // together, so unit order only decides who gets a contested cell.
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit unit = _units[i];
                _stepX[i] = 0;
                _stepY[i] = 0;
                if (!unit.IsAlive)
                {
                    continue;
                }

                if (unit.Phase == UnitPhase.Disengaging)
                {
                    int cells = Math.Min(_rules.CellsPerTick, unit.RetreatCellsRemaining);
                    _stepX[i] = unit.RetreatDirX * cells;
                    _stepY[i] = unit.RetreatDirY * cells;
                    continue;
                }

                BattleUnit target = FindNearestEnemy(unit);
                if (target == null || Distance(unit, target) <= _rules.ImpactRange)
                {
                    continue;
                }

                _stepX[i] = Math.Clamp(target.X - unit.X, -_rules.CellsPerTick, _rules.CellsPerTick);
                _stepY[i] = Math.Clamp(target.Y - unit.Y, -_rules.CellsPerTick, _rules.CellsPerTick);
            }

            ResolveTileOccupancy();

            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit unit = _units[i];
                if (unit.IsAlive && unit.Phase == UnitPhase.Disengaging)
                {
                    int cells = Math.Max(Math.Abs(_stepX[i]), Math.Abs(_stepY[i]));
                    unit.RetreatCellsRemaining = cells == 0 ? 0 : unit.RetreatCellsRemaining - cells;
                    if (unit.RetreatCellsRemaining == 0)
                    {
                        unit.Phase = UnitPhase.Charging;
                    }
                }

                unit.X += _stepX[i];
                unit.Y += _stepY[i];
            }

            // Collect impacts from the post-movement positions.
            _impactAttackers.Clear();
            _impactDefenders.Clear();
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit unit = _units[i];
                if (!unit.IsAlive || unit.CooldownTicks > 0 || unit.Phase == UnitPhase.Disengaging)
                {
                    continue;
                }

                BattleUnit target = FindNearestEnemy(unit);
                if (target != null && Distance(unit, target) <= _rules.ImpactRange)
                {
                    _impactAttackers.Add(unit);
                    _impactDefenders.Add(target);
                }
            }

            // Collect arrow volleys from the same post-movement positions.
            _volleyArchers.Clear();
            _volleyTargets.Clear();
            if (_rules.ArrowRange > 0)
            {
                foreach (BattleUnit archer in _units)
                {
                    if (archer.Side != BattleSide.Enemy || !archer.IsAlive || archer.ArrowCooldownTicks > 0)
                    {
                        continue;
                    }

                    BattleUnit target = FindNearestEnemy(archer);
                    if (target == null || target.Phase != UnitPhase.Charging)
                    {
                        continue;
                    }

                    int distance = Distance(archer, target);
                    if (distance > _rules.ImpactRange && distance <= _rules.ArrowRange)
                    {
                        _volleyArchers.Add(archer);
                        _volleyTargets.Add(target);
                    }
                }
            }

            // Raise every event (damage not yet applied), then apply all damage together.
            _volleyDamages.Clear();
            for (int i = 0; i < _volleyArchers.Count; i++)
            {
                var volley = new ArrowVolleyEventArgs(_volleyArchers[i], _volleyTargets[i]);
                OnArrowVolley?.Invoke(this, volley);
                _volleyDamages.Add(Math.Max(0, (int)Math.Floor(_rules.ArrowDamage * volley.DamageMultiplier)));
            }

            _impactDamages.Clear();
            for (int i = 0; i < _impactAttackers.Count; i++)
            {
                BattleUnit attacker = _impactAttackers[i];
                var args = new ChargeImpactEventArgs(attacker, _impactDefenders[i]);
                OnChargeImpact?.Invoke(this, args);
                float troopMultiplier = (float)Math.Pow(attacker.Troops / _rules.ReferenceTroops, _rules.TroopExponent);
                _impactDamages.Add(CalculateDamage(
                    attacker.ChargePower * args.PowerMultiplier * troopMultiplier,
                    _impactDefenders[i].Defense));
            }

            for (int i = 0; i < _volleyArchers.Count; i++)
            {
                BattleUnit target = _volleyTargets[i];
                target.Troops = Math.Max(0, target.Troops - _volleyDamages[i]);
                _volleyArchers[i].ArrowCooldownTicks = _rules.ArrowIntervalTicks;
            }

            for (int i = 0; i < _impactAttackers.Count; i++)
            {
                BattleUnit attacker = _impactAttackers[i];
                BattleUnit defender = _impactDefenders[i];
                defender.Troops = Math.Max(0, defender.Troops - _impactDamages[i]);
                attacker.CooldownTicks = _rules.ImpactIntervalTicks;
                attacker.ChargesDone++;
                if (attacker.ChargesDone >= _rules.ChargesBeforeMelee)
                {
                    attacker.Phase = UnitPhase.Melee;
                    continue;
                }

                // Retreat direction, per axis: (a) away from the unit just hit; (b) on the same cell
                // there is no "away", so use own start cell minus the target's start cell; (c) if that is
                // also zero, there is no direction to retreat in, so skip the retreat and charge again.
                int retreatDirX = Math.Sign(attacker.X - defender.X);
                int retreatDirY = Math.Sign(attacker.Y - defender.Y);
                if (retreatDirX == 0 && retreatDirY == 0)
                {
                    retreatDirX = Math.Sign(attacker.StartX - defender.StartX);
                    retreatDirY = Math.Sign(attacker.StartY - defender.StartY);
                }

                if (retreatDirX == 0 && retreatDirY == 0)
                {
                    attacker.Phase = UnitPhase.Charging;
                    continue;
                }

                attacker.Phase = UnitPhase.Disengaging;
                attacker.RetreatDirX = retreatDirX;
                attacker.RetreatDirY = retreatDirY;
                attacker.RetreatCellsRemaining = _rules.DisengageDistance;
            }
        }

        /// <summary>
        /// Cancels planned moves (sets the step to 0) until no two living units would share a cell:
        /// a move into a staying unit's cell, into a cell a lower-index unit also moves into, or into the cell
        /// of a unit moving into this unit's cell is cancelled. Cancelling only ever adds staying units, so the
        /// loop ends after at most one pass per unit.
        /// </summary>
        private void ResolveTileOccupancy()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                _moving[i] = _units[i].IsAlive && (_stepX[i] != 0 || _stepY[i] != 0);
            }

            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < _units.Count; i++)
                {
                    if (!_moving[i])
                    {
                        continue;
                    }

                    BattleUnit unit = _units[i];
                    int destX = unit.X + _stepX[i];
                    int destY = unit.Y + _stepY[i];
                    for (int j = 0; j < _units.Count; j++)
                    {
                        BattleUnit other = _units[j];
                        if (j == i || !other.IsAlive)
                        {
                            continue;
                        }

                        int otherDestX = other.X + _stepX[j];
                        int otherDestY = other.Y + _stepY[j];
                        bool blocked = !_moving[j]
                            ? other.X == destX && other.Y == destY
                            : (otherDestX == destX && otherDestY == destY && j < i)
                              || (other.X == destX && other.Y == destY && otherDestX == unit.X && otherDestY == unit.Y);
                        if (blocked)
                        {
                            _moving[i] = false;
                            _stepX[i] = 0;
                            _stepY[i] = 0;
                            changed = true;
                            break;
                        }
                    }
                }
            }
        }

        private int CalculateDamage(float power, float defense)
        {
            float raw = power * _rules.PowerScale - defense * _rules.DefenseScale;
            return Math.Max(_rules.MinDamage, (int)Math.Floor(raw));
        }

        private BattleUnit FindNearestEnemy(BattleUnit unit)
        {
            BattleUnit nearest = null;
            int nearestDistance = 0;
            int nearestManhattan = 0;
            foreach (BattleUnit other in _units)
            {
                if (other.Side == unit.Side || !other.IsAlive)
                {
                    continue;
                }

                int distance = Distance(unit, other);
                int manhattan = Math.Abs(unit.X - other.X) + Math.Abs(unit.Y - other.Y);
                if (nearest == null || distance < nearestDistance
                    || (distance == nearestDistance && manhattan < nearestManhattan))
                {
                    nearest = other;
                    nearestDistance = distance;
                    nearestManhattan = manhattan;
                }
            }

            return nearest;
        }

        private static int Distance(BattleUnit a, BattleUnit b)
        {
            return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
        }

        private int GetTroops(BattleSide side)
        {
            int total = 0;
            foreach (BattleUnit unit in _units)
            {
                if (unit.Side == side)
                {
                    total += unit.Troops;
                }
            }

            return total;
        }
    }
}
