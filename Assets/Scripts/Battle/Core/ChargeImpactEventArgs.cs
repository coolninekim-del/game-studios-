using System;

namespace KnightOrder.Battle
{
    /// <summary>
    /// Data of a charge collision, raised before its damage is resolved.
    /// Handlers (Story 002 timing input) may change <see cref="PowerMultiplier"/>.
    /// Implements: Story 001 (OnChargeImpact).
    /// </summary>
    public sealed class ChargeImpactEventArgs : EventArgs
    {
        internal ChargeImpactEventArgs(BattleUnit attacker, BattleUnit defender)
        {
            Attacker = attacker;
            Defender = defender;
        }

        /// <summary>The unit whose charge power is being applied.</summary>
        public BattleUnit Attacker { get; }

        /// <summary>The unit receiving the damage.</summary>
        public BattleUnit Defender { get; }

        /// <summary>Multiplier on the attacker's charge power for this impact. Defaults to 1.0.</summary>
        public float PowerMultiplier { get; set; } = 1.0f;
    }
}
