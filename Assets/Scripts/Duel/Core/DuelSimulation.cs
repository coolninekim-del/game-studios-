using System;
using KnightOrder.Battle.Timing;

namespace KnightOrder.Duel
{
    /// <summary>
    /// The boss duel as a joust: the protagonist and the boss ride at each other in repeated passes. Each pass
    /// runs on a fixed timeline from its start time S:
    /// S + ApproachSeconds - GuardRevealSeconds: the boss's shield side for this pass is shown (from the
    /// fixed <see cref="DuelSetup.BossGuardPattern"/>) and taps start to count;
    /// impact = S + ApproachSeconds: the pass resolves, the tap judged by <see cref="ChargeTimingJudge"/>
    /// against the window [impact - HeroWindowSeconds, impact] (HeroWindowSeconds widens with horse agility);
    /// the next pass starts TurnaroundSeconds after the impact.
    /// The protagonist taps once per pass and picks an aim with it (High or Low); the first tap between the
    /// reveal and the impact counts, any other tap is ignored. Damage follows <see cref="DuelRules"/>; with no
    /// input the protagonist deals nothing and takes the boss's lance every pass, so an untouched duel is lost.
    /// The duel ends when either rider's health reaches 0 (both on the same pass = defeat) or after
    /// MaxPasses passes (defeat). No randomness: the same taps at the same times always give the same result.
    /// Pure C#, single-thread only; time advances only through <see cref="Advance"/>.
    /// Implements: Story 004, Story 005 (horse agility widens the window).
    ///
    /// Usage:
    /// <code>
    /// var duel = new DuelSimulation(rules, setup);
    /// duel.OnPassResolved += (s, e) => ShowPass(e);
    /// // every frame:
    /// if (tapped) duel.Tap(pointerInUpperHalf ? LanceAim.High : LanceAim.Low);
    /// duel.Advance(Time.deltaTime);
    /// if (duel.IsFinished) Show(duel.GetResult());
    /// </code>
    /// </summary>
    public sealed class DuelSimulation
    {
        private readonly DuelRules _rules;
        private readonly DuelSetup _setup;
        private float? _tapTime;
        private LanceAim? _tapAim;
        private bool _passResolved;
        private DuelResult _result;

        /// <summary>Creates a duel at time 0, at the start of the first pass.</summary>
        /// <param name="rules">Duel rules (validated by the loader).</param>
        /// <param name="setup">Both duelists and the boss's shield pattern (must not be empty).</param>
        public DuelSimulation(DuelRules rules, DuelSetup setup)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _setup = setup ?? throw new ArgumentNullException(nameof(setup));
            if (setup.BossGuardPattern == null || setup.BossGuardPattern.Count == 0)
            {
                throw new ArgumentException("the boss guard pattern must not be empty", nameof(setup));
            }

            HeroPower = LancePowerOf(rules, setup.Hero);
            BossPower = LancePowerOf(rules, setup.Boss);
            HeroMaxHp = setup.Hero.RiderHp + setup.Hero.HorseHp;
            BossMaxHp = setup.Boss.RiderHp + setup.Boss.HorseHp;
            HeroWindowSeconds = HeroWindowOf(rules, setup.Hero);
            HeroHp = HeroMaxHp;
            BossHp = BossMaxHp;
        }

        /// <summary>Raised when a pass resolves, after health has been reduced.</summary>
        public event EventHandler<PassResolvedEventArgs> OnPassResolved;

        /// <summary>Raised once when the duel ends, after the last <see cref="OnPassResolved"/>.</summary>
        public event EventHandler OnFinished;

        /// <summary>Seconds since the duel started.</summary>
        public float Now { get; private set; }

        /// <summary>0-based number of the current pass (the last resolved one once finished).</summary>
        public int PassIndex { get; private set; }

        /// <summary>Start time of the current pass.</summary>
        public float PassStartTime { get; private set; }

        /// <summary>Time at which the boss's shield side is shown and taps start to count.</summary>
        public float GuardRevealTime => ImpactTime - _rules.GuardRevealSeconds;

        /// <summary>Start of the judgement window.</summary>
        public float WindowStartTime => ImpactTime - HeroWindowSeconds;

        /// <summary>
        /// The protagonist's judgement window: min(GuardRevealSeconds, WindowSeconds * (1 + HorseAgility *
        /// AgilityWindowScale)).
        /// </summary>
        public float HeroWindowSeconds { get; }

        /// <summary>Time at which the current pass resolves.</summary>
        public float ImpactTime => PassStartTime + _rules.ApproachSeconds;

        /// <summary>Start time of the next pass.</summary>
        public float NextPassStartTime => ImpactTime + _rules.TurnaroundSeconds;

        /// <summary>True once the boss's shield side for the current pass is shown.</summary>
        public bool IsGuardRevealed => Now >= GuardRevealTime;

        /// <summary>True while a tap would be a Success.</summary>
        public bool IsWindowOpen => !IsFinished && !_passResolved && Now >= WindowStartTime;

        /// <summary>True between the current pass's impact and the start of the next pass (riders turning).</summary>
        public bool IsTurning => _passResolved;

        /// <summary>The side the boss's shield covers on the current pass (only show it once revealed).</summary>
        public LanceAim BossGuard => GuardFor(PassIndex);

        /// <summary>Where the protagonist aimed on the current pass, or null before a counted tap.</summary>
        public LanceAim? HeroAim => _tapAim;

        /// <summary>True once the current pass has a counted tap.</summary>
        public bool HasTapped => _tapTime.HasValue;

