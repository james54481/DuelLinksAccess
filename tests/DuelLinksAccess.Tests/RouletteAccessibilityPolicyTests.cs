using Xunit;
namespace DuelLinksAccess.Tests;
public class RouletteAccessibilityPolicyTests
{
    [Theory]
    [InlineData(true, 2, 3, 2)]
    [InlineData(true, 0, 3, 0)]
    [InlineData(false, 2, 3, null)]
    [InlineData(true, -1, 3, null)]
    [InlineData(true, 3, 3, null)]
    [InlineData(true, 0, 0, null)]
    public void ResultsAreOnlyResolvedAfterStoppingAndWithinListBounds(bool ready, long result, int count, int? expected)
        => Assert.Equal(expected, RouletteAccessibilityPolicy.ResultIndex(ready, result, count));
    [Theory]
    [InlineData(0, 4, 4L)]
    [InlineData(2, 6, 12L)]
    [InlineData(3, 6, 18L)]
    [InlineData(-1, 6, null)]
    [InlineData(1, 6, null)]
    [InlineData(99, 6, null)]
    [InlineData(2, -1, null)]
    public void UnknownBonusOrMissingStepsDoNotBecomeNormalResults(long bonus, long steps, long? expected)
        => Assert.Equal(expected, RouletteAccessibilityPolicy.ResultSteps(steps, RouletteAccessibilityPolicy.ResultMultiplier(bonus)));
    [Fact]
    public void OverflowRemainsUnknown()
        => Assert.Null(RouletteAccessibilityPolicy.ResultSteps(long.MaxValue, 3));
    [Theory]
    [InlineData(false, true, false, "ready")]
    [InlineData(false, true, true, "spinning")]
    [InlineData(true, false, true, "duel")]
    [InlineData(true, true, true, "duel")]
    [InlineData(false, false, false, "unknown")]
    [InlineData(false, false, true, "unknown")]
    public void DuelResultTakesPriorityOverCachedStartValues(bool duel, bool cost, bool spinning, string expected)
        => Assert.Equal(expected, RouletteAccessibilityPolicy.Phase(duel, cost, spinning));
    [Theory]
    [InlineData("Event.Roulette.dialog.level_select.vals.buttonlist.0", true)]
    [InlineData("Event.Roulette.dialog.level_select.vals.buttonlist.1", true)]
    [InlineData("Event.Roulette.dialog.level_select.vals.buttonlist.0.group", false)]
    [InlineData("Event.Other.dialog.level_select.vals.buttonlist.0", false)]
    [InlineData(null, false)]
    public void OnlyVerifiedDifficultyRowsBypassLayoutFilter(string path, bool expected)
        => Assert.Equal(expected, RouletteAccessibilityPolicy.IsDifficultyRow(path));
    [Theory]
    [InlineData("7。/。459", 7, 459)]
    [InlineData("0 / 0", 0, 0)]
    [InlineData("12 / 1,459", 12, 1459)]
    [InlineData("7／3", 7, 3)]
    public void SeparatesRequiredAndOwnedWithoutInferringAffordability(string text, int required, int owned)
    {
        Assert.True(RouletteAccessibilityPolicy.TryCost(text, out int a, out int b));
        Assert.Equal(required, a);
        Assert.Equal(owned, b);
    }
    [Theory]
    [InlineData("7")]
    [InlineData("7/459/12")]
    [InlineData("-1/459")]
    [InlineData("7/1,45")]
    [InlineData("7/9999999999999999999")]
    [InlineData(null)]
    public void MalformedOrMissingValuesAreNotReportedAsZero(string text)
        => Assert.False(RouletteAccessibilityPolicy.TryCost(text, out _, out _));
    [Theory]
    [InlineData("btn", "Event.Roulette.data.map", "Event.Roulette.data.lot", "7。/。459", true)]
    [InlineData("btn", "Event.Roulette.data.map", "Event.Roulette.data.lot", "12 / 100", true)]
    [InlineData("ButtonSet", "Event.Roulette.data.map", "Event.Roulette.data.lot", "7。/。459", false)]
    [InlineData("btn", "Other.map", "Event.Roulette.data.lot", "7。/。459", false)]
    [InlineData("btn", "Event.Roulette.data.map", "Event.Roulette.data.lot", "活動劇情", false)]
    public void StartLabelRequiresTheObservedWheelContext(string name, string map, string lot, string text, bool expected)
        => Assert.Equal(expected, RouletteAccessibilityPolicy.IsStartControl(name, map, lot, text));
    [Theory]
    [InlineData("Event.Roulette.data.map.list.0", 1)]
    [InlineData("Event.Roulette.data.map.list.27", 28)]
    [InlineData("Event.Roulette.data.map", -1)]
    [InlineData("Event.Roulette.data.lot.list.0", -1)]
    [InlineData("Event.Roulette.data.map.list.2.unit", -1)]
    [InlineData(null, -1)]
    public void OnlyMapCellsHaveRoutePositions(string path, int expected)
        => Assert.Equal(expected, RouletteAccessibilityPolicy.MapPosition(path));
    [Theory]
    [InlineData("balloon:msg?target=Item20&text=場景4", "場景4")]
    [InlineData("balloon:msg?text=%E5%A0%B4%E6%99%AF3&target=Item12", "場景3")]
    [InlineData("https://example.com?text=wrong", null)]
    [InlineData("balloon:msg?target=Item20", null)]
    public void ReadsOnlyNativeBalloonDisplayText(string link, string expected)
        => Assert.Equal(expected, RouletteAccessibilityPolicy.BalloonText(link));
}
