namespace KnightOrder.Battle
{
    /// <summary>
    /// Result of a finished battle: the winner and each side's remaining troops.
    /// Implements: Story 001.
    /// </summary>
    public sealed class BattleResult
    {
        internal BattleResult(BattleWinner winner, int playerTroops, int enemyTroops, bool reachedTickCap)
        {
            Winner = winner;
            PlayerTroops = playerTroops;
            EnemyTroops = enemyTroops;
            ReachedTickCap = reachedTickCap;
        }

        /// <summary>
        /// Which side won. None if both sides were wiped out on the same tick, or if the battle
        /// was stopped by the tick cap (see <see cref="ReachedTickCap"/>).
        /// </summary>
        public BattleWinner Winner { get; }

        /// <summary>Total troops the player's order has left.</summary>
        public int PlayerTroops { get; }

        /// <summary>Total troops the enemy order has left.</summary>
        public int EnemyTroops { get; }

        /// <summary>
        /// True if the battle hit <see cref="BattleRules.MaxTicks"/> with both sides still holding
        /// troops. Winner is then None. A side wiped out on the cap tick itself counts as a normal
        /// end (flag false).
        /// </summary>
        public bool ReachedTickCap { get; }
    }
}
