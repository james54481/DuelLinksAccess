using Xunit;
namespace DuelLinksAccess.Tests;
public class EventDetailTextTests
{
    [Theory]
    [InlineData("遊戲玩法", true)]
    [InlineData("推薦獎勵", true)]
    [InlineData("▼已獲得", false)]
    [InlineData("關閉", false)]
    [InlineData(null, false)]
    public void ExpansionAndCloseButtonsAreNotTabs(string text, bool expected)
        => Assert.Equal(expected, EventDetailText.IsTabLabel(text));
    [Fact]
    public void KeepsRewardConditionsAndNamesInNativeOrder()
        => Assert.Equal(new[] { "[完成區域5]", "技能「照亮我道路的命運抽牌」", "①：抽2張牌。" },
            EventDetailText.Paragraphs("[完成區域5]\r\n技能「照亮我道路的命運抽牌」\n\n①：抽2張牌。"));
    [Fact]
    public void EmptyContentAddsNoNavigationStop()
        => Assert.Empty(EventDetailText.Paragraphs(" \r\n\t"));
    [Fact]
    public void DoesNotRewriteNativeCardNamesOrNumbers()
        => Assert.Equal(new[] { "槍管裝填超充能龍 ×1", "11/24/2026 12:59截止" },
            EventDetailText.Paragraphs("槍管裝填超充能龍 ×1\r11/24/2026 12:59截止"));
}
