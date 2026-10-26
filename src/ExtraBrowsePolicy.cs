namespace DuelLinksAccess
{
    // james54481, 2026-10-25. Native MyselfExtra items carry DB IDs,
    // while their visual ListCard.UniqueID can legitimately be -1.
    public static class ExtraBrowsePolicy
    {
        public static bool UseViewList(bool selectionActive, bool viewActive, bool viewClosing, bool ownExtra)
            => !selectionActive && viewActive && !viewClosing && ownExtra;

        public static int CardId(bool ownExtraList, int cid, int mixedId)
        {
            if (!ownExtraList) return 0;
            if (cid > 0 && cid < 100000) return cid;
            return mixedId >= 1000 && mixedId < 100000 ? mixedId : 0;
        }

        public static bool CanRequestSummon(bool active, bool closing, bool ownExtraList,
            int uid, int cardId, bool mainPhase)
            => active && !closing && ownExtraList && uid > 0 && cardId > 0 && mainPhase;
    }
}
