using System;
using System.Collections.Generic;

namespace KnightOrder.Battle.Timing
{
    /// <summary>
    /// Drives a <see cref="BattleSimulation"/> in real time and applies the timing input to the player's
    /// charge impacts (Story 002, through OnChargeImpact) and to the enemy arrow volleys at the player's
    /// units (Story 003, through OnArrowVolley). Pure C# (no UnityEngine) so the whole input rule is
    /// unit-testable; the Unity view only forwards taps and elapsed time.
    /// Implements: Story 002, Story 003.
    ///
    /// Time: tick T resolves at T * SecondsPerTick seconds. After every tick (and at construction) the
    /// controller steps a <see cref="BattleSimulation.Clone"/> ahead far enough to cover the longer cue lead
    /// and records each player-unit impact and each volley at a player unit it sees as a
    /// <see cref="PendingImpact"/>. The next tick's prediction is exact (the simulation is deterministic and
    /// the timing input changes damage, not movement); later ticks are re-predicted after every tick,
    /// keeping the tap of an event that is still predicted at the same tick and dropping events that no
    /// longer happen.
    ///
    /// Input: one tap input for both kinds. <see cref="Tap"/> gives the current time to the soonest
    /// untapped events whose cue is visible (every event of either kind resolving on that same tick, so one
    /// tap covers units hitting together; events of later ticks keep waiting for their own tap). A tap with
    /// no visible untapped cue is ignored. When the real event resolves, <see cref="ChargeTimingJudge"/>
    /// judges its first tap against its own window: a charge on Success has its PowerMultiplier multiplied
    /// by SuccessPowerMultiplier, an arrow volley on Success has its DamageMultiplier multiplied by
    /// ArrowSuccessDamageMultiplier; otherwise both are unchanged. Charges and arrows on the same tick are
    /// judged independently. Enemy charge impacts are not affected.
    ///
    /// Usage (one frame):
    /// <code>
    /// var controller = new ChargeTimingController(new BattleSimulation(rules, setup), timingRules);
    /// controller.OnImpactJudged += (s, e) =&gt; ShowFeedback(e.AttackerIndex, e.Outcome);
    /// controller.OnArrowJudged += (s, e) =&gt; ShowArrowFeedback(e.TargetIndex, e.Outcome);
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
        private readonly List<Prediction> _predictions = new List<Prediction>();
        private int[] _nextX;
        private int[] _nextY;
        private BattleSimulation _lookahead;

        /// <summary>Starts driving <paramref name="simulation"/> from its current tick.</summary>
        /// <param name="simulation">The battle; the controller subscribes to its OnChargeImpact and
        /// OnArrowVolley.</param>
        /// <param name="rules">Timing numbers from external data.</param>
        /// <param name="windowModifier">Optional charge window hook (see <see cref="WindowModifier"/>);
        /// passed here so it also applies to the impacts predicted during construction.</param>
        public ChargeTimingController(
            BattleSimulation simulation, ChargeTimingRules rules, IChargeWindowModifier windowModifier = null)
        {
            Simulation = simulation;
            _rules = rules;
            WindowModifier = windowModifier;
            float longestCueLead = Math.Max(rules.CueLeadSeconds, rules.ArrowCueLeadSeconds);
            // The small epsilon keeps float noise (1.2f / 0.4f = 3.0000002) from adding a tick.
            _lookaheadTicks = Math.Max(1, (int)Math.Ceiling(longestCueLead / rules.SecondsPerTick - 1e-4));
            _nextX = new int[simulation.Units.Count];
            _nextY = new int[simulation.Units.Count];
            Now = TimeOfTick(simulation.Tick);
            simulation.OnChargeImpact += HandleChargeImpact;
            simulation.OnArrowVolley += HandleArrowVolley;
            Repredict();
        }

        /// <summary>Raised for each player charge impact once judged, before its damage is applied.</summary>
        public event EventHandler<ImpactJudgedEventArgs> OnImpactJudged;

        /// <summary>Raised for each arrow volley at a player unit once judged, before its damage is applied.</summary>
        public event EventHandler<ArrowJudgedEventArgs> OnArrowJudged;

        /// <summary>Raised after every simulated tick (positions and troops updated).</summary>
        public event EventHandler OnTicked;

        /// <summary>The driven battle.</summary>
        public BattleSimulation Simulation { get; }

        /// <summary>
        /// Optional hook that widens or narrows the charge window per attacker (Story 005 horse stats).
        /// Read when an impact is first predicted, so a change affects impacts predicted afterwards only.
        /// Null = WindowSeconds from data. Arrow windows are not affected.
        /// </summary>
        public IChargeWindowModifier WindowModifier { get; set; }

        /// <summary>Current battle time in seconds.</summary>
        public float Now { get; private set; }

