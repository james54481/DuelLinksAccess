namespace DuelLinksAccess
{
    public static class AttackFallbackPolicy
    {
        public static bool IsBlockingCanvasHit(bool hasCanvas, int duelProviderId, int hitProviderId)
            => hasCanvas && (duelProviderId == 0 || hitProviderId == 0 || duelProviderId != hitProviderId);

        public static bool MatchesNativeMenu(int player, int locate, int index, int uid,
            int menuPlayer, int menuLocate, int menuIndex, int currentUid)
            => uid > 0 && uid == currentUid && player == menuPlayer
                && locate == menuLocate && index == menuIndex;

        public static bool IsVisibleClickPoint(float x, float y, float depth, int width, int height)
            => float.IsFinite(x) && float.IsFinite(y) && float.IsFinite(depth)
                && depth > 0 && x > 0 && y > 0 && x < width && y < height;

        public static bool CanRetryFieldTap(bool localMonster, bool inputOpen,
            int expectedId, int actualId, int expectedUid, int actualUid)
            => localMonster && inputOpen && expectedId > 0 && expectedUid > 0
                && expectedId == actualId && expectedUid == actualUid;

        public static bool CanOfferAttack(bool battleInput, bool? faceUp,
            bool? attackPosition, int targetMask)
            => battleInput && faceUp == true && attackPosition == true && targetMask != 0;

        public static bool ShouldRetry(
            bool autoAttack, bool battleInputActive)
        {
            return !autoAttack && battleInputActive;
        }
    }
}
