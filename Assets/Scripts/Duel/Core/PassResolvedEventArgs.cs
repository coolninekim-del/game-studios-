using System;
using KnightOrder.Battle.Timing;

namespace KnightOrder.Duel
{
    /// <summary>
    /// One resolved pass: how the protagonist's tap was judged, where both aimed, and the damage dealt.
    /// Implements: Story 004.
    /// </summary>
    public sealed class PassResolvedEventArgs : EventArgs
    {
        internal PassResolvedEventArgs(int passIndex, TimingOutcome outcome, LanceAim? heroAim, LanceAim bossGuard,
            int damageToBoss, int damageToHero)
        {
            PassIndex = passIndex;
            Outcome = outcome;
            HeroAim = heroAim;
            BossGuard = bossGuard;
            DamageToBoss = damageToBoss;
            DamageToHero = damageToHero;
        }

        /// <summary>0-based pass number.</summary>
        public int PassIndex { get; }

        /// <summary>Success (tap inside the window), Early (before it) or Miss (no tap).</summary>
        public TimingOutcome Outcome { get; }

        /// <summary>Where the protagonist aimed, or null without a tap.</summary>
        public LanceAim? HeroAim { get; }

        /// <summary>The side the boss's shield covered on this pass.</summary>
        public LanceAim BossGuard { get; }

        /// <summary>True if the protagonist tapped and aimed at the side the shield covered.</summary>
        public bool HitShield => HeroAim.HasValue && HeroAim.Value == BossGuard;

        /// <summary>Health the boss lost on this pass.</summary>
        public int DamageToBoss { get; }

        /// <summary>Health the protagonist lost on this pass (0 when the boss's lance was deflected).</summary>
        public int DamageToHero { get; }
    }
}
