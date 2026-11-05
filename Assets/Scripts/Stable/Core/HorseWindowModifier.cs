using KnightOrder.Battle;
using KnightOrder.Battle.Timing;

namespace KnightOrder.Stable
{
    /// <summary>
    /// Widens the charge judgement window of the player's mounted unit (the protagonist) by its horse's
    /// agility: window = base * (1 + HorseAgility * BattleAgilityWindowScale). Foot-soldier units keep the
    /// base window.
    /// Implements: Story 005 (connects the Story 002 window hook to horse stats).
    ///
    /// Usage:
    /// <code>
    /// controller.WindowModifier = new HorseWindowModifier(profile, stableRules);
    /// </code>
    /// </summary>
    public sealed class HorseWindowModifier : IChargeWindowModifier
    {
        private readonly float _factor;

        /// <summary>Creates the modifier for one profile.</summary>
        /// <param name="profile">The protagonist's profile (read once).</param>
        /// <param name="rules">Stable rules with the agility scale.</param>
        public HorseWindowModifier(KnightProfile profile, StableRules rules)
        {
            _factor = KnightConversion.BattleWindowFactor(profile, rules);
        }

        /// <inheritdoc/>
        public float ModifyWindowSeconds(BattleUnit attacker, float baseWindowSeconds)
        {
            return attacker.IsMounted && attacker.Side == BattleSide.Player ? baseWindowSeconds * _factor : baseWindowSeconds;
        }
    }
}