        /// <summary>Protagonist lance power: round(RiderAttack * (1 + HorseSpeed * SpeedPowerScale)).</summary>
        public int HeroPower { get; }

        /// <summary>Boss lance power, same formula.</summary>
        public int BossPower { get; }

        /// <summary>Protagonist duel health at the start: RiderHp + HorseHp.</summary>
        public int HeroMaxHp { get; }

        /// <summary>Boss duel health at the start: RiderHp + HorseHp.</summary>
        public int BossMaxHp { get; }

        /// <summary>Protagonist health left.</summary>
        public int HeroHp { get; private set; }

        /// <summary>Boss health left.</summary>
        public int BossHp { get; private set; }

        /// <summary>True once the duel has ended.</summary>
        public bool IsFinished => _result != null;

        /// <summary>The side the boss's shield covers on a pass.</summary>
        /// <param name="passIndex">0-based pass number.</param>
        /// <returns>The pattern entry for that pass, repeating from the start.</returns>
        public LanceAim GuardFor(int passIndex)
        {
            return _setup.BossGuardPattern[passIndex % _setup.BossGuardPattern.Count];
        }

        /// <summary>The protagonist taps now, aiming the lance.</summary>
        /// <param name="aim">High or Low.</param>
        /// <returns>True if the tap counts for the current pass; false if the duel is over, the shield is
        /// not shown yet, the impact has passed, or this pass already has a tap.</returns>
        public bool Tap(LanceAim aim)
        {
            if (IsFinished || _passResolved || _tapTime.HasValue || Now < GuardRevealTime)
            {
                return false;
            }

            _tapTime = Now;
            _tapAim = aim;
            return true;
        }

        /// <summary>Advances duel time, resolving every impact reached on the way.</summary>
        /// <param name="seconds">Seconds to advance (negative values are treated as 0).</param>
        public void Advance(float seconds)
        {
            if (IsFinished || seconds <= 0f)
            {
                return;
            }

            float target = Now + seconds;
            while (!IsFinished)
            {
                if (!_passResolved && target >= ImpactTime)
                {
                    Now = ImpactTime;
                    ResolvePass();
                }
                else if (_passResolved && target >= NextPassStartTime)
                {
                    StartNextPass();
                }
                else
                {
                    Now = target;
                    break;
                }
            }
        }

        /// <summary>The result of the finished duel.</summary>
        /// <exception cref="InvalidOperationException">The duel is still running.</exception>
        public DuelResult GetResult()
        {
            return _result ?? throw new InvalidOperationException("the duel is still running");
        }

        /// <summary>Lance power of a duelist: round(RiderAttack * (1 + HorseSpeed * SpeedPowerScale)).</summary>
        /// <param name="rules">Duel rules.</param>
        /// <param name="stats">The duelist.</param>
        /// <returns>The lance power used for every pass.</returns>
        public static int LancePowerOf(DuelRules rules, DuelistStats stats)
        {
            return (int)Math.Round(stats.RiderAttack * (1.0 + stats.HorseSpeed * (double)rules.SpeedPowerScale),
                MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// The protagonist's judgement window: min(GuardRevealSeconds, WindowSeconds * (1 + HorseAgility *
        /// AgilityWindowScale)).
        /// </summary>
        /// <param name="rules">Duel rules.</param>
        /// <param name="hero">The protagonist.</param>
        /// <returns>Window width in seconds.</returns>
        public static float HeroWindowOf(DuelRules rules, DuelistStats hero)
        {
            return Math.Min(rules.GuardRevealSeconds,
                rules.WindowSeconds * (1f + hero.HorseAgility * rules.AgilityWindowScale));
        }

        private void ResolvePass()
        {
            TimingOutcome outcome = ChargeTimingJudge.Judge(ImpactTime, HeroWindowSeconds, _tapTime);
            LanceAim guard = BossGuard;
            float multiplier = outcome == TimingOutcome.Success ? _rules.SuccessMultiplier
                : outcome == TimingOutcome.Early ? _rules.EarlyMultiplier : 0f;
            if (_tapAim.HasValue && _tapAim.Value == guard)
            {
                multiplier *= _rules.GuardedMultiplier;
            }

            int damageToBoss = Math.Max(0, (int)Math.Floor(HeroPower * (double)multiplier));
            int damageToHero = outcome == TimingOutcome.Success ? 0 : BossPower;
            BossHp = Math.Max(0, BossHp - damageToBoss);
            HeroHp = Math.Max(0, HeroHp - damageToHero);

            _passResolved = true;
            var args = new PassResolvedEventArgs(PassIndex, outcome, _tapAim, guard, damageToBoss, damageToHero);
            bool reachedCap = PassIndex + 1 >= _rules.MaxPasses;
            if (HeroHp == 0 || BossHp == 0 || reachedCap)
            {
                bool victory = HeroHp > 0 && BossHp == 0;
                _result = new DuelResult(victory, PassIndex + 1, HeroHp, BossHp,
                    victory ? _setup.VictoryGrowthReward : 0, HeroHp > 0 && BossHp > 0);
            }

            OnPassResolved?.Invoke(this, args);
            if (IsFinished)
            {
                OnFinished?.Invoke(this, EventArgs.Empty);
            }
        }

        private void StartNextPass()
        {
            Now = NextPassStartTime;
            PassStartTime = Now;
            PassIndex++;
            _passResolved = false;
            _tapTime = null;
            _tapAim = null;
        }
    }
}
