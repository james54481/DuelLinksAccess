namespace DuelLinksAccess
{
    public static class ExtraSummonPolicy
    {
        public const int PileEntryIndex = 0;
        public static int FindVisual(int wantedUid, int[] visualUids, int[] dataIndices, int[] itemUids)
        {
            if (wantedUid <= 0 || visualUids == null || dataIndices == null || itemUids == null) return -1;
            for (int i = 0; i < visualUids.Length && i < dataIndices.Length; i++)
            {
                int data = dataIndices[i];
                if (data >= 0 && data < itemUids.Length && visualUids[i] == wantedUid
                    && itemUids[data] == wantedUid) return i;
            }
            return -1;
        }
    }
}
