namespace KnightOrder.Stable
{
    /// <summary>
    /// One training action offered in the stable: it raises one stat by a fixed amount.
    /// Implements: Story 005.
    /// </summary>
    public sealed class TrainingOption
    {
        /// <summary>The stat this training raises.</summary>
        public KnightStat Stat { get; set; }

        /// <summary>How much one training raises the stat.</summary>
        public int Gain { get; set; }
    }
}
