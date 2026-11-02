namespace DuelLinksAccess
{
    internal static class TraderPresentationPolicy
    {
        internal static bool IsPresentation(string mode, bool resultBackground, bool resultItem, bool resultCard)
            => resultBackground || resultItem || mode == "TalkThanksWait" || mode == "TalkThanksEnd";
        internal static bool CanAdvance(bool resultBackground, bool resultItem, bool resultCard)
            => resultBackground || resultItem;
    }
}
