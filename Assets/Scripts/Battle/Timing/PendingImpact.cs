namespace KnightOrder.Battle.Timing
{
    /// <summary>
    /// A predicted timing event the input can still change: a player unit's charge impact (Story 002) or an
    /// enemy arrow volley at a player unit (Story 003).
    /// Read-only view for presentation; only <see cref="ChargeTimingController"/> mutates it.
    /// Implements: Story 002, Story 003.
    /// </summary>
    public sealed class PendingImpact
    {
        internal PendingImpact(
            TimingCueKind kind, int attackerIndex, int targetIndex, int impactTick, float impactTime,
            float windowSeconds, float cueLeadSeconds)
        {
            Kind = kind;
            AttackerIndex = attackerIndex;
            TargetIndex = targetIndex;
            ImpactTick = impactTick;
            ImpactTime = impactTime;
            WindowSeconds = windowSeconds;
            CueLeadSeconds = cueLeadSeconds;
            CueStartTime = impactTime - cueLeadSeconds;
        }

        /// <summary>Charge impact or arrow volley.</summary>
        public TimingCueKind Kind { get; }

        /// <summary>
        /// Index in <see cref="BattleSimulation.Units"/> of the unit acting: the charging player unit, or
        /// the enemy unit firing the arrows.
        /// </summary>
        public int AttackerIndex { get; }

        /// <summary>
        /// Index in <see cref="BattleSimulation.Units"/> of the unit receiving it: the enemy hit by the
        /// charge (as predicted), or the player unit the arrows fly at.
        /// </summary>
        public int TargetIndex { get; }

        /// <summary>Tick at which the impact or volley resolves.</summary>
        public int ImpactTick { get; }

        /// <summary>Seconds at which it resolves (the end of the window).</summary>
        public float ImpactTime { get; }

        /// <summary>Width of this event's window (for charges, after the window modifier hook).</summary>
        public float WindowSeconds { get; }

        /// <summary>Seconds between the cue appearing and the event resolving.</summary>
        public float CueLeadSeconds { get; }

        /// <summary>Seconds at which the cue appears.</summary>
        public float CueStartTime { get; }

        /// <summary>Seconds at which the window opens.</summary>
        public float WindowStartTime => ImpactTime - WindowSeconds;

        /// <summary>Seconds of the first tap registered for this event, or null.</summary>
        public float? TapTime { get; internal set; }

        /// <summary>True once a tap has been registered; later taps do not change it.</summary>
        public bool IsTapped => TapTime.HasValue;

        /// <summary>True when the cue should be visible at <paramref name="now"/>.</summary>
        /// <param name="now">Current battle time in seconds.</param>
        /// <returns>Whether now is in [CueStartTime, ImpactTime].</returns>
        public bool IsCueVisible(float now) => now >= CueStartTime && now <= ImpactTime;

        /// <summary>True when a tap at <paramref name="now"/> would be a Success.</summary>
        /// <param name="now">Current battle time in seconds.</param>
        /// <returns>Whether now is in [WindowStartTime, ImpactTime].</returns>
        public bool IsWindowOpen(float now) => now >= WindowStartTime && now <= ImpactTime;
    }
}
