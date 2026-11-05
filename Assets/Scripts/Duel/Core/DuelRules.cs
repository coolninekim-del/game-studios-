namespace KnightOrder.Duel
{
    /// <summary>
    /// Every tunable number of the boss duel, loaded from external data (Assets/Data/Duel/duel_rules.json).
    /// Implements: Story 004 (no hardcoded duel values).
    ///
    /// Lance power = round(RiderAttack * (1 + HorseSpeed * SpeedPowerScale)); duel health = RiderHp + HorseHp.
    /// Per pass, damage to the boss = floor(protagonist power * timing multiplier * (GuardedMultiplier if the
    /// lance was aimed at the boss's shield, else 1)); the timing multiplier is SuccessMultiplier, EarlyMultiplier
    /// or 0 (no tap). Damage to the protagonist = boss power, or 0 when the tap was a Success (the boss's lance
    /// is deflected).
    /// </summary>
    public sealed class DuelRules
    {
        /// <summary>Seconds from the start of a pass (riders at the ends of the list) to the impact.</summary>
        public float ApproachSeconds { get; set; }

        /// <summary>Seconds before the impact at which the boss's shield side is shown and taps start to count.</summary>
        public float GuardRevealSeconds { get; set; }

        /// <summary>Width of the judgement window, ending at the impact.</summary>
        public float WindowSeconds { get; set; }

        /// <summary>Seconds after the impact until the next pass starts (riders turn their horses).</summary>
        public float TurnaroundSeconds { get; set; }

        /// <summary>Lance power gained per point of horse speed, as a fraction of rider attack.</summary>
        public float SpeedPowerScale { get; set; }

        /// <summary>Timing multiplier on the protagonist's lance for a tap inside the window.</summary>
        public float SuccessMultiplier { get; set; }

        /// <summary>Timing multiplier on the protagonist's lance for a tap before the window.</summary>
        public float EarlyMultiplier { get; set; }

        /// <summary>Extra multiplier when the lance is aimed at the side the boss's shield covers.</summary>
        public float GuardedMultiplier { get; set; }

        /// <summary>Safety cap: when this many passes end with both riders up, the duel is lost.</summary>
        public int MaxPasses { get; set; }
    }
}
