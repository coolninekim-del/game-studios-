namespace KnightOrder.Battle
{
    /// <summary>
    /// Runtime state of one cavalry unit. Only the simulation mutates it.
    /// Implements: Story 001.
    /// </summary>
    public sealed class BattleUnit
    {
        internal BattleUnit(BattleSide side, UnitSetup setup)
        {
            Side = side;
            Troops = setup.Troops;
            ChargePower = setup.ChargePower;
            Defense = setup.Defense;
            X = setup.StartX;
            Y = setup.StartY;
        }

        /// <summary>The order this unit belongs to.</summary>
        public BattleSide Side { get; }

        /// <summary>Remaining troops; the unit is removed from the fight at 0.</summary>
        public int Troops { get; internal set; }

        /// <summary>Charge power used as the attack value on impact.</summary>
        public float ChargePower { get; }

        /// <summary>Defense value subtracted from incoming charge power.</summary>
        public float Defense { get; }

        /// <summary>Current grid column.</summary>
        public int X { get; internal set; }

        /// <summary>Current grid row.</summary>
        public int Y { get; internal set; }

        /// <summary>True while the unit still has troops.</summary>
        public bool IsAlive => Troops > 0;

        internal int CooldownTicks { get; set; }
    }
}
