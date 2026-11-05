using System;
using KnightOrder.Battle;
using KnightOrder.Battle.Timing;
using KnightOrder.Duel;

namespace KnightOrder.Stable
{
    /// <summary>
    /// Turns the trained profile into battle and duel values. The formulas' numbers come from external data
    /// (<see cref="StableRules"/> for the battle, <see cref="DuelRules"/> for the duel).
    /// Implements: Story 005 (horse and rider stats feed the next battle and duel).
    ///
    /// Usage:
    /// <code>
    /// KnightConversion.AddToBattle(battleSetup, profile, stableRules);
    /// duelSetup.Hero = KnightConversion.ToDuelist(profile);
    /// KnightNumbers numbers = KnightConversion.Numbers(profile, stableRules, timingRules, duelRules);
    /// </code>
    /// </summary>
    public static class KnightConversion
    {
        /// <summary>The protagonist as one mounted battle unit at the stable rules' start cell.</summary>
        /// <param name="profile">The protagonist's profile.</param>
        /// <param name="rules">Stable rules.</param>
        /// <returns>Charge power round(RiderAttack * (1 + HorseSpeed * BattleSpeedPowerScale)), troops
        /// RiderHp + HorseHp, defense BattleDefense, IsMounted.</returns>
        public static UnitSetup ToBattleUnit(KnightProfile profile, StableRules rules)
        {
            return new UnitSetup
            {
                Troops = profile.RiderHp + profile.HorseHp,
                ChargePower = (float)Math.Round(profile.RiderAttack * (1.0 + profile.HorseSpeed * (double)rules.BattleSpeedPowerScale),
                    MidpointRounding.AwayFromZero),
                Defense = rules.BattleDefense,
                StartX = rules.BattleStartX,
                StartY = rules.BattleStartY,
                IsMounted = true
            };
        }

        /// <summary>Adds the mounted protagonist to the player's side of a battle setup.</summary>
        /// <param name="setup">The battle setup; the protagonist becomes its last player unit.</param>
        /// <param name="profile">The protagonist's profile.</param>
        /// <param name="rules">Stable rules.</param>
        /// <exception cref="InvalidOperationException">Another unit already starts on the protagonist's cell.</exception>
        public static void AddToBattle(BattleSetup setup, KnightProfile profile, StableRules rules)
        {
            UnitSetup hero = ToBattleUnit(profile, rules);
            foreach (UnitSetup unit in setup.PlayerUnits)
            {
                ThrowIfSameCell(unit, hero);
            }

            foreach (UnitSetup unit in setup.EnemyUnits)
            {
                ThrowIfSameCell(unit, hero);
            }

            setup.PlayerUnits.Add(hero);
        }

        /// <summary>Factor on the base charge window: 1 + HorseAgility * BattleAgilityWindowScale.</summary>
        /// <param name="profile">The protagonist's profile.</param>
        /// <param name="rules">Stable rules.</param>
        /// <returns>The factor (1 when agility or the scale is 0).</returns>
        public static float BattleWindowFactor(KnightProfile profile, StableRules rules)
        {
            return 1f + profile.HorseAgility * rules.BattleAgilityWindowScale;
        }

        /// <summary>The protagonist as a duelist.</summary>
        /// <param name="profile">The protagonist's profile.</param>
        /// <returns>Duel stats with the same rider and horse values.</returns>
        public static DuelistStats ToDuelist(KnightProfile profile)
        {
            return new DuelistStats
            {
                RiderAttack = profile.RiderAttack,
                RiderHp = profile.RiderHp,
                HorseSpeed = profile.HorseSpeed,
                HorseHp = profile.HorseHp,
                HorseAgility = profile.HorseAgility
            };
        }

        /// <summary>Every battle and duel number the profile produces.</summary>
        /// <param name="profile">The protagonist's profile.</param>
        /// <param name="rules">Stable rules.</param>
        /// <param name="timing">Battle charge timing rules (base window and cue lead).</param>
        /// <param name="duel">Duel rules.</param>
        /// <returns>The numbers, computed with the same formulas the battle and duel use.</returns>
        public static KnightNumbers Numbers(KnightProfile profile, StableRules rules, ChargeTimingRules timing, DuelRules duel)
        {
            UnitSetup unit = ToBattleUnit(profile, rules);
            DuelistStats duelist = ToDuelist(profile);
            return new KnightNumbers
            {
                BattleChargePower = (int)unit.ChargePower,
                BattleHealth = unit.Troops,
                BattleWindowSeconds = Math.Clamp(timing.WindowSeconds * BattleWindowFactor(profile, rules), 0f, timing.CueLeadSeconds),
                DuelLancePower = DuelSimulation.LancePowerOf(duel, duelist),
                DuelHealth = duelist.RiderHp + duelist.HorseHp,
                DuelWindowSeconds = DuelSimulation.HeroWindowOf(duel, duelist)
            };
        }

        private static void ThrowIfSameCell(UnitSetup unit, UnitSetup hero)
        {
            if (unit.StartX == hero.StartX && unit.StartY == hero.StartY)
            {
                throw new InvalidOperationException("the protagonist's battle start cell (" + hero.StartX + ", "
                    + hero.StartY + ") is already taken");
            }
        }
    }
}
