using Xunit;
namespace DuelLinksAccess.Tests;
public class ArenaCasualBannerTests
{
    [Fact] public void ExpandedCasualBannerDescribesCollapse()
        => Assert.Equal("arena_casual_collapse", ArenaMenuPolicy.CasualBannerKey(true, true));
    [Theory] [InlineData(true, false)] [InlineData(false, true)] [InlineData(false, false)]
    public void CollapsedOrOtherModesKeepTheirOwnNames(bool casual, bool expanded)
        => Assert.Null(ArenaMenuPolicy.CasualBannerKey(casual, expanded));
}
