namespace DuelLinksAccess
{
    public enum TicketBrowseSource { None, Choice, Exchange }
    internal static class TicketBrowseSourcePolicy
    {
        internal static TicketBrowseSource Choose(string mode, int choices, int exchanges)
        {
            // BoxChip.exchangeList is the pending selection, not the catalogue.
            // Never fall back to it while the catalogue is being refreshed.
            if (mode == "BoxChip") return choices > 0 ? TicketBrowseSource.Choice : TicketBrowseSource.None;
            if (exchanges > 0) return TicketBrowseSource.Exchange;
            return choices > 0 ? TicketBrowseSource.Choice : TicketBrowseSource.None;
        }
    }
}