        /// <summary>Predicted charge impacts and arrow volleys not resolved yet, soonest tick first.</summary>
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

        /// <summary>The soonest pending event of either kind whose cue is visible now, or null.</summary>
        /// <returns>The event the on-screen timing bar should show.</returns>
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
        /// <returns>True if the tap was given to at least one event; false if no cue was visible.</returns>
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
            TimingOutcome outcome = JudgeAndRemove(TimingCueKind.Charge, attackerIndex);
            float multiplier = ChargeTimingJudge.PowerMultiplier(outcome, _rules.SuccessPowerMultiplier);
            e.PowerMultiplier *= multiplier;
            OnImpactJudged?.Invoke(this, new ImpactJudgedEventArgs(
                attackerIndex, IndexOf(Simulation.Units, e.Defender), outcome, multiplier));
        }

        private void HandleArrowVolley(object sender, ArrowVolleyEventArgs e)
        {
            if (e.Target.Side != BattleSide.Player)
            {
                return;
            }

            int archerIndex = IndexOf(Simulation.Units, e.Archer);
            TimingOutcome outcome = JudgeAndRemove(TimingCueKind.Arrow, archerIndex);
            float multiplier = ChargeTimingJudge.DamageMultiplier(outcome, _rules.ArrowSuccessDamageMultiplier);
            e.DamageMultiplier *= multiplier;
            OnArrowJudged?.Invoke(this, new ArrowJudgedEventArgs(
                archerIndex, IndexOf(Simulation.Units, e.Target), outcome, multiplier));
        }

        private TimingOutcome JudgeAndRemove(TimingCueKind kind, int attackerIndex)
        {
            for (int i = 0; i < _pending.Count; i++)
            {
                PendingImpact impact = _pending[i];
                if (impact.Kind == kind && impact.AttackerIndex == attackerIndex && impact.ImpactTick == Simulation.Tick)
                {
                    _pending.RemoveAt(i);
                    return ChargeTimingJudge.Judge(impact.ImpactTime, impact.WindowSeconds, impact.TapTime);
                }
            }

            return TimingOutcome.Miss;
        }

        private void Repredict()
        {
            _predictions.Clear();
            _lookahead = Simulation.Clone();
            _lookahead.OnChargeImpact += RecordPredictedImpact;
            _lookahead.OnArrowVolley += RecordPredictedVolley;
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
            foreach (Prediction prediction in _predictions)
            {
                PendingImpact kept = null;
                foreach (PendingImpact impact in _pending)
                {
                    if (impact.Kind == prediction.Kind && impact.AttackerIndex == prediction.AttackerIndex
                        && impact.ImpactTick == prediction.Tick)
                    {
                        kept = impact;
                        break;
                    }
                }

                _nextPending.Add(kept ?? Create(prediction));
            }

            _pending.Clear();
            _pending.AddRange(_nextPending);
        }

        private PendingImpact Create(Prediction prediction)
        {
            float cueLead;
            float window;
            if (prediction.Kind == TimingCueKind.Arrow)
            {
                cueLead = _rules.ArrowCueLeadSeconds;
                window = _rules.ArrowWindowSeconds;
            }
            else
            {
                cueLead = _rules.CueLeadSeconds;
                window = _rules.WindowSeconds;
                if (WindowModifier != null)
                {
                    window = WindowModifier.ModifyWindowSeconds(Simulation.Units[prediction.AttackerIndex], window);
                }
            }

            return new PendingImpact(
                prediction.Kind, prediction.AttackerIndex, prediction.TargetIndex, prediction.Tick,
                TimeOfTick(prediction.Tick), Math.Clamp(window, 0f, cueLead), cueLead);
        }

        private void RecordPredictedImpact(object sender, ChargeImpactEventArgs e)
        {
            if (e.Attacker.Side == BattleSide.Player)
            {
                _predictions.Add(new Prediction(
                    TimingCueKind.Charge, IndexOf(_lookahead.Units, e.Attacker), IndexOf(_lookahead.Units, e.Defender),
                    _lookahead.Tick));
            }
        }

        private void RecordPredictedVolley(object sender, ArrowVolleyEventArgs e)
        {
            if (e.Target.Side == BattleSide.Player)
            {
                _predictions.Add(new Prediction(
                    TimingCueKind.Arrow, IndexOf(_lookahead.Units, e.Archer), IndexOf(_lookahead.Units, e.Target),
                    _lookahead.Tick));
            }
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

        private readonly struct Prediction
        {
            public Prediction(TimingCueKind kind, int attackerIndex, int targetIndex, int tick)
            {
                Kind = kind;
                AttackerIndex = attackerIndex;
                TargetIndex = targetIndex;
                Tick = tick;
            }

            public TimingCueKind Kind { get; }

            public int AttackerIndex { get; }

            public int TargetIndex { get; }

            public int Tick { get; }
        }
    }
}
