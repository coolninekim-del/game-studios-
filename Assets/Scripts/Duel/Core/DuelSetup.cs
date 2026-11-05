using System.Collections.Generic;

namespace KnightOrder.Duel
{
    /// <summary>
    /// One duel: the protagonist, the boss, the boss's shield pattern and the reward for winning.
    /// Implements: Story 004.
    /// </summary>
    public sealed class DuelSetup
    {
        /// <summary>The protagonist and their horse.</summary>
        public DuelistStats Hero { get; set; } = new DuelistStats();

        /// <summary>The boss and its horse.</summary>
        public DuelistStats Boss { get; set; } = new DuelistStats();

        /// <summary>
        /// The side the boss's shield covers on each pass, repeated from the start when the list runs out.
        /// Fixed, so a player can learn a boss. Must not be empty.
        /// </summary>
        public List<LanceAim> BossGuardPattern { get; set; } = new List<LanceAim>();

        /// <summary>Growth points granted when the protagonist wins.</summary>
        public int VictoryGrowthReward { get; set; }
    }
}
