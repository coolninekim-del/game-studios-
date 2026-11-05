using System;

namespace KnightOrder.Battle.Timing
{
    /// <summary>
    /// An enemy arrow volley at a player unit was judged; raised from inside OnArrowVolley, before its
    /// damage is applied. Presentation uses it for the block/hit feedback.
    /// Implements: Story 003.
    /// </summary>
    public sealed class ArrowJudgedEventArgs : EventArgs
    {
        internal ArrowJudgedEventArgs(int archerIndex, int targetIndex, TimingOutcome outcome, float damageMultiplier)
        {
            ArcherIndex = archerIndex;
            TargetIndex = targetIndex;
            Outcome = outcome;
            DamageMultiplier = damageMultiplier;
        }

        /// <summary>Index of the firing enemy unit in <see cref="BattleSimulation.Units"/>.</summary>
        public int ArcherIndex { get; }

        /// <summary>Index of the player unit hit by the volley in <see cref="BattleSimulation.Units"/>.</summary>
        public int TargetIndex { get; }

        /// <summary>The judged outcome.</summary>
        public TimingOutcome Outcome { get; }

        /// <summary>Multiplier the timing input applied to this volley's damage (1 = full damage).</summary>
        public float DamageMultiplier { get; }
    }
}
