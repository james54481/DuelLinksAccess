namespace DuelLinksAccess
{
    public enum TextEditPurpose { PlayerName, FriendSearch, CardSearch, DeckSearch }

    public static class TextEditorPolicy
    {
        public static int MinimumLength(TextEditPurpose purpose) => purpose == TextEditPurpose.PlayerName ? 3 : 0;
        public static int MaximumLength(TextEditPurpose purpose, int nativeLimit) => nativeLimit > 0
            ? nativeLimit : purpose == TextEditPurpose.PlayerName ? 12 : 32767;
        // Empty text clears a search; null represents Cancel and must never be committed.
        public static bool CanApply(string result, bool sameDialog, bool inputActive) =>
            result != null && sameDialog && inputActive;
    }
}
