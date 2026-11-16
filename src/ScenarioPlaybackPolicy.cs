namespace DuelLinksAccess
{
    internal static class ScenarioPlaybackPolicy
    {
        internal static bool Initialize(long controller, long appliedController, bool ready)
            => ready && controller != 0 && controller != appliedController;
    }
}
