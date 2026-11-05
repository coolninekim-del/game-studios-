namespace KnightOrder.Battle.Timing
{
    /// <summary>
    /// Tunable numbers of the charge timing input, loaded from external data
    /// (Assets/Data/Battle/battle_charge_timing.json).
    /// Implements: Story 002 (judgement window width and success multiplier are external data),
    /// Story 003 (arrow window, cue lead and damage multiplier; same time line with the Arrow* values).
    ///
    /// Time line of one predicted impact at time I (= impact tick * SecondsPerTick):
    ///   [I - CueLeadSeconds, I - window)  cue shown, a tap here is Early (fail)
    ///   [I - window, I]                   judgement window, a tap here is Success
    /// where window = WindowSeconds, optionally changed by an <see cref="IChargeWindowModifier"/>
    /// and clamped to [0, CueLeadSeconds].
    /// </summary>
    public sealed class ChargeTimingRules
    {
        /// <summary>Real-time seconds per battle tick while the battle is shown.</summary>
        public float SecondsPerTick { get; set; }

        /// <summary>Width in seconds of the judgement window that ends at the impact moment.</summary>
        public float WindowSeconds { get; set; }

        /// <summary>
        /// Seconds before the impact at which the cue appears. Taps after the cue appears but before the
        /// window opens count as Early, so tapping constantly does not succeed. Must be &gt;= WindowSeconds.
        /// </summary>
        public float CueLeadSeconds { get; set; }

        /// <summary>Charge power multiplier applied to the impact on a Success.</summary>
        public float SuccessPowerMultiplier { get; set; }

        /// <summary>Width in seconds of the arrow window that ends when the volley lands.</summary>
        public float ArrowWindowSeconds { get; set; }

        /// <summary>
        /// Seconds before the volley lands at which the arrow cue appears. Must be &gt;= ArrowWindowSeconds.
        /// </summary>
        public float ArrowCueLeadSeconds { get; set; }

        /// <summary>Multiplier on the volley's damage on a Success, in [0, 1] (0 = fully blocked).</summary>
        public float ArrowSuccessDamageMultiplier { get; set; }
    }
}
