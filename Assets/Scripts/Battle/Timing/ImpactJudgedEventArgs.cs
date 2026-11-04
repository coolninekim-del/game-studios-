using System;

namespace KnightOrder.Battle.Timing
{
    /// <summary>
    /// A player unit's impact was judged; raised from inside OnChargeImpact, before damage is applied.
    /// Presentation uses it for the success/fail feedback.
    /// Implements: Story 002.
    /// </summary>
    public sealed class ImpactJudgedEventArgs : EventArgs
    {
        internal ImpactJudgedEventArgs(int attackerIndex, int defenderIndex, TimingOutcome outcome, float powerMultiplier)
        {
            AttackerIndex = attackerIndex;
            DefenderIndex = defenderIndex;
            Outcome = outcome;
            PowerMultiplier = powerMultiplier;
        }

        /// <summary>Index of the attacker in <see cref="BattleSimulation.Units"/>.</summary>
        public int AttackerIndex { get; }

        /// <summary>Index of the defender in <see cref="BattleSimulation.Units"/>.</summary>
        public int DefenderIndex { get; }

        /// <summary>The judged outcome.</summary>
        public TimingOutcome Outcome { get; }

        /// <summary>Multiplier the timing input applied to this impact's charge power.</summary>
        public float PowerMultiplier { get; }
    }
}
