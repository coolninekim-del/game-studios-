namespace KnightOrder.Duel
{
    /// <summary>
    /// One mounted duelist: the rider and the horse. The boss reads fixed values from external data; the
    /// protagonist is filled from the trained knight profile (Story 005, KnightConversion.ToDuelist).
    /// Implements: Story 004 (protagonist and horse stats feed the duel numbers), Story 005 (horse agility).
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

        /// <summary>
        /// The horse's agility, which widens the protagonist's judgement window
        /// (see <see cref="DuelRules.AgilityWindowScale"/>). Not used for the boss.
        /// </summary>
        public int HorseAgility { get; set; }
    }
}
