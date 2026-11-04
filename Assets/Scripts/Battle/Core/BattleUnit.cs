namespace KnightOrder.Battle
{
    /// <summary>
    /// Where a unit is in the charge cycle: approach, impact, retreat, re-approach, melee.
    /// Implements: Story 008.
    /// </summary>
    public enum UnitPhase
    {
        /// <summary>Advancing on the nearest enemy and impacting when in range (a re-charge after a retreat).</summary>
        Charging,

        /// <summary>Retreating after an impact; the unit does not attack but can be hit.</summary>
        Disengaging,

        /// <summary>
        /// Done with its charges: no more retreats. Stays engaged while an enemy is within ImpactRange,
        /// impacting every ImpactIntervalTicks, and advances on the next nearest enemy when none is in range.
        /// </summary>
        Melee
    }

    /// <summary>
    /// Runtime state of one cavalry unit. Only the simulation mutates it.
    /// Implements: Story 001, Story 008.
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
            StartX = setup.StartX;
            StartY = setup.StartY;
        }

        /// <summary>Copies every field of <paramref name="other"/>, for <see cref="BattleSimulation.Clone"/>.</summary>
        internal BattleUnit(BattleUnit other)
        {
            Side = other.Side;
            Troops = other.Troops;
            ChargePower = other.ChargePower;
            Defense = other.Defense;
            X = other.X;
            Y = other.Y;
            StartX = other.StartX;
            StartY = other.StartY;
            Phase = other.Phase;
            ChargesDone = other.ChargesDone;
            CooldownTicks = other.CooldownTicks;
            RetreatDirX = other.RetreatDirX;
            RetreatDirY = other.RetreatDirY;
            RetreatCellsRemaining = other.RetreatCellsRemaining;
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

        /// <summary>Current step of the charge cycle.</summary>
        public UnitPhase Phase { get; internal set; }

        /// <summary>Number of impacts this unit has made as the attacker (re-charges and melee included).</summary>
        public int ChargesDone { get; internal set; }

        internal int CooldownTicks { get; set; }

        /// <summary>
        /// Starting grid column. When a unit sits on its target's cell, its retreat direction is
        /// sign(own start - target's start) per axis.
        /// </summary>
        internal int StartX { get; }

        /// <summary>Starting grid row; see <see cref="StartX"/>.</summary>
        internal int StartY { get; }

        internal int RetreatDirX { get; set; }

        internal int RetreatDirY { get; set; }

        internal int RetreatCellsRemaining { get; set; }
    }
}
