namespace KnightOrder.Battle
{
    /// <summary>
    /// Every tunable number of the auto-battle, loaded from external data
    /// (Assets/Data/Battle/battle_rules.json).
    /// Implements: Story 001 (no hardcoded gameplay values).
    ///
    /// Impact damage (troops lost by the defender) =
    ///   max(MinDamage, floor(ChargePower * PowerMultiplier * PowerScale - Defense * DefenseScale)).
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
    }
}
