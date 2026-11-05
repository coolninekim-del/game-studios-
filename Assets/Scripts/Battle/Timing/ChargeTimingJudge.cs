namespace KnightOrder.Battle.Timing
{
    /// <summary>
    /// Pure judgement of one tap against one impact moment.
    /// Implements: Story 002 (inside the window = Success, outside or no input = default), Story 003 (the
    /// same judgement for arrows).
    ///
    /// Usage:
    /// <code>
    /// TimingOutcome outcome = ChargeTimingJudge.Judge(impactTime: 2.1f, windowSeconds: 0.25f, tapTime: 1.95f);
    /// float multiplier = ChargeTimingJudge.PowerMultiplier(outcome, rules.SuccessPowerMultiplier);
    /// </code>
    /// </summary>
    public static class ChargeTimingJudge
    {
        /// <summary>Judges a tap against the window [impactTime - windowSeconds, impactTime].</summary>
        /// <param name="impactTime">Seconds at which the impact resolves.</param>
        /// <param name="windowSeconds">Window width ending at the impact.</param>
        /// <param name="tapTime">Seconds of the first tap for this impact, or null if there was none.</param>
        /// <returns>Success inside the window (both edges included), Early before it, Miss without a tap.
        /// A tap after the impact cannot belong to it, so it is treated as no tap.</returns>
        public static TimingOutcome Judge(float impactTime, float windowSeconds, float? tapTime)
        {
            if (!tapTime.HasValue || tapTime.Value > impactTime)
            {
                return TimingOutcome.Miss;
            }

            return tapTime.Value >= impactTime - windowSeconds ? TimingOutcome.Success : TimingOutcome.Early;
        }

        /// <summary>Charge power multiplier for an outcome.</summary>
        /// <param name="outcome">The judged outcome.</param>
        /// <param name="successMultiplier">Multiplier from external data used on Success.</param>
        /// <returns><paramref name="successMultiplier"/> on Success, 1 (default impact) otherwise.</returns>
        public static float PowerMultiplier(TimingOutcome outcome, float successMultiplier)
        {
            return outcome == TimingOutcome.Success ? successMultiplier : 1.0f;
        }

        /// <summary>Arrow damage multiplier for an outcome.</summary>
        /// <param name="outcome">The judged outcome.</param>
        /// <param name="successMultiplier">Multiplier from external data used on Success.</param>
        /// <returns><paramref name="successMultiplier"/> on Success, 1 (full arrow damage) otherwise.</returns>
        public static float DamageMultiplier(TimingOutcome outcome, float successMultiplier)
        {
            return outcome == TimingOutcome.Success ? successMultiplier : 1.0f;
        }
    }
}
