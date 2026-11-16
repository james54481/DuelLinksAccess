namespace DuelLinksAccess
{
    // james54481, 2026-11-06: ChainSet's numUid packs UID in the high word.
    public static class DuelChainPolicy
    {
        public static int? DecodeLink(int numUid)
        {
            int link = numUid & 0xffff;
            return link > 0 ? link : null;
        }
    }
}
