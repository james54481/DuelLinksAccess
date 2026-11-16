using Xunit;
namespace DuelLinksAccess.Tests;
public class DeckFilterPolicyTests
{
    [Theory]
    [InlineData("minAtk", "最低攻擊力")]
    [InlineData("maxAtk", "最高攻擊力")]
    [InlineData("minDef", "最低守備力")]
    [InlineData("maxDef", "最高守備力")]
    public void NativeNumericFieldsHaveDistinctTranslatedMeaning(string field, string expected)
    {
        Assert.Equal(expected, LocalizationCatalog.Load("zh-TW").Get(DeckFilterPolicy.NumericKey(field)));
    }
    [Theory]
    [InlineData("青眼白龍")]
    [InlineData("attack")]
    [InlineData(null)]
    public void UnverifiedFieldsAreNeverRelabeled(string field) => Assert.Null(DeckFilterPolicy.NumericKey(field));
    [Theory]
    [InlineData("7", true, false, "等級／階級／連結：7")]
    [InlineData("7", false, true, "鐘擺刻度：7")]
    [InlineData("0", false, true, "鐘擺刻度：0")]
    public void SameNumberIsIdentifiedByNativeGroup(string text, bool level, bool scale, string expected)
    {
        Assert.Equal(expected, LocalizationCatalog.Load("zh-TW").Get(DeckFilterPolicy.ChoiceGroupKey(text, level, scale), text));
    }
    [Theory]
    [InlineData("7", false, false)]
    [InlineData("7", true, true)]
    [InlineData("SR", true, false)]
    [InlineData(null, false, true)]
    public void UnknownOrAmbiguousGroupsRemainUnchanged(string text, bool level, bool scale)
    {
        Assert.Null(DeckFilterPolicy.ChoiceGroupKey(text, level, scale));
    }

    [Fact]
    public void CurrentValueAndTabStateAreReported()
    {
        var loc = LocalizationCatalog.Load("zh-TW");
        Assert.Equal("最低攻擊力，目前值：1500", loc.Get("deck_filter_number", loc.Get("deck_filter_atk_min"), "1500"));
        Assert.Equal("怪獸，已選取分頁", loc.Get("deck_filter_tab_selected", "怪獸"));
        Assert.NotEqual(loc.Get("deck_filter_tab", "怪獸"), loc.Get("deck_filter_tab_selected", "怪獸"));
    }
}
