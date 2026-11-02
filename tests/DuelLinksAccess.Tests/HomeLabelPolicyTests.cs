using Xunit;
namespace DuelLinksAccess.Tests;
public class HomeLabelPolicyTests
{
    [Theory]
    [InlineData("Single", "PresentButton", null, "home_gifts")]
    [InlineData("Single", "InfoButton", null, "home_info")]
    [InlineData("Single", "SelectDeck", null, "home_deck_select")]
    [InlineData("Single", "SeriesLogo", null, "home_series")]
    [InlineData("Single", "MenuButton", null, "home_character_button")]
    [InlineData("Home", "MenuButton", null, "home_return_world")]
    [InlineData("Home", "CharaList", null, "home_chara_list")]
    [InlineData("Home", "ExpUp", null, "home_level_up_button")]
    [InlineData("Home", "switch", null, "home_chara_change_style")]
    [InlineData("Home", "DeckIconBg", null, "home_deck_preview")]
    [InlineData("Gate", "ExpUp", null, null)]
    [InlineData("Home", "38", null, null)]
    [InlineData("Other", "MenuButton", null, null)]
    [InlineData("Single", "ArrowL", null, "home_area_left")]
    [InlineData("Single", "YgomButton", "GeneralInfomationButton", "home_events")]
    [InlineData("Single", "YgomButton", "Other", null)]
    [InlineData("Gate", "ArrowL", null, null)]
    [InlineData("Other", "PresentButton", null, null)]
    [InlineData("Single", "card", null, null)]
    [InlineData("Single", "MenuRightBase", null, "home_character_details")]
    public void UsesVerifiedHomeContextOnly(string view, string name, string parent, string expected)
        => Assert.Equal(expected, HomeLabelPolicy.Key(view, name, parent));
}
