namespace DuelLinksAccess
{
    public static class DailyRewardPolicy
    {
        public static bool IsCaption(string text) => text == "每日獎勵" || text == "每日奖励"
            || text == "Daily Reward" || text == "Daily Rewards";
        public static bool CanHandle(bool atHome, bool dialogOpen, bool knownCaption, bool ownsCenterHit)
            => atHome && !dialogOpen && knownCaption && ownsCenterHit;
    }
}
