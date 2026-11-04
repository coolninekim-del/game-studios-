namespace KnightOrder.Battle.Timing
{
    /// <summary>
    /// Result of the timing input for one charge impact.
    /// Implements: Story 002.
    /// </summary>
    public enum TimingOutcome
    {
        /// <summary>Tapped inside the judgement window: the success multiplier applies.</summary>
        Success,

        /// <summary>Tapped after the cue appeared but before the window opened: default impact.</summary>
        Early,

        /// <summary>No tap before the impact: default impact.</summary>
        Miss
    }
}
