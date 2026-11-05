namespace KnightOrder.Battle
{
    /// <summary>
    /// Outcome of a finished battle.
    /// Implements: Story 001 (battle ends when one side's troops reach 0).
    /// </summary>
    public enum BattleWinner
    {
        /// <summary>Both sides reached 0 troops on the same tick.</summary>
        None,

        /// <summary>The player's side still has troops.</summary>
        Player,

        /// <summary>The enemy's side still has troops.</summary>
        Enemy
    }
}
