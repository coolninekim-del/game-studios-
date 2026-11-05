namespace KnightOrder.Stable
{
    /// <summary>
    /// Carries the trained profile from the stable scene to the battle and duel scenes. Temporary until the
    /// Story 007 run state replaces it. Null until the stable sets it; the battle and duel then use the start
    /// profile. Single-thread only (main thread).
    /// Implements: Story 005.
    ///
    /// Usage:
    /// <code>
    /// KnightProfileStore.Current = visit.Profile;                            // stable
    /// KnightProfile profile = KnightProfileStore.Current ?? startProfile;    // battle / duel
    /// </code>
    /// </summary>
    public static class KnightProfileStore
    {
        /// <summary>The latest trained profile, or null when the stable has not been visited.</summary>
        public static KnightProfile Current { get; set; }
    }
}
