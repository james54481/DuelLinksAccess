using Xunit;
namespace DuelLinksAccess.Tests;
public class AlbumTextPolicyTests
{
    [Theory]
    [InlineData("▼主劇情", "主劇情")]
    [InlineData("▼決鬥", "決鬥")]
    [InlineData("▶傳奇決鬥者已解鎖。", "傳奇決鬥者已解鎖。")]
    public void CategoryUsesItsOwnNativeLabel(string nativeLabel, string expected)
        => Assert.Equal(expected, AlbumTextPolicy.Category(nativeLabel));
    [Fact] public void CharacterTagsPreserveNativeNames()
        => Assert.Equal("闇遊戲VS海馬瀨人", AlbumTextPolicy.Title("<chara id='1' get='name'/>VS<chara id='2' get='name'/>", id => id == 1 ? "闇遊戲" : "海馬瀨人"));
    [Fact] public void UnknownNamesAreExplicit()
        => Assert.Equal("獲得UNKNOWN", AlbumTextPolicy.Title("獲得<chara id='9' get='name'/>", _ => null));
    [Fact] public void OrdinaryTitleIsUnchanged()
        => Assert.Equal("歡迎來到Duel Links", AlbumTextPolicy.Title("歡迎來到Duel Links", _ => null));
    [Fact] public void VisibleCostsKeepRequiredAndHeldOrder()
    { Assert.True(AlbumTextPolicy.Cost("費用 10,000/868,869", out var cost, out var held)); Assert.Equal(10000, cost); Assert.Equal(868869, held); }
    [Theory] [InlineData("")] [InlineData("10000")] [InlineData("UNKNOWN/100")]
    public void MissingCostsAreNotInvented(string text) => Assert.False(AlbumTextPolicy.Cost(text, out _, out _));
    [Fact] public void OnlyExactPlayableRowsQualify()
    {
        Assert.True(AlbumTextPolicy.IsPlayPath("Album.data.slist.0.scenario.0.list.2"));
        Assert.False(AlbumTextPolicy.IsPlayPath("Album.data.slist.0"));
        Assert.False(AlbumTextPolicy.IsPlayPath("Album.data.slist.0.scenario.0.list.2.extra"));
        Assert.True(AlbumTextPolicy.IsCategoryPath("Album.data.slist.0"));
    }
}
