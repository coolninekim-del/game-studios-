namespace KnightOrder.Stable
{
    /// <summary>
    /// What one training changed: the stat and the whole profile before and after, so the stable can show the
    /// growth of both the stat and the battle and duel numbers it feeds.
    /// Implements: Story 005.
    /// </summary>
    public sealed class TrainingResult
    {
        internal TrainingResult(KnightStat stat, KnightProfile before, KnightProfile after)
        {
            Stat = stat;
            Before = before;
            After = after;
        }

        /// <summary>The trained stat.</summary>
        public KnightStat Stat { get; }

        /// <summary>A copy of the profile before the training.</summary>
        public KnightProfile Before { get; }

        /// <summary>A copy of the profile after the training.</summary>
        public KnightProfile After { get; }

        /// <summary>The stat's value before the training.</summary>
        public int StatBefore => Before.Get(Stat);

        /// <summary>The stat's value after the training.</summary>
        public int StatAfter => After.Get(Stat);
    }
}
