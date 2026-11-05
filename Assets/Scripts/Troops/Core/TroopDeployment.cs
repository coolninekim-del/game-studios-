using System;
using System.Collections.Generic;
using KnightOrder.Battle;

namespace KnightOrder.Troops
{
    /// <summary>
    /// Puts the roster's squads into a battle setup and, after the battle, writes the survivors back. Squad i
    /// replaces the i-th foot (non-mounted) player unit of the setup: its troops become the squad's troops and
    /// its charge power and defense are raised by the squad's grade (<see cref="TroopRules"/>), the setup's
    /// values being grade 1. A squad with 0 troops does not fight: its unit is removed from the setup.
    /// Mounted units (the protagonist) are left as they are and are not part of the roster.
    /// Implements: Story 006 (grades reach the battle numbers, battle losses persist).
    ///
    /// Usage:
    /// <code>
    /// TroopDeployment deployment = TroopDeployment.Deploy(battleSetup, roster, troopRules);
    /// // ... run the battle ...
    /// deployment.RecordSurvivors(roster, simulation);
    /// </code>
    /// </summary>
    public sealed class TroopDeployment
    {
        private readonly int[] _unitIndexOfSquad;

        private TroopDeployment(int[] unitIndexOfSquad)
        {
            _unitIndexOfSquad = unitIndexOfSquad;
        }

        /// <summary>Applies the roster to the setup's player foot units (the setup is changed in place).</summary>
        /// <param name="setup">A battle setup with exactly one foot player unit per squad.</param>
        /// <param name="roster">The order's squads.</param>
        /// <param name="rules">Troop rules (grade bonuses).</param>
        /// <returns>Which battle unit each squad became, for <see cref="RecordSurvivors"/>.</returns>
        /// <exception cref="InvalidOperationException">The setup's foot player unit count differs from the squad count.</exception>
        public static TroopDeployment Deploy(BattleSetup setup, TroopRoster roster, TroopRules rules)
        {
            var footUnits = new List<UnitSetup>();
            foreach (UnitSetup unit in setup.PlayerUnits)
            {
                if (!unit.IsMounted)
                {
                    footUnits.Add(unit);
                }
            }

            if (footUnits.Count != roster.Squads.Count)
            {
                throw new InvalidOperationException("the battle setup has " + footUnits.Count
                    + " foot player units but the roster has " + roster.Squads.Count + " squads");
            }

            for (int i = 0; i < footUnits.Count; i++)
            {
                Squad squad = roster.Squads[i];
                UnitSetup unit = footUnits[i];
                if (squad.Troops <= 0)
                {
                    setup.PlayerUnits.Remove(unit);
                    footUnits[i] = null;
                    continue;
                }

                int grades = Math.Max(0, squad.Grade - 1);
                unit.Troops = squad.Troops;
                unit.ChargePower *= 1f + grades * rules.PowerPerGrade;
                unit.Defense += grades * rules.DefensePerGrade;
            }

            var indices = new int[footUnits.Count];
            for (int i = 0; i < footUnits.Count; i++)
            {
                // Player units come first in the simulation, in setup order, so the setup index is the unit index.
                indices[i] = footUnits[i] == null ? -1 : setup.PlayerUnits.IndexOf(footUnits[i]);
            }

            return new TroopDeployment(indices);
        }

        /// <summary>The battle unit index of a squad, or -1 when the squad did not fight.</summary>
        /// <param name="squadIndex">The squad.</param>
        /// <returns>Index into <see cref="BattleSimulation.Units"/>, or -1.</returns>
        public int UnitIndexOf(int squadIndex) => _unitIndexOfSquad[squadIndex];

        /// <summary>
        /// Writes each fighting squad's remaining troops back into the roster; squads that did not fight keep 0.
        /// Call it once the battle is over.
        /// </summary>
        /// <param name="roster">The roster that was deployed.</param>
        /// <param name="simulation">The battle simulation built from the deployed setup.</param>
        public void RecordSurvivors(TroopRoster roster, BattleSimulation simulation)
        {
            for (int i = 0; i < _unitIndexOfSquad.Length; i++)
            {
                int unitIndex = _unitIndexOfSquad[i];
                roster.Squads[i].Troops = unitIndex < 0 ? 0 : simulation.Units[unitIndex].Troops;
            }
        }
    }
}
