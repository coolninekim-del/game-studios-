namespace KnightOrder.Duel
{
    /// <summary>
    /// One mounted duelist: the rider and the horse. Story 004 reads fixed test values from external data;
    /// Story 005 will fill these from training.
    /// Implements: Story 004 (protagonist and horse stats feed the duel numbers).
    /// </summary>
    public sealed class DuelistStats
    {
        /// <summary>The rider's attack, the base of lance power.</summary>
        public int RiderAttack { get; set; }

        /// <summary>The rider's health, part of duel health.</summary>
        public int RiderHp { get; set; }

        /// <summary>The horse's speed, which raises lance power (see <see cref="DuelRules.SpeedPowerScale"/>).</summary>
        public int HorseSpeed { get; set; }

        /// <summary>The horse's health, part of duel health.</summary>
        public int HorseHp { get; set; }
    }
}
