using System.Linq;
using Xunit;
namespace DuelLinksAccess.Tests;
public class DeckSelectionPolicyTests
{
    [Theory]
    [InlineData("Dark Magician the Magician of Black Magic", "Dark Magi...", "Dark Magician the Magician of Black Magic")]
    [InlineData("黑魔導牌組", "黑魔…", "黑魔導牌組")]
    [InlineData(null, "牌組", "牌組")]
    [InlineData(" ", "視覺名稱", "視覺名稱")]
    public void NamesPreferFullGameDataWithoutTranslation(string data, string visible, string expected)
        => Assert.Equal(expected, DeckSelectionPolicy.FullName(data, visible));
    [Theory]
    [InlineData(true, false, false, 1, true)]
    [InlineData(true, false, true, -1, false)]
    [InlineData(true, false, true, 1, false)]
    [InlineData(true, true, false, 1, false)]
    [InlineData(false, false, false, 1, false)]
    [InlineData(true, false, false, -1, false)]
    public void OnlyOwnedCharacterDecksReceiveSlotNumbers(bool info, bool purchase, bool structure, int owner, bool expected)
        => Assert.Equal(expected, DeckSelectionPolicy.UsesCharacterOrdinal(info, purchase, structure, owner));
    [Theory]
    [InlineData(10, 30, 0, 100, 200, 0)]
    [InlineData(-20, 0, 0, 100, 200, -0.1f)]
    [InlineData(100, 120, 0, 100, 200, 0.1f)]
    [InlineData(100, 120, 0, 100, 0, 0)]
    public void RevealsHiddenEdgesButLeavesVisibleTargetsAndNonScrollableListsAlone(float min, float max, float vmin, float vmax, float travel, float expected)
        => Assert.Equal(expected, DeckSelectionPolicy.RevealOffset(min, max, vmin, vmax, travel), 5);

    [Theory]
    [InlineData(0, 100, -10, 20, 20)]
    [InlineData(0, 100, -30, -10, 0)]
    [InlineData(0, 100, 110, 130, 0)]
    public void FooterClipsOnlyItsOverlappingArea(float min, float max, float fmin, float fmax, float expected)
        => Assert.Equal(expected, DeckSelectionPolicy.VisibleBottom(min, max, fmin, fmax));
    [Theory]
    [InlineData("等級:7", 7)]
    [InlineData("等級：25", 25)]
    [InlineData("LV: 43", 43)]
    [InlineData("Level 12", 12)]
    [InlineData("7", 7)]
    [InlineData("ATK 2500", null)]
    [InlineData("等級:0", null)]
    [InlineData("等級:UNKNOWN", null)]
    public void CharacterLevelUsesOnlyTheNativeLevelLabel(string text, int? expected)
        => Assert.Equal(expected, DeckSelectionPolicy.CharacterLevel(text));

    [Fact]
    public void CardInsideViewportButBehindFooterMustStillMove()
    {
        Assert.Equal(0, DeckSelectionPolicy.RevealOffset(5, 15, 0, 100, 200));
        float bottom = DeckSelectionPolicy.VisibleBottom(0, 100, -5, 20);
        Assert.True(DeckSelectionPolicy.RevealOffset(5, 15, bottom, 100, 200) < 0);
    }

    [Fact]
    public void NoFlagsMeansNoInventedStatus()
        => Assert.Empty(DeckSelectionPolicy.StatusKeys(false, false, false, false));
    [Fact]
    public void IndependentFlagsCanAllBeReported()
        => Assert.Equal(new[] { "deck_select_in_use", "deck_select_favorite", "deck_select_locked", "deck_select_warning" },
            DeckSelectionPolicy.StatusKeys(true, true, true, true).ToArray());
    [Fact]
    public void WarningDoesNotClaimAnUnverifiedReason()
        => Assert.Equal(new[] { "deck_select_warning" }, DeckSelectionPolicy.StatusKeys(false, false, false, true).ToArray());
}
