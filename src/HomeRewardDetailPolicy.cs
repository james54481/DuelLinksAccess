namespace DuelLinksAccess
{
    internal static class HomeRewardDetailPolicy
    {
        // An unchanged, already-visible detail cannot identify a different reward.
        internal static bool CanRead(bool wasVisible, int previous, int current, int expected)
            => current > 0 && (expected > 0 ? current == expected : !wasVisible || current != previous);
    }
}
