namespace KnightOrder.Battle.Timing
{
    /// <summary>
    /// A predicted impact of a player unit that the timing input can still change.
    /// Read-only view for presentation; only <see cref="ChargeTimingController"/> mutates it.
    /// Implements: Story 002.
    /// </summary>
    public sealed class PendingImpact
    {
        internal PendingImpact(int attackerIndex, int impactTick, float impactTime, float windowSeconds, float cueLeadSeconds)
        {
            AttackerIndex = attackerIndex;
            ImpactTick = impactTick;
            ImpactTime = impactTime;
            WindowSeconds = windowSeconds;
            CueStartTime = impactTime - cueLeadSeconds;
        }

        /// <summary>Index of the attacker in <see cref="BattleSimulation.Units"/>.</summary>
        public int AttackerIndex { get; }

        /// <summary>Tick at which the impact resolves.</summary>
        public int ImpactTick { get; }

        /// <summary>Seconds at which the impact resolves (the end of the window).</summary>
        public float ImpactTime { get; }

        /// <summary>Width of this impact's window, after the window modifier hook.</summary>
        public float WindowSeconds { get; }

        /// <summary>Seconds at which the cue appears.</summary>
        public float CueStartTime { get; }

        /// <summary>Seconds at which the window opens.</summary>
        public float WindowStartTime => ImpactTime - WindowSeconds;

        /// <summary>Seconds of the first tap registered for this impact, or null.</summary>
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
