using System;
using System.IO;
using UnityEngine;

namespace KnightOrder.Battle.Loading
{
    /// <summary>
    /// Unity boundary: parses and validates battle_charge_timing.json (Assets/Data/Battle/).
    /// Like <see cref="BattleDataLoader"/>, the caller supplies the JSON text.
    ///
    /// Wire format: seconds_per_tick (float &gt; 0, real-time seconds per battle tick),
    /// window_seconds (float &gt; 0, judgement window ending at the impact), cue_lead_seconds
    /// (float &gt;= window_seconds, how long before the impact the cue appears; taps between cue and window
    /// are Early), success_power_multiplier (float &gt;= 1, charge power multiplier on Success).
    /// Story 003 keys: arrow_window_seconds (float &gt; 0), arrow_cue_lead_seconds (float &gt;=
    /// arrow_window_seconds), arrow_success_damage_multiplier (float in [0, 1], arrow damage multiplier on
    /// Success; 0 = fully blocked).
    /// Implements: Story 002 (window width and multiplier from external data), Story 003 (arrow values).
    ///
    /// Usage:
    /// <code>
    /// ChargeTimingRules rules = ChargeTimingDataLoader.ParseRules(timingJson.text);
    /// </code>
    /// </summary>
    public static class ChargeTimingDataLoader
    {
        [Serializable]
        private class RulesWire
        {
            public float seconds_per_tick;
            public float window_seconds;
            public float cue_lead_seconds;
            public float success_power_multiplier;
            public float arrow_window_seconds;
            public float arrow_cue_lead_seconds;
            public float arrow_success_damage_multiplier;
        }

        /// <summary>Parses battle_charge_timing.json text.</summary>
        /// <param name="json">JSON text in the battle_charge_timing.json wire format.</param>
        /// <returns>Validated timing rules.</returns>
        /// <exception cref="InvalidDataException">The text is empty/invalid or a value is out of range.</exception>
        public static Timing.ChargeTimingRules ParseRules(string json)
        {
            Require(!string.IsNullOrWhiteSpace(json), "JSON text is empty");
            RulesWire wire;
            try
            {
                wire = JsonUtility.FromJson<RulesWire>(json);
            }
            catch (Exception e)
            {
                throw new InvalidDataException("Charge timing data: invalid JSON: " + e.Message, e);
            }

            Require(wire != null, "JSON did not produce data");
            Require(wire.seconds_per_tick > 0f, "seconds_per_tick must be > 0");
            Require(wire.window_seconds > 0f, "window_seconds must be > 0");
            Require(
                wire.cue_lead_seconds >= wire.window_seconds,
                "cue_lead_seconds (" + wire.cue_lead_seconds + ") must be >= window_seconds ("
                + wire.window_seconds + "), the cue has to appear before the window opens");
            Require(
                wire.success_power_multiplier >= 1f,
                "success_power_multiplier must be >= 1 (a success must not weaken the charge)");
            Require(wire.arrow_window_seconds > 0f, "arrow_window_seconds must be > 0");
            Require(
                wire.arrow_cue_lead_seconds >= wire.arrow_window_seconds,
                "arrow_cue_lead_seconds (" + wire.arrow_cue_lead_seconds + ") must be >= arrow_window_seconds ("
                + wire.arrow_window_seconds + "), the cue has to appear before the window opens");
            Require(
                wire.arrow_success_damage_multiplier >= 0f && wire.arrow_success_damage_multiplier <= 1f,
                "arrow_success_damage_multiplier must be in [0, 1] (a success must not increase arrow damage)");

            return new Timing.ChargeTimingRules
            {
                SecondsPerTick = wire.seconds_per_tick,
                WindowSeconds = wire.window_seconds,
                CueLeadSeconds = wire.cue_lead_seconds,
                SuccessPowerMultiplier = wire.success_power_multiplier,
                ArrowWindowSeconds = wire.arrow_window_seconds,
                ArrowCueLeadSeconds = wire.arrow_cue_lead_seconds,
                ArrowSuccessDamageMultiplier = wire.arrow_success_damage_multiplier
            };
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidDataException("Charge timing data: " + message);
            }
        }
    }
}
