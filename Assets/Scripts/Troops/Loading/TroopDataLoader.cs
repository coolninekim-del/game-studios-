using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KnightOrder.Troops.Loading
{
    /// <summary>
    /// Unity boundary: parses and validates the troop JSON data (Assets/Data/Troops/). The caller supplies the
    /// JSON text. The private DTOs below are the snake_case wire format and map once into the pure-C# core types.
    ///
    /// Wire format of troops_rules.json: max_troops_per_squad (int &gt;= 1); reinforce_gold_per_troop (int &gt;= 0);
    /// promote_gold_costs (list of int &gt;= 0, cost to leave grade 1, 2, ...; may be empty = no promotion);
    /// power_per_grade (float &gt;= 0, fraction of the base charge power per grade above 1); defense_per_grade
    /// (float &gt;= 0).
    /// Wire format of a roster file: gold (int &gt;= 0); squads (non-empty list of { troops (int &gt;= 0),
    /// grade (int &gt;= 1) }).
    /// Implements: Story 006 (all troop numbers from external data).
    ///
    /// Usage:
    /// <code>
    /// TroopRules rules = TroopDataLoader.ParseRules(rulesAsset.text);
    /// TroopRoster start = TroopDataLoader.ParseRoster(rosterAsset.text, rules);
    /// </code>
    /// </summary>
    public static class TroopDataLoader
    {
        [Serializable]
        private class RulesWire
        {
            public int max_troops_per_squad;
            public int reinforce_gold_per_troop;
            public List<int> promote_gold_costs;
            public float power_per_grade;
            public float defense_per_grade;
        }

        [Serializable]
        private class SquadWire
        {
            public int troops;
            public int grade;
        }

        [Serializable]
        private class RosterWire
        {
            public int gold;
            public List<SquadWire> squads;
        }

        /// <summary>Parses troops_rules.json text.</summary>
        /// <param name="json">JSON text in the troops_rules.json wire format.</param>
        /// <returns>Validated rules.</returns>
        /// <exception cref="InvalidDataException">The text is empty/invalid or a value is out of range.</exception>
        public static TroopRules ParseRules(string json)
        {
            RulesWire wire = FromJson<RulesWire>(json, "troops_rules");
            Require(wire.max_troops_per_squad >= 1, "max_troops_per_squad must be >= 1");
            Require(wire.reinforce_gold_per_troop >= 0, "reinforce_gold_per_troop must be >= 0");
            var costs = wire.promote_gold_costs ?? new List<int>();
            for (int i = 0; i < costs.Count; i++)
            {
                Require(costs[i] >= 0, "promote_gold_costs[" + i + "] must be >= 0");
            }

            Require(wire.power_per_grade >= 0f, "power_per_grade must be >= 0");
            Require(wire.defense_per_grade >= 0f, "defense_per_grade must be >= 0");
            return new TroopRules
            {
                MaxTroopsPerSquad = wire.max_troops_per_squad,
                ReinforceGoldPerTroop = wire.reinforce_gold_per_troop,
                PromoteGoldCosts = new List<int>(costs),
                PowerPerGrade = wire.power_per_grade,
                DefensePerGrade = wire.defense_per_grade
            };
        }

        /// <summary>Parses a roster JSON text (see troops_start_roster.json) and checks it against the rules.</summary>
        /// <param name="json">JSON text in the roster wire format.</param>
        /// <param name="rules">Troop rules: troops must fit the squad maximum and grades the highest grade.</param>
        /// <returns>Validated roster.</returns>
        /// <exception cref="InvalidDataException">The text is empty/invalid or a value is out of range.</exception>
        public static TroopRoster ParseRoster(string json, TroopRules rules)
        {
            RosterWire wire = FromJson<RosterWire>(json, "troop roster");
            Require(wire.gold >= 0, "gold must be >= 0");
            Require(wire.squads != null && wire.squads.Count > 0, "squads must contain at least one entry");
            var roster = new TroopRoster { Gold = wire.gold };
            for (int i = 0; i < wire.squads.Count; i++)
            {
                SquadWire squad = wire.squads[i];
                Require(squad != null, "squads[" + i + "] is missing");
                Require(squad.troops >= 0 && squad.troops <= rules.MaxTroopsPerSquad,
                    "squads[" + i + "].troops must be >= 0 and <= max_troops_per_squad (" + rules.MaxTroopsPerSquad + ")");
                Require(squad.grade >= 1 && squad.grade <= rules.MaxGrade,
                    "squads[" + i + "].grade must be >= 1 and <= the highest grade (" + rules.MaxGrade + ")");
                roster.Squads.Add(new Squad { Troops = squad.troops, Grade = squad.grade });
            }

            return roster;
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
                throw new InvalidDataException("Troop data: " + name + ": invalid JSON: " + e.Message, e);
            }

            Require(value != null, name + ": JSON did not produce data");
            return value;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidDataException("Troop data: " + message);
            }
        }
    }
}
