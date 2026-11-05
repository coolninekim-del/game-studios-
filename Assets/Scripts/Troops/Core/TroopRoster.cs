using System.Collections.Generic;

namespace KnightOrder.Troops
{
    /// <summary>
    /// The order's foot-soldier squads and its gold, with the reinforce and promote rules. A refused request
    /// changes nothing. Battle losses are written back by <see cref="TroopDeployment.RecordSurvivors"/>, so
    /// they stay until the next stable visit. Pure C#, single-thread only.
    /// Implements: Story 006.
    ///
    /// Usage:
    /// <code>
    /// TroopActionResult r = roster.Reinforce(0, 20, rules);   // squad 0 gains 20 soldiers for 20 gold
    /// if (r == TroopActionResult.NotEnoughGold) ShowNoGold();
    /// roster.Promote(1, rules);
    /// if (roster.IsWipedOut) EndRun();
    /// </code>
    /// </summary>
    public sealed class TroopRoster
    {
        /// <summary>The squads, in battle order (squad i is the i-th foot unit of the battle setup).</summary>
        public List<Squad> Squads { get; set; } = new List<Squad>();

        /// <summary>Gold available for reinforcing and promoting.</summary>
        public int Gold { get; set; }

        /// <summary>Soldiers in all squads together.</summary>
        public int TotalTroops
        {
            get
            {
                int total = 0;
                foreach (Squad squad in Squads)
                {
                    total += squad.Troops;
                }

                return total;
            }
        }

        /// <summary>True when every squad has 0 soldiers: the order is wiped out (Story 007's fail state).</summary>
        public bool IsWipedOut => TotalTroops == 0;

        /// <summary>Gold needed to add soldiers to a squad.</summary>
        /// <param name="count">Soldiers to add.</param>
        /// <param name="rules">Troop rules.</param>
        /// <returns>count * ReinforceGoldPerTroop.</returns>
        public static int ReinforceCost(int count, TroopRules rules) => count * rules.ReinforceGoldPerTroop;

        /// <summary>Gold needed to promote a squad from its grade, or -1 at the highest grade.</summary>
        /// <param name="grade">The squad's current grade.</param>
        /// <param name="rules">Troop rules.</param>
        /// <returns>PromoteGoldCosts[grade - 1], or -1.</returns>
        public static int PromoteCost(int grade, TroopRules rules)
        {
            return grade >= 1 && grade < rules.MaxGrade ? rules.PromoteGoldCosts[grade - 1] : -1;
        }

        /// <summary>Adds soldiers to a squad for gold.</summary>
        /// <param name="squadIndex">The squad.</param>
        /// <param name="count">Soldiers to add (at least 1).</param>
        /// <param name="rules">Troop rules.</param>
        /// <returns>Done, or why the request was refused (nothing changes then).</returns>
        public TroopActionResult Reinforce(int squadIndex, int count, TroopRules rules)
        {
            if (squadIndex < 0 || squadIndex >= Squads.Count || count < 1)
            {
                return TroopActionResult.InvalidRequest;
            }

            Squad squad = Squads[squadIndex];
            if (squad.Troops + count > rules.MaxTroopsPerSquad)
            {
                return TroopActionResult.SquadFull;
            }

            int cost = ReinforceCost(count, rules);
            if (Gold < cost)
            {
                return TroopActionResult.NotEnoughGold;
            }

            Gold -= cost;
            squad.Troops += count;
            return TroopActionResult.Done;
        }

        /// <summary>Raises a squad's grade by 1 for gold.</summary>
        /// <param name="squadIndex">The squad.</param>
        /// <param name="rules">Troop rules.</param>
        /// <returns>Done, or why the request was refused (nothing changes then).</returns>
        public TroopActionResult Promote(int squadIndex, TroopRules rules)
        {
            if (squadIndex < 0 || squadIndex >= Squads.Count)
            {
                return TroopActionResult.InvalidRequest;
            }

            Squad squad = Squads[squadIndex];
            int cost = PromoteCost(squad.Grade, rules);
            if (cost < 0)
            {
                return TroopActionResult.MaxGrade;
            }

            if (Gold < cost)
            {
                return TroopActionResult.NotEnoughGold;
            }

            Gold -= cost;
            squad.Grade++;
            return TroopActionResult.Done;
        }

        /// <summary>An independent copy.</summary>
        /// <returns>A new roster with copies of the squads.</returns>
        public TroopRoster Clone()
        {
            var copy = new TroopRoster { Gold = Gold };
            foreach (Squad squad in Squads)
            {
                copy.Squads.Add(squad.Clone());
            }

            return copy;
        }
    }
}
