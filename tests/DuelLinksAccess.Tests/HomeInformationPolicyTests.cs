using Xunit;
namespace DuelLinksAccess.Tests;
public class HomeInformationPolicyTests
{
    [Theory]
    [InlineData("file://Special/Images/#/Campaign145_Banner1", "home_information_daily_dream_packs")]
    [InlineData("file://Special/Images/#/Campaign15_Banner1", "home_information_exp_bonus")]
    [InlineData("file://Temp/Event/Ev0196/Images/#/GUI_T_Banner2", "home_information_duelist_road_lottery")]
    [InlineData("file://Temp/Event/Ev0198/Images/#/GUI_T_Banner2", null)]
    [InlineData("file://Special/Images/#/Campaign999_Banner1", null)]
    public void NewArtworkUsesVerifiedNamesWithoutMatchingOtherEvents(string path, string expected)
        => Assert.Equal(expected, HomeInformationPolicy.BannerKey(path));
    [Fact]
    public void SelectedTabFollowsTheRenderedNativeList()
    {
        Assert.True(HomeInformationPolicy.SelectedTab("有用的資訊", new[] { "Information.page.top.now.list2.0" }));
        Assert.False(HomeInformationPolicy.SelectedTab("正在進行的活動", new[] { "Information.page.top.now.list2.0" }));
        Assert.True(HomeInformationPolicy.SelectedTab("正在進行的活動", new[] { "Information.page.top.now.list.0" }));
    }
    [Fact]
    public void ChangingOrEmptyListsDoNotInventASelectedTab()
    {
        Assert.Null(HomeInformationPolicy.SelectedTab("有用的資訊", System.Array.Empty<string>()));
        Assert.Null(HomeInformationPolicy.SelectedTab("有用的資訊", new[] { "Information.page.top.now.list.0", "Information.page.top.now.list2.0" }));
    }
    [Fact]
    public void RankedDuelArtworkHasItsVerifiedName()
        => Assert.Equal("home_information_ranked", HomeInformationPolicy.BannerKey("file://Images/Menu/#/GUI_T_Banner1_PVPRank2"));
    [Theory]
    [InlineData(true, false, true, false, true)]
    [InlineData(false, true, true, false, true)]
    [InlineData(false, false, true, false, false)]
    [InlineData(true, true, true, true, false)]
    [InlineData(true, false, false, false, false)]
    public void InformationBackNeverHijacksADialogOrAnUnrelatedScreen(bool requested, bool closing, bool home, bool dialog, bool expected)
        => Assert.Equal(expected, HomeInformationPolicy.BlockNativeBack(requested, closing, home, dialog));
    [Theory]
    [InlineData("Information.page.top.now.list2.0", true)]
    [InlineData("Information.page.top.now.list2.0.banner", false)]
    [InlineData("Information.page.top.now.list2", false)]
    public void UsefulInformationRowsUseTheirOwnListIdentity(string path, bool expected)
        => Assert.Equal(expected, HomeInformationPolicy.RowPath(path) != null);
    [Theory]
    [InlineData("file://Special/Images/#/Convenient_Banner1", "home_information_useful_features")]
    [InlineData("file://Images/Menu/#/GUI_T_KCreport_Banner1", "home_information_kc_report")]
    [InlineData("file://Special/Images/#/SeriesIntro_Banner1", "home_information_series_intro")]
    [InlineData("file://Unseen_Banner1", null)]
    [InlineData("file://Special/Images/#/Campaign142_Banner1", "home_information_anniversary_countdown")]
    [InlineData("file://Special/Campaign130/Images/#/Campaign130_Banner1", "home_information_ninth_anniversary")]
    [InlineData("file://Images/Menu/#/GUI_T_Banner1_PVPRankCard", "home_information_ticket_exchange")]
    public void ImageNamesRequireVerifiedArtwork(string path, string expected)
        => Assert.Equal(expected, HomeInformationPolicy.BannerKey(path));
    [Fact]
    public void ParentAndImageOfOneActivityShareIdentity()
        => Assert.Equal(HomeInformationPolicy.ItemKey("Information.page.top.now.list.0", 1),
            HomeInformationPolicy.ItemKey("Information.page.top.now.list.0", 2));
    [Fact]
    public void DifferentRowsAreNeverMergedByTheirSharedTitle()
        => Assert.NotEqual(HomeInformationPolicy.ItemKey("Information.page.top.now.list.0", 1),
            HomeInformationPolicy.ItemKey("Information.page.top.now.list.1", 1));
    [Fact]
    public void ACardPreviewIsNotMergedIntoTheActivityLink()
        => Assert.NotEqual(HomeInformationPolicy.ItemKey("Information.page.top.now.list.0", 1, "open"),
            HomeInformationPolicy.ItemKey("Information.page.top.now.list.0", 2, "card:3"));
    [Theory]
    [InlineData(null, 13, true)]
    [InlineData(null, 0, false)]
    [InlineData("Information.page.top.now.list.0", 1, false)]
    public void ListBodyIsNotAnActivity(string row, int children, bool expected)
        => Assert.Equal(expected, HomeInformationPolicy.SkipContainer(row, children));
    [Theory]
    [InlineData("Information.page.top.now.list.0", true)]
    [InlineData("Information.page.top.useful.list.12", true)]
    [InlineData("Information.page.top.now", false)]
    [InlineData("Information.page.top.now.list.0.title", false)]
    [InlineData("Other.list.0", false)]
    public void OnlyAnExactNativeInformationRowIsAnIdentity(string path, bool expected)
        => Assert.Equal(expected, HomeInformationPolicy.RowPath(path) != null);
    [Theory]
    [InlineData("正在進行的活動", true)]
    [InlineData("有用的資訊", true)]
    [InlineData("Useful Information", true)]
    [InlineData("過去的活動", false)]
    [InlineData("活動道具：數據資料", false)]
    [InlineData(null, false)]
    public void OnlyVerifiedTabTitlesUseTabRefresh(string title, bool expected)
        => Assert.Equal(expected, HomeInformationPolicy.IsTabTitle(title));
    [Theory]
    [InlineData(true, "Single", "BannerList", true)]
    [InlineData(false, "Single", "BannerList", false)]
    [InlineData(true, "PvpMainMenu", "BannerList", false)]
    [InlineData(true, "Single", "Other", false)]
    [InlineData(true, "Single", null, false)]
    public void OwnershipRequiresAnExplicitHomeInformationRequest(bool request, string content, string panel, bool expected)
        => Assert.Equal(expected, HomeInformationPolicy.CanOwn(request, content, panel));

    [Theory]
    [InlineData(910, 100, 1280, 700, true)]
    [InlineData(1281, 100, 1650, 700, false)]
    [InlineData(-500, 100, -100, 700, false)]
    [InlineData(1260, 100, 1650, 700, false)]
    [InlineData(910, 720, 1280, 1080, false)]
    public void CollapsedAndClippedPanelsAreNotNavigated(float left, float bottom, float right, float top, bool expected)
        => Assert.Equal(expected, HomeInformationPolicy.HasVisibleArea(left, bottom, right, top, 1280, 720));
}
