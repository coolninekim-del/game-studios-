namespace KnightOrder.Battle.Timing
{
    /// <summary>
    /// Hook that changes the judgement window width per attacker, for example from horse stats.
    /// Implements: Story 002 (hook); connected in Story 005.
    ///
    /// Usage:
    /// <code>
    /// public sealed class HorseWindowModifier : IChargeWindowModifier
    /// {
    ///     public float ModifyWindowSeconds(BattleUnit attacker, float baseWindowSeconds)
    ///         =&gt; baseWindowSeconds * 1.2f;
    /// }
    /// controller.WindowModifier = new HorseWindowModifier();
    /// </code>
    /// </summary>
    public interface IChargeWindowModifier
    {
        /// <summary>Returns the window width to use for this attacker's next impact.</summary>
        /// <param name="attacker">The player unit about to impact.</param>
        /// <param name="baseWindowSeconds">The window width from external data.</param>
        /// <returns>Window width in seconds; clamped by the caller to [0, CueLeadSeconds].</returns>
        float ModifyWindowSeconds(BattleUnit attacker, float baseWindowSeconds);
    }
}
