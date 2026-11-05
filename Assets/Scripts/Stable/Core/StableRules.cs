using System.Collections.Generic;

namespace KnightOrder.Stable
{
    /// <summary>
    /// Every tunable number of the stable and of the stat-to-battle conversion, loaded from external data
    /// (Assets/Data/Stable/stable_rules.json). The duel conversion numbers live in the duel rules.
    /// Implements: Story 005 (no hardcoded stable values).
    ///
    /// In battle the protagonist is one mounted unit: charge power = round(RiderAttack * (1 + HorseSpeed *
    /// BattleSpeedPowerScale)), health (troops) = RiderHp + HorseHp, defense = BattleDefense, and its charge
    /// window = base window * (1 + HorseAgility * BattleAgilityWindowScale).
    /// </summary>
    public sealed class StableRules
    {
        /// <summary>Training actions per stable visit.</summary>
        public int ActionsPerVisit { get; set; }

        /// <summary>The trainings offered, one per stat at most, in display order.</summary>
        public List<TrainingOption> Trainings { get; set; } = new List<TrainingOption>();

        /// <summary>Battle charge power gained per point of horse speed, as a fraction of rider attack.</summary>
        public float BattleSpeedPowerScale { get; set; }

        /// <summary>Battle charge window gained per point of horse agility, as a fraction of the base window.</summary>
        public float BattleAgilityWindowScale { get; set; }

        /// <summary>The mounted protagonist's defense in battle.</summary>
        public float BattleDefense { get; set; }

        /// <summary>The mounted protagonist's starting grid column in battle.</summary>
        public int BattleStartX { get; set; }

        /// <summary>The mounted protagonist's starting grid row in battle.</summary>
        public int BattleStartY { get; set; }

        /// <summary>The training for a stat, or null if the stable does not offer one.</summary>
        /// <param name="stat">The stat.</param>
        /// <returns>The option, or null.</returns>
        public TrainingOption TrainingFor(KnightStat stat)
        {
            foreach (TrainingOption option in Trainings)
            {
                if (option.Stat == stat)
                {
                    return option;
                }
            }

            return null;
        }
    }
}
