using System;

namespace KnightOrder.Stable
{
    /// <summary>
    /// The protagonist's and the horse's stats: the one data model that the stable trains and that the battle
    /// (<see cref="KnightConversion.ToBattleUnit"/>) and the duel (<see cref="KnightConversion.ToDuelist"/>)
    /// both read.
    /// Implements: Story 005.
    ///
    /// Usage:
    /// <code>
    /// var profile = new KnightProfile { RiderAttack = 30, RiderHp = 100, HorseSpeed = 20, HorseHp = 40, HorseAgility = 10 };
    /// int speed = profile.Get(KnightStat.HorseSpeed);
    /// </code>
    /// </summary>
    public sealed class KnightProfile
    {
        /// <summary>The rider's attack.</summary>
        public int RiderAttack { get; set; }

        /// <summary>The rider's health.</summary>
        public int RiderHp { get; set; }

        /// <summary>The horse's speed.</summary>
        public int HorseSpeed { get; set; }

        /// <summary>The horse's health.</summary>
        public int HorseHp { get; set; }

        /// <summary>The horse's agility.</summary>
        public int HorseAgility { get; set; }

        /// <summary>Reads one stat.</summary>
        /// <param name="stat">The stat to read.</param>
        /// <returns>Its current value.</returns>
        public int Get(KnightStat stat)
        {
            switch (stat)
            {
                case KnightStat.RiderAttack: return RiderAttack;
                case KnightStat.RiderHp: return RiderHp;
                case KnightStat.HorseSpeed: return HorseSpeed;
                case KnightStat.HorseHp: return HorseHp;
                case KnightStat.HorseAgility: return HorseAgility;
                default: throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
            }
        }

        /// <summary>Writes one stat.</summary>
        /// <param name="stat">The stat to write.</param>
        /// <param name="value">Its new value.</param>
        public void Set(KnightStat stat, int value)
        {
            switch (stat)
            {
                case KnightStat.RiderAttack: RiderAttack = value; break;
                case KnightStat.RiderHp: RiderHp = value; break;
                case KnightStat.HorseSpeed: HorseSpeed = value; break;
                case KnightStat.HorseHp: HorseHp = value; break;
                case KnightStat.HorseAgility: HorseAgility = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
            }
        }

        /// <summary>An independent copy.</summary>
        /// <returns>A new profile with the same values.</returns>
        public KnightProfile Clone()
        {
            return new KnightProfile
            {
                RiderAttack = RiderAttack,
                RiderHp = RiderHp,
                HorseSpeed = HorseSpeed,
                HorseHp = HorseHp,
                HorseAgility = HorseAgility
            };
        }
    }
}
