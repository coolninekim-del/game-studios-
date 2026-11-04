using System;

namespace KnightOrder.Battle
{
    /// <summary>
    /// Data of an enemy arrow volley at a charging player unit, raised before its damage is resolved.
    /// Handlers (Story 003 timing input) may change <see cref="DamageMultiplier"/>.
    /// Implements: Story 003 (OnArrowVolley).
    /// </summary>
    public sealed class ArrowVolleyEventArgs : EventArgs
    {
        internal ArrowVolleyEventArgs(BattleUnit archer, BattleUnit target)
        {
            Archer = archer;
            Target = target;
        }

        /// <summary>The enemy unit firing the volley.</summary>
        public BattleUnit Archer { get; }

        /// <summary>The charging player unit the volley hits.</summary>
        public BattleUnit Target { get; }

        /// <summary>Multiplier on the volley's ArrowDamage for this volley. Defaults to 1.0.</summary>
        public float DamageMultiplier { get; set; } = 1.0f;
    }
}
