using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KnightOrder.Stable.Loading
{
    /// <summary>
    /// Unity boundary: parses and validates the stable JSON data (Assets/Data/Stable/). The caller supplies the
    /// JSON text. The private DTOs below are the snake_case wire format and map once into the pure-C# core types.
    ///
    /// Wire format of stable_rules.json: actions_per_visit (int &gt;= 1); trainings (non-empty list of
    /// { stat, gain }, stat one of "rider_attack", "rider_hp", "horse_speed", "horse_hp", "horse_agility", each
    /// stat at most once, gain int &gt;= 1); battle_speed_power_scale (float &gt;= 0); battle_agility_window_scale
    /// (float &gt;= 0); battle_defense (float &gt;= 0); battle_start_x, battle_start_y (int &gt;= 0, the mounted
    /// protagonist's start cell).
    /// Wire format of a profile file: rider_attack, rider_hp, horse_speed, horse_hp, horse_agility (int &gt;= 0)
    /// with rider_hp + horse_hp &gt;= 1.
    /// Implements: Story 005 (all stable numbers from external data).
    ///
    /// Usage:
    /// <code>
    /// StableRules rules = StableDataLoader.ParseRules(rulesAsset.text);
    /// KnightProfile start = StableDataLoader.ParseProfile(profileAsset.text);
    /// </code>
    /// </summary>
    public static class StableDataLoader
    {
        private static readonly Dictionary<string, KnightStat> StatNames = new Dictionary<string, KnightStat>
        {
            { "rider_attack", KnightStat.RiderAttack },
            { "rider_hp", KnightStat.RiderHp },
            { "horse_speed", KnightStat.HorseSpeed },
            { "horse_hp", KnightStat.HorseHp },
            { "horse_agility", KnightStat.HorseAgility }
        };

        [Serializable]
        private class TrainingWire
        {
            public string stat;
            public int gain;
        }

        [Serializable]
        private class RulesWire
        {
            public int actions_per_visit;
            public List<TrainingWire> trainings;
            public float battle_speed_power_scale;
            public float battle_agility_window_scale;
            public float battle_defense;
            public int battle_start_x;
            public int battle_start_y;
        }

        [Serializable]
        private class ProfileWire
        {
            public int rider_attack;
            public int rider_hp;
            public int horse_speed;
            public int horse_hp;
            public int horse_agility;
        }

        /// <summary>Parses stable_rules.json text.</summary>
        /// <param name="json">JSON text in the stable_rules.json wire format.</param>
        /// <returns>Validated rules.</returns>
        /// <exception cref="InvalidDataException">The text is empty/invalid or a value is out of range.</exception>
        public static StableRules ParseRules(string json)
        {
            RulesWire wire = FromJson<RulesWire>(json, "stable_rules");
            Require(wire.actions_per_visit >= 1, "actions_per_visit must be >= 1");
            Require(wire.trainings != null && wire.trainings.Count > 0, "trainings must contain at least one entry");
            Require(wire.battle_speed_power_scale >= 0f, "battle_speed_power_scale must be >= 0");
            Require(wire.battle_agility_window_scale >= 0f, "battle_agility_window_scale must be >= 0");
            Require(wire.battle_defense >= 0f, "battle_defense must be >= 0");
            Require(wire.battle_start_x >= 0 && wire.battle_start_y >= 0, "battle_start_x and battle_start_y must be >= 0");
            var rules = new StableRules
            {
                ActionsPerVisit = wire.actions_per_visit,
                BattleSpeedPowerScale = wire.battle_speed_power_scale,
                BattleAgilityWindowScale = wire.battle_agility_window_scale,
                BattleDefense = wire.battle_defense,
                BattleStartX = wire.battle_start_x,
                BattleStartY = wire.battle_start_y
            };
            for (int i = 0; i < wire.trainings.Count; i++)
            {
                TrainingWire training = wire.trainings[i];
                Require(training != null && training.stat != null && StatNames.ContainsKey(training.stat),
                    "trainings[" + i + "].stat must be one of rider_attack, rider_hp, horse_speed, horse_hp, horse_agility, got \""
                    + training?.stat + "\"");
                KnightStat stat = StatNames[training.stat];
                Require(rules.TrainingFor(stat) == null, "trainings[" + i + "]: " + training.stat + " is listed twice");
                Require(training.gain >= 1, "trainings[" + i + "].gain must be >= 1");
                rules.Trainings.Add(new TrainingOption { Stat = stat, Gain = training.gain });
            }

            return rules;
        }

        /// <summary>Parses a profile JSON text (see knight_start_profile.json).</summary>
        /// <param name="json">JSON text in the profile wire format.</param>
        /// <returns>Validated profile.</returns>
        /// <exception cref="InvalidDataException">The text is empty/invalid or a value is out of range.</exception>
        public static KnightProfile ParseProfile(string json)
        {
            ProfileWire wire = FromJson<ProfileWire>(json, "knight profile");
            Require(wire.rider_attack >= 0 && wire.rider_hp >= 0 && wire.horse_speed >= 0 && wire.horse_hp >= 0
                && wire.horse_agility >= 0, "knight profile values must be >= 0");
            Require(wire.rider_hp + wire.horse_hp >= 1, "knight profile: rider_hp + horse_hp must be >= 1");
            return new KnightProfile
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
                throw new InvalidDataException("Stable data: " + name + ": invalid JSON: " + e.Message, e);
            }

            Require(value != null, name + ": JSON did not produce data");
            return value;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidDataException("Stable data: " + message);
            }
        }
    }
}
