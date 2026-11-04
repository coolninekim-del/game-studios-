using System;
using System.Collections.Generic;

namespace KnightOrder.Battle.Timing
{
    /// <summary>
    /// Drives a <see cref="BattleSimulation"/> in real time and applies the charge timing input to the
    /// player's impacts through Story 001's OnChargeImpact. Pure C# (no UnityEngine) so the whole input
    /// rule is unit-testable; the Unity view only forwards taps and elapsed time.
    /// Implements: Story 002.
    ///
    /// Time: tick T resolves at T * SecondsPerTick seconds. After every tick (and at construction) the
    /// controller steps a <see cref="BattleSimulation.Clone"/> ahead far enough to cover CueLeadSeconds and
    /// records each player-unit impact it sees as a <see cref="PendingImpact"/>. The next tick's prediction
    /// is exact (the simulation is deterministic and the timing input changes damage, not movement);
    /// later ticks are re-predicted after every tick, keeping the tap of an impact that is still predicted
    /// at the same tick and dropping impacts that no longer happen.
    ///
    /// Input: <see cref="Tap"/> gives the current time to the soonest untapped impacts whose cue is visible
    /// (every impact resolving on that same tick, so one tap covers units hitting together; impacts of later
    /// ticks keep waiting for their own tap). A tap with no visible untapped cue is ignored.
    /// When the real impact resolves, <see cref="ChargeTimingJudge"/> judges the first tap and the
    /// impact's PowerMultiplier is multiplied by SuccessPowerMultiplier on Success, unchanged otherwise.
    /// Enemy impacts are not affected (arrow input is Story 003).
    ///
    /// Usage (one frame):
    /// <code>
    /// var controller = new ChargeTimingController(new BattleSimulation(rules, setup), timingRules);
    /// controller.OnImpactJudged += (s, e) =&gt; ShowFeedback(e.AttackerIndex, e.Outcome);
    /// if (tapPressedThisFrame) controller.Tap();
    /// controller.Advance(deltaTime);
    /// </code>
    /// Single-thread only.
    /// </summary>
    public sealed class ChargeTimingController
    {
        private readonly ChargeTimingRules _rules;
        private readonly int _lookaheadTicks;
        private readonly List<PendingImpact> _pending = new List<PendingImpact>();
        private readonly List<PendingImpact> _nextPending = new List<PendingImpact>();
        private readonly List<int> _predictedAttackers = new List<int>();
        private readonly List<int> _predictedTicks = new List<int>();
        private int[] _nextX;
        private int[] _nextY;
        private BattleSimulation _lookahead;

        /// <summary>Starts driving <paramref name="simulation"/> from its current tick.</summary>
        /// <param name="simulation">The battle; the controller subscribes to its OnChargeImpact.</param>
        /// <param name="rules">Timing numbers from external data.</param>
        /// <param name="windowModifier">Optional window hook (see <see cref="WindowModifier"/>); passed here
        /// so it also applies to the impacts predicted during construction.</param>
        public ChargeTimingController(
            BattleSimulation simulation, ChargeTimingRules rules, IChargeWindowModifier windowModifier = null)
        {
            Simulation = simulation;
            _rules = rules;
            WindowModifier = windowModifier;
            // The small epsilon keeps float noise (1.2f / 0.4f = 3.0000002) from adding a tick.
            _lookaheadTicks = Math.Max(1, (int)Math.Ceiling(rules.CueLeadSeconds / rules.SecondsPerTick - 1e-4));
            _nextX = new int[simulation.Units.Count];
            _nextY = new int[simulation.Units.Count];
            Now = TimeOfTick(simulation.Tick);
            simulation.OnChargeImpact += HandleChargeImpact;
            Repredict();
        }

        /// <summary>Raised for each player impact once judged, before its damage is applied.</summary>
        public event EventHandler<ImpactJudgedEventArgs> OnImpactJudged;

        /// <summary>Raised after every simulated tick (positions and troops updated).</summary>
        public event EventHandler OnTicked;

        /// <summary>The driven battle.</summary>
        public BattleSimulation Simulation { get; }

        /// <summary>
        /// Optional hook that widens or narrows the window per attacker (Story 005 horse stats).
        /// Read when an impact is first predicted, so a change affects impacts predicted afterwards only.
        /// Null = WindowSeconds from data.
        /// </summary>
        public IChargeWindowModifier WindowModifier { get; set; }

        /// <summary>Current battle time in seconds.</summary>
        public float Now { get; private set; }

        /// <summary>Predicted player impacts not resolved yet, soonest first.</summary>
        public IReadOnlyList<PendingImpact> Pending => _pending;

        /// <summary>Fraction [0, 1) of the way from the current tick to the next one.</summary>
        public float TickProgress =>
            Math.Clamp((Now - TimeOfTick(Simulation.Tick)) / _rules.SecondsPerTick, 0f, 1f);

        /// <summary>Grid column the unit will have after the next tick (exact; for smooth movement).</summary>
        /// <param name="unitIndex">Index in <see cref="BattleSimulation.Units"/>.</param>
        /// <returns>The predicted column.</returns>
        public int NextX(int unitIndex) => _nextX[unitIndex];

        /// <summary>Grid row the unit will have after the next tick; see <see cref="NextX"/>.</summary>
        /// <param name="unitIndex">Index in <see cref="BattleSimulation.Units"/>.</param>
        /// <returns>The predicted row.</returns>
        public int NextY(int unitIndex) => _nextY[unitIndex];

