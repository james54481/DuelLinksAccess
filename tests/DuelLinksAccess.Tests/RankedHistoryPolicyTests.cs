using Xunit;

namespace DuelLinksAccess.Tests;

public class RankedHistoryPolicyTests
{
    [Fact]
    public void RequiresBothVisibleNameAndTimeInsteadOfSharedLastRow()
    {
        var first = new RankedHistoryPolicy.Entry("對 Player A", "10/26 11:47", "Lose", "青銅階級 5");
        var last = new RankedHistoryPolicy.Entry("對 Player B", "10/24 16:17", "Win", "青銅階級 7");
        Assert.Same(first, RankedHistoryPolicy.Match(new[] { first, last }, new[] { "對 Player A", "10/26\u00a011:47" }));
        Assert.Null(RankedHistoryPolicy.Match(new[] { first, last }, new[] { "對 Player A", "10/24 16:17" }));
        Assert.Null(RankedHistoryPolicy.Match(new[] { first, last }, new[] { "對 Player A" }));
    }

    [Fact]
    public void DuplicateNamesAreDisambiguatedByTimeAndDuplicateMatchesRejected()
    {
        var a = new RankedHistoryPolicy.Entry("對 Player", "10/26 11:47", "Lose", "5");
        var b = new RankedHistoryPolicy.Entry("對 Player", "10/25 18:54", "Win", "7");
        Assert.Same(b, RankedHistoryPolicy.Match(new[] { a, b }, new[] { b.Name, b.Time }));
        Assert.Null(RankedHistoryPolicy.Match(new[] { a, a }, new[] { a.Name, a.Time }));
        Assert.Null(RankedHistoryPolicy.Match(new[] { a, b }, new[] { a.Name, a.Time, b.Time }));
    }

    [Theory]
    [InlineData("$.PvP.bannerList.53.data.page.log.list.9", "$.PvP.bannerList.53.data.page.log.list")]
    [InlineData("$.PvP.bannerList.53.data.page.log.list.9.link", null)]
    [InlineData("$.PvP.bannerList.53.data.replay.tab.0.list.9", null)]
    [InlineData(null, null)]
    public void SharedRowPathCanIdentifyOnlyTheList(string path, string expected)
        => Assert.Equal(expected, RankedHistoryPolicy.ListPath(path));

    [Theory]
    [InlineData("Win", "ranked_history_win")]
    [InlineData("Lose", "ranked_history_loss")]
    [InlineData("Draw", "ranked_history_draw")]
    [InlineData("other", "ranked_history_unknown_result")]
    public void UnknownResultIsNotInvented(string result, string expected)
        => Assert.Equal(expected, RankedHistoryPolicy.ResultKey(result));
}
