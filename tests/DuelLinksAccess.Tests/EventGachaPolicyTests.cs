using Xunit;
namespace DuelLinksAccess.Tests;
public class EventGachaPolicyTests
{
    [Theory]
    [InlineData(1, 0, true)]
    [InlineData(1, 2, false)]
    [InlineData(2, 0, false)]
    [InlineData(1, -1, false)]
    public void SingleReceiptOmittedCounterIsNotZeroRewards(int rows, long copies, bool expected)
        => Assert.Equal(expected, EventGachaPolicy.IsSingleQuantityReceipt(rows, copies));
    [Theory]
    [InlineData(true, true, false, false, true)]
    [InlineData(false, true, false, false, false)]
    [InlineData(true, false, false, false, false)]
    [InlineData(true, true, true, false, false)]
    [InlineData(true, true, false, true, false)]
    public void ContinuationCannotActivateAnotherDrawOrPreview(bool pending, bool samePage, bool action, bool card, bool expected)
        => Assert.Equal(expected, EventGachaPolicy.CanAcknowledge(pending, samePage, action, card));
    [Theory]
    [InlineData("R寶玉 ×3", "R寶玉", 3)]
    [InlineData("EX寶玉（決鬥者之路） ×1", "EX寶玉（決鬥者之路）", 1)]
    public void ReadsNativePerRewardQuantity(string text, string name, long units)
    {
        Assert.True(EventGachaPolicy.TryUnitLabel(text, out var actualName, out long actualUnits));
        Assert.Equal(name, actualName);
        Assert.Equal(units, actualUnits);
    }
    [Fact]
    public void MissingPerRewardQuantityStaysUnknown()
        => Assert.False(EventGachaPolicy.TryUnitLabel("R寶玉", out _, out _));
    [Theory]
    [InlineData(1, 2, 2L)]
    [InlineData(3, 1, 3L)]
    [InlineData(3, 2, 6L)]
    [InlineData(-1, 2, null)]
    [InlineData(3, -1, null)]
    public void TotalIsPerRewardTimesNumberOfRewards(long units, long copies, long? total)
        => Assert.Equal(total, EventGachaPolicy.TotalUnits(units, copies));
    [Fact]
    public void OverflowIsNotAnInventedTotal()
        => Assert.Null(EventGachaPolicy.TotalUnits(long.MaxValue, 2));
    [Theory]
    [InlineData("DialogGet.get_data.item.0", true)]
    [InlineData("EventGacha.state.button.gachaR", false)]
    [InlineData("DialogGet.get_data.item.0.iid", false)]
    public void ReceiptRowsDoNotMatchDrawActions(string path, bool expected)
        => Assert.Equal(expected, EventGachaPolicy.IsReceiptRow(path));
    [Fact]
    public void ZeroOwnedCardsIsRetainedAndRepeatedTextComponentsAreDeduplicated()
        => Assert.Equal("0", EventGachaPolicy.UniqueQuantity(new[] { "槍管裝填超充能龍", "0", "0" }));
    [Fact]
    public void AmbiguousCountsAreNotGuessed()
        => Assert.Equal("UNKNOWN", EventGachaPolicy.UniqueQuantity(new[] { "1", "30" }));
    [Theory]
    [InlineData("EventGacha.htjson.item_list.cwork.tab.0.reward.list.0.group.0.list.2", true)]
    [InlineData("EventGacha.state.button.gachaL", false)]
    [InlineData("EventGacha.state.result.list.2", false)]
    [InlineData("EventGacha.htjson.item_list.cwork.tab.0.reward.list.0.group.0.list.2.id", false)]
    public void OnlyPossibleRewardRowsAreRebuilt(string path, bool expected)
        => Assert.Equal(expected, EventGachaPolicy.IsRewardRow(path));
    [Theory]
    [InlineData(10, 0, true)]
    [InlineData(1, 30, true)]
    [InlineData(0, 0, false)]
    [InlineData(10, -1, false)]
    public void MissingCostIsNotTreatedAsFree(long number, long cost, bool expected)
        => Assert.Equal(expected, EventGachaPolicy.ValidDraw(number, cost));
    [Theory]
    [InlineData("1170", true)]
    [InlineData("1,170", true)]
    [InlineData("30。0", false)]
    [InlineData("消費硬幣: 30", false)]
    public void BalanceRequiresOneUnambiguousNumber(string text, bool expected)
        => Assert.Equal(expected, EventGachaPolicy.IsQuantity(text));
    [Fact]
    public void HeaderSelectionIsNotMistakenForAnAction()
        => Assert.False(EventGachaPolicy.IsDrawPath("EventGacha.state"));
}
