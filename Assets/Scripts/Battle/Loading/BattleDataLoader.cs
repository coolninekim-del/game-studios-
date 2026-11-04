using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KnightOrder.Battle.Loading
{
    /// <summary>
    /// Unity boundary: parses and validates the battle JSON data (Assets/Data/Battle/).
    /// The caller supplies the JSON text (for example <c>textAsset.text</c>), so how the files
    /// are delivered (TextAsset reference, Addressables, ...) stays the caller's decision.
    ///
    /// The JSON files use snake_case keys (project data-file rule), while JsonUtility binds by exact
    /// field name, so the private nested DTOs below ARE the wire format and each maps once into the
    /// pure-C# core types. This is the only place that touches UnityEngine.
    ///
    /// Wire format of battle_rules.json: power_scale (float, multiplier on attacker charge power),
    /// defense_scale (float, multiplier on defender defense), min_damage (int troops, per-impact floor),
    /// cells_per_tick (int grid cells per axis per tick), impact_range (int Chebyshev grid cells,
    /// must be >= 1 and >= cells_per_tick so approaching units cannot leap past each other),
    /// impact_interval_ticks (int ticks between a unit's impacts), max_ticks (int safety cap on
    /// battle length). These checks prevent the known non-collision case; they do not prove every
    /// layout ends with a wipe-out, so termination is guaranteed by max_ticks.
    /// Story 008 keys: reference_troops (float &gt; 0, troop count where the troop multiplier is 1),
    /// troop_exponent (float &gt;= 0, multiplier = (attacker troops / reference_troops) ^ troop_exponent;
    /// 0 = no effect, 1 = linear, below 1 = diminishing), charges_before_melee (int &gt;= 1, impacts with a
    /// retreat after each before the unit stops retreating and fights in melee; 1 = melee after the first
    /// impact), disengage_distance (int &gt;= 1 and &gt; impact_range, grid cells retreated after an impact,
    /// so a retreat actually leaves impact range; always checked, even when charges_before_melee is 1
    /// and it is unused).
    /// Story 003 keys: arrow_range (int &gt;= 0, Chebyshev distance at which an enemy unit fires a volley at
    /// its nearest charging player unit; 0 = no arrows, otherwise must be &gt; impact_range), arrow_damage
    /// (int &gt;= 0, troops killed by an undefended volley), arrow_interval_ticks (int &gt;= 1 when arrows are
    /// on, ticks between a unit's volleys). A missing key reads as 0, so an older file means "no arrows".
    /// Wire format of a setup file: player_units and enemy_units, each a list of
    /// { troops (int), charge_power (float), defense (float), start_x (int), start_y (int) }.
    /// Implements: Story 001 (all numbers from external data), Story 003 (arrow numbers).
    /// </summary>
    public static class BattleDataLoader
    {
        [Serializable]
        private class RulesWire
        {
            public float power_scale;
            public float defense_scale;
            public int min_damage;
            public int cells_per_tick;
            public int impact_range;
            public int impact_interval_ticks;
            public int max_ticks;
            public float reference_troops;
            public float troop_exponent;
            public int charges_before_melee;
            public int disengage_distance;
            public int arrow_range;
            public int arrow_damage;
            public int arrow_interval_ticks;
        }

        [Serializable]
        private class UnitWire
        {
            public int troops;
            public float charge_power;
            public float defense;
            public int start_x;
            public int start_y;
        }

        [Serializable]
        private class SetupWire
        {
            public List<UnitWire> player_units;
            public List<UnitWire> enemy_units;
        }

        /// <summary>Parses battle_rules.json text.</summary>
        /// <param name="json">JSON text in the battle_rules.json wire format.</param>
        /// <returns>Validated rules.</returns>
        /// <exception cref="InvalidDataException">The text is empty/invalid or a value is out of range.</exception>
        public static BattleRules ParseRules(string json)
        {
            RulesWire wire = FromJson<RulesWire>(json, "battle_rules");
            Require(wire.power_scale > 0f, "power_scale must be > 0");
            Require(wire.defense_scale >= 0f, "defense_scale must be >= 0");
            Require(wire.min_damage >= 1, "min_damage must be >= 1 (every impact must deal damage)");
            Require(wire.cells_per_tick >= 1, "cells_per_tick must be >= 1 (units must be able to move)");
            Require(wire.impact_range >= 1, "impact_range must be >= 1");
            Require(
                wire.impact_range >= wire.cells_per_tick,
                "impact_range (" + wire.impact_range + ") must be >= cells_per_tick (" + wire.cells_per_tick
                + "), otherwise approaching units can leap past each other without ever colliding");
            Require(wire.impact_interval_ticks >= 1, "impact_interval_ticks must be >= 1");
            Require(wire.max_ticks >= 1, "max_ticks must be >= 1");
            Require(wire.reference_troops > 0f, "reference_troops must be > 0");
            Require(wire.troop_exponent >= 0f, "troop_exponent must be >= 0");
            Require(wire.charges_before_melee >= 1, "charges_before_melee must be >= 1");
            Require(wire.disengage_distance >= 1, "disengage_distance must be >= 1");
            Require(
                wire.disengage_distance > wire.impact_range,
                "disengage_distance (" + wire.disengage_distance + ") must be > impact_range (" + wire.impact_range
                + "), otherwise a retreating unit is still within impact range and never really re-charges");
            Require(wire.arrow_range >= 0, "arrow_range must be >= 0 (0 = no arrows)");
            if (wire.arrow_range > 0)
            {
                Require(
                    wire.arrow_range > wire.impact_range,
                    "arrow_range (" + wire.arrow_range + ") must be > impact_range (" + wire.impact_range
                    + "), volleys are fired before the collision");
                Require(wire.arrow_damage >= 0, "arrow_damage must be >= 0");
                Require(wire.arrow_interval_ticks >= 1, "arrow_interval_ticks must be >= 1");
            }

            return new BattleRules
            {
                PowerScale = wire.power_scale,
                DefenseScale = wire.defense_scale,
                MinDamage = wire.min_damage,
                CellsPerTick = wire.cells_per_tick,
                ImpactRange = wire.impact_range,
                ImpactIntervalTicks = wire.impact_interval_ticks,
                MaxTicks = wire.max_ticks,
                ReferenceTroops = wire.reference_troops,
                TroopExponent = wire.troop_exponent,
                ChargesBeforeMelee = wire.charges_before_melee,
                DisengageDistance = wire.disengage_distance,
                ArrowRange = wire.arrow_range,
                ArrowDamage = wire.arrow_damage,
                ArrowIntervalTicks = wire.arrow_interval_ticks
            };
        }

        /// <summary>Parses a battle setup JSON text (see battle_sample_setup.json).</summary>
        /// <param name="json">JSON text in the setup wire format.</param>
        /// <returns>Validated setup with at least one unit per side.</returns>
        /// <exception cref="InvalidDataException">The text is empty/invalid or a value is out of range.</exception>
        public static BattleSetup ParseSetup(string json)
        {
            SetupWire wire = FromJson<SetupWire>(json, "battle setup");
            return new BattleSetup
            {
                PlayerUnits = ConvertUnits(wire.player_units, "player_units"),
                EnemyUnits = ConvertUnits(wire.enemy_units, "enemy_units")
            };
        }

        private static List<UnitSetup> ConvertUnits(List<UnitWire> wires, string name)
        {
            Require(wires != null && wires.Count > 0, name + " must contain at least one unit");
            var units = new List<UnitSetup>(wires.Count);
            for (int i = 0; i < wires.Count; i++)
            {
                UnitWire wire = wires[i];
                Require(wire.troops >= 1, name + "[" + i + "].troops must be >= 1");
                Require(wire.charge_power >= 0f, name + "[" + i + "].charge_power must be >= 0");
                Require(wire.defense >= 0f, name + "[" + i + "].defense must be >= 0");
                units.Add(new UnitSetup
                {
                    Troops = wire.troops,
                    ChargePower = wire.charge_power,
                    Defense = wire.defense,
                    StartX = wire.start_x,
                    StartY = wire.start_y
                });
            }

            return units;
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
                throw new InvalidDataException("Battle data: " + name + ": invalid JSON: " + e.Message, e);
            }

            Require(value != null, name + ": JSON did not produce data");
            return value;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidDataException("Battle data: " + message);
            }
        }
    }
}
