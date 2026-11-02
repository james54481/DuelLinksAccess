using Xunit;

namespace DuelLinksAccess.Tests
{
    public class ArenaMenuPolicyTests
    {
        [Theory]
        [InlineData("PvpMainMenu", "card", true, "$.PvP.bannerList.53", 4041, true)]
        [InlineData("PvpMainMenu", "card", true, "$.PvP.bannerList.19", 15199, true)]
        [InlineData("PvpMainMenu", "card", true, "$.PvP.bannerList.61", 4041, true)]
        [InlineData("Deck", "card", true, "$.PvP.bannerList.53", 4041, false)]
        [InlineData("PvpMainMenu", "btn", true, "$.PvP.bannerList.53", 4041, false)]
        [InlineData("PvpMainMenu", "card", false, "$.PvP.bannerList.53", 4041, false)]
        [InlineData("PvpMainMenu", "card", true, "$.PvP.bannerList.53.data", 4041, false)]
        [InlineData("PvpMainMenu", "card", true, "$.PvP.bannerList.53", 0, false)]
        public void DedupPreservesOnlyVerifiedArenaDeckEditorTargets(string screen, string child,
            bool active, string path, int cid, bool expected)
            => Assert.Equal(expected, ArenaMenuPolicy.PreferDeckEditor(screen, child, active, path, cid));

        [Theory]
        [InlineData("$.PvP.bannerList.53.data.page.reward.category.0.list.28", 0)]
        [InlineData("$.PvP.bannerList.53.data.page.reward.category.1.list.6", 1)]
        [InlineData("$.PvP.bannerList.53.data.page.reward.category.2.list.6", -1)]
        [InlineData("$.PvP.bannerList.53.data.page.reward.category.1.list.6.item", -1)]
        [InlineData("$.PvP.bannerList.53.data.page.log.list.9", -1)]
        [InlineData(null, -1)]
        public void RewardPathsRejectOtherCategoriesAndMatchHistory(string path, int expected)
            => Assert.Equal(expected, ArenaMenuPolicy.RewardCategory(path));

        [Theory]
        [InlineData("$.PvP.bannerList.53.data.page.button.duel", true)]
        [InlineData("$.PvP.bannerList.53.data.page.button.rb", false)]
        [InlineData("$.PvP.bannerList.53.data.replay.button.duel", false)]
        [InlineData("$.PvP.bannerList.53.data.page.button.duel.extra", false)]
        public void DuelActionDoesNotRelabelOtherControls(string path, bool expected)
            => Assert.Equal(expected, ArenaMenuPolicy.IsDuelButton(path));

        [Fact]
        public void RankedRewardResourcesPreserveGameItemNamesAndParameters()
        {
            var catalog = LocalizationCatalog.Load("zh-TW");
            Assert.Equal("累積獎勵，累積勝利 27 次：寶石，數量 5",
                catalog.Get("ranked_cumulative_reward", catalog.Get("ranked_wins_threshold", 27), "寶石", 5));
            Assert.Equal("階級提升獎勵，青銅階級 5：寶石，數量 10",
                catalog.Get("ranked_rank_reward", "青銅階級 5", "寶石", 10));
        }

        [Theory]
        [InlineData("GUI_T_ComButton60_0", "arena_replay_lock")]
        [InlineData("GUI_T_ComButton61_0", "arena_replay_unlock")]
        [InlineData("GUI_T_ComButton61_01", null)]
        [InlineData("GUI_T_ComButton61_1", null)]
        [InlineData("GUI_T_Icon_Lock", null)]
        public void LockActionUsesOnlyVerifiedStates(string sprite, string expected)
            => Assert.Equal(expected, ArenaMenuPolicy.ReplayLockActionKey(new[] { sprite }));

        [Fact]
        public void ConflictingLockStatesAreNeverGuessed()
        {
            Assert.Null(ArenaMenuPolicy.ReplayLockActionKey(new[] { "GUI_T_ComButton60_0", "GUI_T_ComButton61_0" }));
            Assert.Equal("arena_replay_unlock", ArenaMenuPolicy.ReplayLockActionKey(new[] { "background", "GUI_T_ComButton61_0", "GUI_T_ComButton61_0" }));
        }

