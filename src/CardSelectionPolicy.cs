namespace DuelLinksAccess
{
    /// <summary>Never substitute a display ordinal for an unverified game index.</summary>
    public static class CardSelectionPolicy
    {
        public static int ResolveDataIndex(int visualIndex, int mappedIndex, int itemCount)
            => visualIndex >= 0 && mappedIndex >= 0 && mappedIndex < itemCount
                ? mappedIndex : -1;

        public static bool CanDispatchExtra(int expectedCardId, int actualCardId,
            int expectedUid, int actualUid, int commandIndex)
            => expectedCardId > 0 && expectedCardId == actualCardId
                && expectedUid > 0 && expectedUid == actualUid && commandIndex >= 0;
    }
}
