using System;
using System.Collections.Generic;

namespace KnightOrder.Battle
{
    /// <summary>
    /// Tick-based, input-free cavalry-charge auto-battle on a logical grid.
    /// Pure C# (no UnityEngine); positions are grid cells, never screen coordinates.
    /// Implements: design/game-brief.md (MVP 3, auto-battle charge collision), Story 001.
    ///
    /// Each tick: (1) impact cooldowns count down, (2) every living unit that has no enemy
    /// within ImpactRange advances toward its nearest enemy, (3) every living unit whose
    /// cooldown is 0 and has an enemy within ImpactRange impacts it. All impact events of a
    /// tick are raised before any damage is applied, and all damage is applied simultaneously.
    /// The simulation uses no randomness: nearest-enemy ties go to the lowest unit index
    /// (player units first, in setup order), so the same setup always gives the same result.
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
        private readonly BattleUnit[] _targets;
        private readonly int[] _stepX;
        private readonly int[] _stepY;

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
        }

        /// <summary>
        /// Raised when two units collide, BEFORE the impact's damage is resolved.
        /// Handlers may set <see cref="ChargeImpactEventArgs.PowerMultiplier"/>.
        /// The sender is this simulation.
        /// </summary>
        public event EventHandler<ChargeImpactEventArgs> OnChargeImpact;

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
            }

            // Movement is computed from the tick-start positions and applied together,
            // so unit order does not bias who closes the distance first.
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit unit = _units[i];
                _stepX[i] = 0;
                _stepY[i] = 0;
                if (!unit.IsAlive)
                {
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

            for (int i = 0; i < _units.Count; i++)
            {
                _units[i].X += _stepX[i];
                _units[i].Y += _stepY[i];
            }

            // Collect impacts from the post-movement positions.
            _impactAttackers.Clear();
            _impactDefenders.Clear();
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit unit = _units[i];
                if (!unit.IsAlive || unit.CooldownTicks > 0)
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

            // Raise every event (damage not yet applied), then apply all damage together.
            _impactDamages.Clear();
            for (int i = 0; i < _impactAttackers.Count; i++)
            {
                var args = new ChargeImpactEventArgs(_impactAttackers[i], _impactDefenders[i]);
                OnChargeImpact?.Invoke(this, args);
                _impactDamages.Add(CalculateDamage(
                    _impactAttackers[i].ChargePower * args.PowerMultiplier,
                    _impactDefenders[i].Defense));
            }

            for (int i = 0; i < _impactAttackers.Count; i++)
            {
                BattleUnit defender = _impactDefenders[i];
                defender.Troops = Math.Max(0, defender.Troops - _impactDamages[i]);
                _impactAttackers[i].CooldownTicks = _rules.ImpactIntervalTicks;
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
            foreach (BattleUnit other in _units)
            {
                if (other.Side == unit.Side || !other.IsAlive)
                {
                    continue;
                }

                int distance = Distance(unit, other);
                if (nearest == null || distance < nearestDistance)
                {
                    nearest = other;
                    nearestDistance = distance;
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
