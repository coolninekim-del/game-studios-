using System;

namespace KnightOrder.Stable
{
    /// <summary>
    /// One stable visit: a limited number of training actions, each raising one stat of the profile by the
    /// option's gain. Works on its own copy of the profile; read <see cref="Profile"/> for the result.
    /// Pure C#, single-thread only.
    /// Implements: Story 005.
    ///
    /// Usage:
    /// <code>
    /// var visit = new StableSession(rules, KnightProfileStore.Current ?? startProfile);
    /// TrainingResult result = visit.Train(KnightStat.HorseSpeed);   // null when no action is left
    /// KnightProfileStore.Current = visit.Profile;
    /// </code>
    /// </summary>
    public sealed class StableSession
    {
        private readonly StableRules _rules;

        /// <summary>Starts a visit with every action available.</summary>
        /// <param name="rules">Stable rules (validated by the loader).</param>
        /// <param name="profile">The profile to train; it is copied, the caller's object is not changed.</param>
        public StableSession(StableRules rules, KnightProfile profile)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Profile = (profile ?? throw new ArgumentNullException(nameof(profile))).Clone();
            ActionsLeft = rules.ActionsPerVisit;
        }

        /// <summary>The trained profile so far.</summary>
        public KnightProfile Profile { get; }

        /// <summary>Training actions still available in this visit.</summary>
        public int ActionsLeft { get; private set; }

        /// <summary>Training actions per visit.</summary>
        public int ActionsPerVisit => _rules.ActionsPerVisit;

        /// <summary>The last training's result, or null before the first one.</summary>
        public TrainingResult LastResult { get; private set; }

        /// <summary>Spends one action to raise a stat.</summary>
        /// <param name="stat">The stat to train.</param>
        /// <returns>What changed, or null (nothing changes) when no action is left or the stat has no training.</returns>
        public TrainingResult Train(KnightStat stat)
        {
            TrainingOption option = _rules.TrainingFor(stat);
            if (ActionsLeft <= 0 || option == null)
            {
                return null;
            }

            KnightProfile before = Profile.Clone();
            Profile.Set(stat, Profile.Get(stat) + option.Gain);
            ActionsLeft--;
            LastResult = new TrainingResult(stat, before, Profile.Clone());
            return LastResult;
        }
    }
}