        /// <summary>The soonest pending impact whose cue is visible now, or null.</summary>
        /// <returns>The impact the on-screen timing bar should show.</returns>
        public PendingImpact GetActiveCue()
        {
            foreach (PendingImpact impact in _pending)
            {
                if (impact.IsCueVisible(Now))
                {
                    return impact;
                }
            }

            return null;
        }

        /// <summary>Registers a tap (touch or click) at <see cref="Now"/>.</summary>
        /// <returns>True if the tap was given to at least one impact; false if no cue was visible.</returns>
        public bool Tap()
        {
            int tick = -1;
            foreach (PendingImpact impact in _pending)
            {
                if (!impact.IsTapped && impact.IsCueVisible(Now) && (tick < 0 || impact.ImpactTick < tick))
                {
                    tick = impact.ImpactTick;
                }
            }

            if (tick < 0)
            {
                return false;
            }

            foreach (PendingImpact impact in _pending)
            {
                if (!impact.IsTapped && impact.ImpactTick == tick)
                {
                    impact.TapTime = Now;
                }
            }

            return true;
        }

        /// <summary>Advances real time, simulating every tick that falls due. Stops when the battle ends.</summary>
        /// <param name="deltaSeconds">Elapsed seconds; must be &gt;= 0.</param>
        public void Advance(float deltaSeconds)
        {
            if (Simulation.IsFinished)
            {
                return;
            }

            Now += deltaSeconds;
            while (!Simulation.IsFinished && Now >= TimeOfTick(Simulation.Tick + 1))
            {
                Simulation.Step();
                Repredict();
                OnTicked?.Invoke(this, EventArgs.Empty);
            }
        }

        private float TimeOfTick(int tick) => tick * _rules.SecondsPerTick;

        private void HandleChargeImpact(object sender, ChargeImpactEventArgs e)
        {
            if (e.Attacker.Side != BattleSide.Player)
            {
                return;
            }

            int attackerIndex = IndexOf(Simulation.Units, e.Attacker);
            TimingOutcome outcome = TimingOutcome.Miss;
            for (int i = 0; i < _pending.Count; i++)
            {
                PendingImpact impact = _pending[i];
                if (impact.AttackerIndex == attackerIndex && impact.ImpactTick == Simulation.Tick)
                {
                    outcome = ChargeTimingJudge.Judge(impact.ImpactTime, impact.WindowSeconds, impact.TapTime);
                    _pending.RemoveAt(i);
                    break;
                }
            }

            float multiplier = ChargeTimingJudge.PowerMultiplier(outcome, _rules.SuccessPowerMultiplier);
            e.PowerMultiplier *= multiplier;
            OnImpactJudged?.Invoke(this, new ImpactJudgedEventArgs(
                attackerIndex, IndexOf(Simulation.Units, e.Defender), outcome, multiplier));
        }

        private void Repredict()
        {
            _predictedAttackers.Clear();
            _predictedTicks.Clear();
            _lookahead = Simulation.Clone();
            _lookahead.OnChargeImpact += RecordPredictedImpact;
            for (int k = 0; k < _lookaheadTicks && !_lookahead.IsFinished; k++)
            {
                _lookahead.Step();
                if (k == 0)
                {
                    for (int i = 0; i < _lookahead.Units.Count; i++)
                    {
                        _nextX[i] = _lookahead.Units[i].X;
                        _nextY[i] = _lookahead.Units[i].Y;
                    }
                }
            }

            if (_lookahead.Tick == Simulation.Tick)
            {
                // Battle already finished: nothing moves.
                for (int i = 0; i < Simulation.Units.Count; i++)
                {
                    _nextX[i] = Simulation.Units[i].X;
                    _nextY[i] = Simulation.Units[i].Y;
                }
            }

            _lookahead = null;

            _nextPending.Clear();
            for (int p = 0; p < _predictedAttackers.Count; p++)
            {
                int attackerIndex = _predictedAttackers[p];
                int tick = _predictedTicks[p];
                PendingImpact kept = null;
                foreach (PendingImpact impact in _pending)
                {
                    if (impact.AttackerIndex == attackerIndex && impact.ImpactTick == tick)
                    {
                        kept = impact;
                        break;
                    }
                }

                _nextPending.Add(kept ?? new PendingImpact(
                    attackerIndex, tick, TimeOfTick(tick), WindowFor(attackerIndex), _rules.CueLeadSeconds));
            }

            _pending.Clear();
            _pending.AddRange(_nextPending);
        }

        private void RecordPredictedImpact(object sender, ChargeImpactEventArgs e)
        {
            if (e.Attacker.Side == BattleSide.Player)
            {
                _predictedAttackers.Add(IndexOf(_lookahead.Units, e.Attacker));
                _predictedTicks.Add(_lookahead.Tick);
            }
        }

        private float WindowFor(int attackerIndex)
        {
            float window = _rules.WindowSeconds;
            if (WindowModifier != null)
            {
                window = WindowModifier.ModifyWindowSeconds(Simulation.Units[attackerIndex], window);
            }

            return Math.Clamp(window, 0f, _rules.CueLeadSeconds);
        }

        private static int IndexOf(IReadOnlyList<BattleUnit> units, BattleUnit unit)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (ReferenceEquals(units[i], unit))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
