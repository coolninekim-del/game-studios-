namespace KnightOrder.Stable
{
    /// <summary>
    /// The battle and duel numbers one profile produces, for showing growth in the stable.
    /// Implements: Story 005.
    /// </summary>
    public sealed class KnightNumbers
    {
        /// <summary>Charge power of the mounted protagonist in battle.</summary>
        public int BattleChargePower { get; set; }

        /// <summary>Health (troops) of the mounted protagonist in battle.</summary>
        public int BattleHealth { get; set; }

        /// <summary>The protagonist's charge judgement window in battle, in seconds.</summary>
        public float BattleWindowSeconds { get; set; }

        /// <summary>Lance power in the duel.</summary>
        public int DuelLancePower { get; set; }

        /// <summary>Health in the duel.</summary>
        public int DuelHealth { get; set; }

        /// <summary>The protagonist's judgement window in the duel, in seconds.</summary>
        public float DuelWindowSeconds { get; set; }
    }
}
