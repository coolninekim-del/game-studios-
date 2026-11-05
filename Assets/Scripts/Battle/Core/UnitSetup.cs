namespace KnightOrder.Battle
{
    /// <summary>
    /// Initial values of one unit. Positions are logical grid cells.
    /// Implements: Story 001, Story 005 (mounted protagonist).
    /// </summary>
    public sealed class UnitSetup
    {
        /// <summary>Starting troop count of the unit.</summary>
        public int Troops { get; set; }

        /// <summary>Charge power used as the attack value on impact.</summary>
        public float ChargePower { get; set; }

        /// <summary>Defense value subtracted from incoming charge power.</summary>
        public float Defense { get; set; }

        /// <summary>Starting grid column.</summary>
        public int StartX { get; set; }

        /// <summary>Starting grid row.</summary>
        public int StartY { get; set; }

        /// <summary>
        /// True for a single mounted rider (the protagonist, later heroes): Troops is its health and its
        /// impact damage does not scale with the troop count. False for a foot-soldier unit.
        /// </summary>
        public bool IsMounted { get; set; }
    }
}
