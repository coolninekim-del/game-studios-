using System.Collections.Generic;

namespace KnightOrder.Troops
{
    /// <summary>
    /// Every tunable number of troop training, loaded from external data (Assets/Data/Troops/troops_rules.json).
    /// Implements: Story 006 (no hardcoded troop values).
    ///
    /// Reinforcing n soldiers costs n * ReinforceGoldPerTroop; promoting from grade g costs PromoteGoldCosts[g - 1],
    /// so the highest grade is PromoteGoldCosts.Count + 1. In battle a squad of grade g has charge power =
    /// base * (1 + (g - 1) * PowerPerGrade) and defense = base + (g - 1) * DefensePerGrade, the base values being
    /// the battle setup's foot-unit values (grade 1).
    /// </summary>
    public sealed class TroopRules
    {
        /// <summary>Most soldiers one squad can hold.</summary>
        public int MaxTroopsPerSquad { get; set; }

        /// <summary>Gold per soldier added by a reinforcement.</summary>
        public int ReinforceGoldPerTroop { get; set; }

        /// <summary>Gold to promote from grade 1, 2, ... (index = grade - 1).</summary>
        public List<int> PromoteGoldCosts { get; set; } = new List<int>();

        /// <summary>Charge power gained per grade above 1, as a fraction of the base power.</summary>
        public float PowerPerGrade { get; set; }

        /// <summary>Defense gained per grade above 1.</summary>
        public float DefensePerGrade { get; set; }

        /// <summary>The highest grade: PromoteGoldCosts.Count + 1.</summary>
        public int MaxGrade => PromoteGoldCosts.Count + 1;
    }
}
