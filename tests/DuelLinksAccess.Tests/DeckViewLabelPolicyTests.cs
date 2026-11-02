using Xunit;

namespace DuelLinksAccess.Tests;

public class DeckViewLabelPolicyTests
{
    [Theory]
    [InlineData("DeckDetail_PC", "HomeButton", "deck_view_home")]
    [InlineData("DeckDetail_PC", "BackButton", "screen_back")]
    [InlineData("DeckDetail_PC", "SkillInfoButton", "deck_view_skill_info")]
    [InlineData("DeckDetail_PC", "FooterButton(Clone)", null)]
    [InlineData("DeckEdit2", "SkillInfoButton", null)]
    [InlineData("PvpMainMenu", "HomeButton", null)]
    public void OnlyLabelsVerifiedViewControls(string view, string button, string expected)
        => Assert.Equal(expected, DeckViewLabelPolicy.ButtonKey(view, button));

    [Fact]
    public void DoesNotCallAReadOnlyViewAnEditorOrRenameActualEditors()
    {
        Assert.Equal("screen_deck_view", DeckViewLabelPolicy.ScreenKey("DeckDetail_PC"));
        Assert.Null(DeckViewLabelPolicy.ScreenKey("DeckEdit2"));
        var catalog = LocalizationCatalog.Load("zh-TW");
        Assert.Equal("查看牌組", catalog.Get(DeckViewLabelPolicy.ScreenKey("DeckDetail_PC")));
        Assert.Equal("技能資訊", catalog.Get(DeckViewLabelPolicy.ButtonKey("DeckDetail_PC", "SkillInfoButton")));
    }
}
