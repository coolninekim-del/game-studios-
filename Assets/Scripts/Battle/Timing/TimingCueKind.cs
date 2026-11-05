namespace KnightOrder.Battle.Timing
{
    /// <summary>
    /// What a timing cue is for. Both kinds share one tap input and are judged independently.
    /// Implements: Story 002 (charge), Story 003 (arrow).
    /// </summary>
    public enum TimingCueKind
    {
        /// <summary>A player unit's charge impact: Success multiplies its charge power.</summary>
        Charge,

        /// <summary>An enemy arrow volley at a charging player unit: Success reduces its damage.</summary>
        Arrow
    }
}
