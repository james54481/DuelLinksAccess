namespace DuelLinksAccess
{
    // james54481, 2026-11-06: a general card animation is not proof of a reveal.
    public static class DuelFlipPolicy
    {
        public static bool CanAnnounce(string eventName, bool local, int locate,
            int expectedUid, int actualUid, bool faceUp)
        {
            bool monster = locate == 1 || locate == 2 || locate == 3 || locate == 5 || locate == 6;
            return eventName == "CutinFlip" && local && monster && expectedUid > 0
                && expectedUid == actualUid && faceUp;
        }
    }
}
