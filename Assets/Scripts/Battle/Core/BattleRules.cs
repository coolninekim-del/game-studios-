namespace KnightOrder.Battle
{
    /// <summary>
    /// Every tunable number of the auto-battle, loaded from external data
    /// (Assets/Data/Battle/battle_rules.json).
    /// Implements: Story 001 (no hardcoded gameplay values), Story 008 (troop scaling, recharge cycle),
    /// Story 003 (enemy arrow volleys).
    ///
    /// Impact damage (troops lost by the defender) =
    ///   max(MinDamage, floor(AttackPower * PowerScale - Defense * DefenseScale)), where
    ///   AttackPower = ChargePower * PowerMultiplier * (AttackerTroops / ReferenceTroops) ^ TroopExponent.
    /// Arrow damage (troops lost by the target) = floor(ArrowDamage * DamageMultiplier).
    /// </summary>
    public sealed class BattleRules
    {
        /// <summary>Multiplier applied to the attacker's (modified) charge power.</summary>
        public float PowerScale { get; set; }

        /// <summary>Multiplier applied to the defender's defense.</summary>
        public float DefenseScale { get; set; }

        /// <summary>Lower bound of damage per impact, so every impact makes progress.</summary>
        public int MinDamage { get; set; }

        /// <summary>Grid cells a charging unit advances per axis per tick.</summary>
        public int CellsPerTick { get; set; }

        /// <summary>Chebyshev grid distance at or below which two units collide.</summary>
        public int ImpactRange { get; set; }

        /// <summary>Ticks a unit waits after an impact before it can impact again.</summary>
        public int ImpactIntervalTicks { get; set; }

        /// <summary>
        /// Safety cap: the battle stops after this many ticks even if both sides still have troops.
        /// </summary>
        public int MaxTicks { get; set; }

        /// <summary>
        /// Troop count at which the troop multiplier is exactly 1. The multiplier is
        /// (attacker troops / ReferenceTroops) ^ TroopExponent. Must be &gt; 0.
        /// </summary>
        public float ReferenceTroops { get; set; }

        /// <summary>
        /// Shape of the troop multiplier curve. 0 = multiplier always 1 (no troop effect),
        /// 1 = linear, between 0 and 1 = diminishing returns. Must be &gt;= 0.
        /// </summary>
        public float TroopExponent { get; set; }

        /// <summary>
        /// Impacts a unit makes with a retreat after each one (re-charge); once a unit has made this
        /// many impacts it no longer retreats and fights in melee (Story 001 behavior). 1 = melee after the
        /// first impact.
        /// </summary>
        public int ChargesBeforeMelee { get; set; }

        /// <summary>Grid cells a unit retreats after an impact before charging again.</summary>
        public int DisengageDistance { get; set; }

        /// <summary>
        /// Chebyshev grid distance at or below which an enemy unit fires an arrow volley at its nearest
        /// charging player unit (only while that unit is still beyond ImpactRange). 0 = no arrows;
        /// otherwise must be &gt; ImpactRange.
        /// </summary>
        public int ArrowRange { get; set; }

        /// <summary>Troops a volley kills when not defended (DamageMultiplier 1). Must be &gt;= 0.</summary>
        public int ArrowDamage { get; set; }

        /// <summary>Ticks an enemy unit waits after a volley before it can fire again. Must be &gt;= 1.</summary>
        public int ArrowIntervalTicks { get; set; }
    }
}
