namespace KnightOrder.Duel
{
    /// <summary>
    /// Result of a finished duel. Story 007 turns a defeat into the end of the run and a victory into growth.
    /// Implements: Story 004.
    /// </summary>
    public sealed class DuelResult
    {
        internal DuelResult(bool victory, int passesFought, int heroHp, int bossHp, int growthReward, bool reachedPassCap)
        {
            Victory = victory;
            PassesFought = passesFought;
            HeroHp = heroHp;
            BossHp = bossHp;
            GrowthReward = growthReward;
            ReachedPassCap = reachedPassCap;
        }

        /// <summary>
        /// True if the boss fell while the protagonist stayed up. Both falling on the same pass, or the pass
        /// cap, is a defeat.
        /// </summary>
        public bool Victory { get; }

        /// <summary>Number of passes resolved.</summary>
        public int PassesFought { get; }

        /// <summary>Protagonist health left (0 when they fell).</summary>
        public int HeroHp { get; }

        /// <summary>Boss health left (0 when it fell).</summary>
        public int BossHp { get; }

        /// <summary>Growth points earned: <see cref="DuelSetup.VictoryGrowthReward"/> on a victory, otherwise 0.</summary>
        public int GrowthReward { get; }

        /// <summary>True if the duel was stopped by <see cref="DuelRules.MaxPasses"/> with both riders up.</summary>
        public bool ReachedPassCap { get; }
    }
}