        [Theory]
        [InlineData("GUI_T_Icon_Lock", true)]
        [InlineData("GUI_T_ComButton60_0", true)]
        [InlineData("GUI_T_ComButton60_01", false)]
        [InlineData("GUI_T_ComButton60_1", false)]
        [InlineData("GUI_T_Icon_Unlock01", true)]
        [InlineData("GUI_T_Icon_Lock_On", true)]
        [InlineData("GUI_T_Icon_Clock", false)]
        [InlineData("GUI_T_Icon_Block", false)]
        [InlineData("GUI_T_Icon_Locksmith", false)]
        public void LockIconRequiresACompleteSemanticToken(string sprite, bool expected)
            => Assert.Equal(expected, ArenaMenuPolicy.HasLockIcon(new[] { sprite }));

        [Fact]
        public void ImageTitlesRequireExactVerifiedSpriteAndRejectConflicts()
        {
            Assert.Equal("arena_tournament", ArenaMenuPolicy.SpriteTitleKey(new[] { "GUI_T_ModeSelect_PVPTitle12" }));
            Assert.Equal("arena_kc_points", ArenaMenuPolicy.SpriteTitleKey(new[] { "GUI_T_ModeSelect_PVPTitle11" }));
            Assert.Null(ArenaMenuPolicy.SpriteTitleKey(new[] { "GUI_T_ModeSelect_PVPTitle120" }));
            Assert.Null(ArenaMenuPolicy.SpriteTitleKey(new[] { "GUI_T_ModeSelect_PVPTitle12", "GUI_T_ModeSelect_PVPTitle11" }));
        }

        [Theory]
        [InlineData("$.PvP.bannerList.52.data.replay.tab.0.list.2", true)]
        [InlineData("$.PvP.bannerList.52.data.replay.tab.0.list.2.playerR", false)]
        [InlineData("$.PvP.bannerList.52", false)]
        [InlineData(null, false)]
        public void ReplayContextIsRestrictedToExactNativeRows(string path, bool expected)
            => Assert.Equal(expected, ArenaMenuPolicy.IsReplayRow(path));

        [Theory]
        [InlineData("和好友相互切磋！", -1, "", false, "arena_friend")]
        [InlineData("重新觀看最喜愛的決鬥吧！", -1, "", false, "arena_replays")]
        [InlineData("和各地決鬥好手即時對戰！", -1, "", false, "arena_casual")]
        [InlineData("一起決鬥吧！", -1, "", false, "arena_room")]
        [InlineData("你按區域劃分的對戰結果", -1, "", false, "arena_regional")]
        [InlineData("賽季期間：10/1", 1, "青銅階級", false, "arena_rank_speed")]
        [InlineData("賽季期間：10/1", 2, "新手階級", false, "arena_rank_rush")]
        [InlineData("", 1, "", true, "arena_character_deck")]
        [InlineData("unverified banner", 1, "", false, null)]
        [InlineData("", 9, "rank", false, null)]
        [InlineData("", -1, "", false, null)]
        public void ResolvesVerifiedTitlesAndRejectsUnidentifiedBanners(string description,
            int rule, string rank, bool characterDeck, string expected)
            => Assert.Equal(expected, ArenaMenuPolicy.TitleKey(description, rule, rank, characterDeck));

        [Theory]
        [InlineData("$.PvP.bannerList.53", true)]
        [InlineData("$.PvP.bannerList.0", true)]
        [InlineData("$.PvP.bannerList.53.data", false)]
        [InlineData("$.PvP.bannerList.-1", false)]
        [InlineData("$.Other.bannerList.53", false)]
        [InlineData(null, false)]
        [InlineData("$.PvP.bannerList.", false)]
        public void OnlyAcceptsExactBannerRows(string path, bool expected)
            => Assert.Equal(expected, ArenaMenuPolicy.IsBannerPath(path));

        [Fact]
        public void RevealMovesInCorrectDirectionAndLeavesVisibleRowsAlone()
        {
            Assert.Equal(0, ArenaMenuPolicy.ScrollCorrection(20, 80, 0, 100, 400));
            Assert.Equal(-0.05f, ArenaMenuPolicy.ScrollCorrection(-20, 40, 0, 100, 400));
            Assert.Equal(0.05f, ArenaMenuPolicy.ScrollCorrection(60, 120, 0, 100, 400));
            Assert.Equal(0, ArenaMenuPolicy.ScrollCorrection(-20, 40, 0, 100, 0));
        }
    }
}
