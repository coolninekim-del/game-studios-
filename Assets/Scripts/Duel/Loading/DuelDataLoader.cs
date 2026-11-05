using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KnightOrder.Duel.Loading
{
    /// <summary>
    /// Unity boundary: parses and validates the duel JSON data (Assets/Data/Duel/). The caller supplies the
    /// JSON text. The private DTOs below are the snake_case wire format and map once into the pure-C# core types.
    ///
    /// Wire format of duel_rules.json: approach_seconds (float &gt; 0, pass start to impact),
    /// guard_reveal_seconds (float, &gt; 0 and &lt;= approach_seconds, shield shown this long before the impact),
    /// window_seconds (float, &gt; 0 and &lt;= guard_reveal_seconds, judgement window ending at the impact),
    /// turnaround_seconds (float &gt;= 0, impact to next pass), speed_power_scale (float &gt;= 0),
    /// agility_window_scale (float &gt;= 0, optional, default 0; the protagonist's window grows by this fraction
    /// per point of horse agility and is capped at guard_reveal_seconds),
    /// success_multiplier (float &gt; 0), early_multiplier (float &gt;= 0), guarded_multiplier (float &gt;= 0),
    /// max_passes (int &gt;= 1).
    /// Wire format of a setup file: hero and boss, each { rider_attack (int &gt;= 0), rider_hp (int &gt;= 0),
    /// horse_speed (int &gt;= 0), horse_hp (int &gt;= 0), horse_agility (int &gt;= 0, optional, default 0) } with
    /// rider_hp + horse_hp &gt;= 1;
    /// boss_guard_pattern (non-empty list of "high" / "low"); victory_growth_reward (int &gt;= 0).
    /// Implements: Story 004 (all duel numbers from external data), Story 005 (agility).
    ///
    /// Usage:
    /// <code>
    /// DuelRules rules = DuelDataLoader.ParseRules(rulesAsset.text);
    /// DuelSetup setup = DuelDataLoader.ParseSetup(setupAsset.text);
    /// </code>
    /// </summary>
    public static class DuelDataLoader
    {
        [Serializable]
        private class RulesWire
        {
            public float approach_seconds;
            public float guard_reveal_seconds;
            public float window_seconds;
            public float turnaround_seconds;
            public float speed_power_scale;
            public float agility_window_scale;
            public float success_multiplier;
            public float early_multiplier;
            public float guarded_multiplier;
            public int max_passes;
        }

        [Serializable]
        private class DuelistWire
        {
            public int rider_attack;
            public int rider_hp;
            public int horse_speed;
            public int horse_hp;
            public int horse_agility;
        }

        [Serializable]
        private class SetupWire
        {
            public DuelistWire hero;
            public DuelistWire boss;
            public List<string> boss_guard_pattern;
            public int victory_growth_reward;
        }

        /// <summary>Parses duel_rules.json text.</summary>
        /// <param name="json">JSON text in the duel_rules.json wire format.</param>
        /// <returns>Validated rules.</returns>
        /// <exception cref="InvalidDataException">The text is empty/invalid or a value is out of range.</exception>
        public static DuelRules ParseRules(string json)
        {
            RulesWire wire = FromJson<RulesWire>(json, "duel_rules");
            Require(wire.approach_seconds > 0f, "approach_seconds must be > 0");
            Require(wire.guard_reveal_seconds > 0f && wire.guard_reveal_seconds <= wire.approach_seconds,
                "guard_reveal_seconds must be > 0 and <= approach_seconds (" + wire.approach_seconds + ")");
            Require(wire.window_seconds > 0f && wire.window_seconds <= wire.guard_reveal_seconds,
                "window_seconds must be > 0 and <= guard_reveal_seconds (" + wire.guard_reveal_seconds
                + "), the shield must be shown before the window opens");
            Require(wire.turnaround_seconds >= 0f, "turnaround_seconds must be >= 0");
            Require(wire.speed_power_scale >= 0f, "speed_power_scale must be >= 0");
            Require(wire.agility_window_scale >= 0f, "agility_window_scale must be >= 0");
            Require(wire.success_multiplier > 0f, "success_multiplier must be > 0 (a perfect strike must hurt)");
            Require(wire.early_multiplier >= 0f, "early_multiplier must be >= 0");
            Require(wire.guarded_multiplier >= 0f, "guarded_multiplier must be >= 0");
            Require(wire.max_passes >= 1, "max_passes must be >= 1");
            return new DuelRules
            {
                ApproachSeconds = wire.approach_seconds,
                GuardRevealSeconds = wire.guard_reveal_seconds,
                WindowSeconds = wire.window_seconds,
                TurnaroundSeconds = wire.turnaround_seconds,
                SpeedPowerScale = wire.speed_power_scale,
                AgilityWindowScale = wire.agility_window_scale,
                SuccessMultiplier = wire.success_multiplier,
                EarlyMultiplier = wire.early_multiplier,
                GuardedMultiplier = wire.guarded_multiplier,
                MaxPasses = wire.max_passes
            };
        }

        /// <summary>Parses a duel setup JSON text (see duel_sample_setup.json).</summary>
        /// <param name="json">JSON text in the setup wire format.</param>
        /// <returns>Validated setup.</returns>
        /// <exception cref="InvalidDataException">The text is empty/invalid or a value is out of range.</exception>
        public static DuelSetup ParseSetup(string json)
        {
            SetupWire wire = FromJson<SetupWire>(json, "duel setup");
            Require(wire.boss_guard_pattern != null && wire.boss_guard_pattern.Count > 0,
                "boss_guard_pattern must contain at least one entry");
            var pattern = new List<LanceAim>(wire.boss_guard_pattern.Count);
            for (int i = 0; i < wire.boss_guard_pattern.Count; i++)
            {
                string entry = wire.boss_guard_pattern[i];
                Require(entry == "high" || entry == "low", "boss_guard_pattern[" + i + "] must be \"high\" or \"low\", got \"" + entry + "\"");
                pattern.Add(entry == "high" ? LanceAim.High : LanceAim.Low);
            }

            Require(wire.victory_growth_reward >= 0, "victory_growth_reward must be >= 0");
            return new DuelSetup
            {
                Hero = ConvertDuelist(wire.hero, "hero"),
                Boss = ConvertDuelist(wire.boss, "boss"),
                BossGuardPattern = pattern,
                VictoryGrowthReward = wire.victory_growth_reward
            };
        }

        private static DuelistStats ConvertDuelist(DuelistWire wire, string name)
        {
            Require(wire != null, name + " is missing");
            Require(wire.rider_attack >= 0, name + ".rider_attack must be >= 0");
            Require(wire.rider_hp >= 0, name + ".rider_hp must be >= 0");
            Require(wire.horse_speed >= 0, name + ".horse_speed must be >= 0");
            Require(wire.horse_hp >= 0, name + ".horse_hp must be >= 0");
            Require(wire.horse_agility >= 0, name + ".horse_agility must be >= 0");
            Require(wire.rider_hp + wire.horse_hp >= 1, name + ": rider_hp + horse_hp must be >= 1");
            return new DuelistStats
            {
                RiderAttack = wire.rider_attack,
                RiderHp = wire.rider_hp,
                HorseSpeed = wire.horse_speed,
                HorseHp = wire.horse_hp,
                HorseAgility = wire.horse_agility
            };
        }

        private static T FromJson<T>(string json, string name) where T : class
        {
            Require(!string.IsNullOrWhiteSpace(json), name + ": JSON text is empty");
            T value;
            try
            {
                value = JsonUtility.FromJson<T>(json);
            }
            catch (Exception e) when (!(e is InvalidDataException))
            {
                throw new InvalidDataException("Duel data: " + name + ": invalid JSON: " + e.Message, e);
            }

            Require(value != null, name + ": JSON did not produce data");
            return value;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidDataException("Duel data: " + message);
            }
        }
    }
}
