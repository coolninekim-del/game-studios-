namespace KnightOrder.Stable
{
    /// <summary>
    /// One trainable stat of the protagonist and the horse.
    /// Implements: Story 005.
    /// </summary>
    public enum KnightStat
    {
        /// <summary>The rider's attack, the base of charge and lance power.</summary>
        RiderAttack,

        /// <summary>The rider's health, part of battle and duel health.</summary>
        RiderHp,

        /// <summary>The horse's speed, which raises charge and lance power.</summary>
        HorseSpeed,

        /// <summary>The horse's health, part of battle and duel health.</summary>
        HorseHp,

        /// <summary>The horse's agility, which widens the protagonist's judgement windows.</summary>
        HorseAgility
    }
}
