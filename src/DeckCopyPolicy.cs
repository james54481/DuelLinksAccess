namespace DuelLinksAccess
{
    public enum DeckCopyStage { None, SourceSelection, SourcePreview, DestinationSelection, DestinationPreview }
    public static class DeckCopyPolicy
    {
        public static DeckCopyStage Stage(bool selection, string mode) => (selection, mode) switch
        {
            (true, "CopySrc") or (true, "CopySrcOnly") => DeckCopyStage.SourceSelection,
            (true, "CopyDest") => DeckCopyStage.DestinationSelection,
            (false, "DeckCopySrc") or (false, "DeckCopySrcOnly") => DeckCopyStage.SourcePreview,
            (false, "DeckCopyDest") => DeckCopyStage.DestinationPreview,
            _ => DeckCopyStage.None
        };
    }
    public sealed class DeckCopyState
    {
        private int _session;
        public string SourceKey { get; private set; }
        public string SourceLabel { get; private set; }
        public string DestinationKey { get; private set; }
        public string DestinationLabel { get; private set; }
        public int DestinationSelectionRoot { get; private set; }
        public bool DestinationSlotKnown { get; private set; }
        public void End()
        {
            _session = 0; SourceKey = SourceLabel = DestinationKey = DestinationLabel = null;
            DestinationSelectionRoot = 0; DestinationSlotKnown = false;
        }
        public void BeginSource(int session)
        {
            if (_session == session) return;
            _session = session; SourceKey = SourceLabel = DestinationKey = DestinationLabel = null;
            DestinationSlotKnown = false; DestinationSelectionRoot = 0;
        }
        public void Source(string key, string label)
        {
            if (SourceKey != key) { DestinationKey = DestinationLabel = null; DestinationSlotKnown = false; DestinationSelectionRoot = 0; }
            SourceKey = key; SourceLabel = label;
        }
        public void Destination(string key, string label, int selectionRoot = 0, bool slotKnown = true)
        { DestinationKey = key; DestinationLabel = label; DestinationSelectionRoot = selectionRoot; DestinationSlotKnown = slotKnown; }
        public bool Matches(string source, string destination) => !string.IsNullOrWhiteSpace(source)
            && !string.IsNullOrWhiteSpace(destination) && SourceKey == source && DestinationKey == destination
            && !string.IsNullOrWhiteSpace(SourceLabel) && !string.IsNullOrWhiteSpace(DestinationLabel) && DestinationSlotKnown;
    }
}
