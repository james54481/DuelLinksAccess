using Xunit;

namespace DuelLinksAccess.Tests;

public sealed class TradeExecutionPolicyTests
{
    [Theory]
    [InlineData(42, 42, true, true, true)]
    [InlineData(42, 43, true, true, false)]
    [InlineData(42, -1, true, true, false)]
    [InlineData(42, 42, false, true, false)]
    [InlineData(42, 42, true, false, false)]
    public void CanClick_RequiresMatchingSelectionAndUsableButton(
        int expectedItemId,
        int actualItemId,
        bool buttonActive,
        bool buttonInteractable,
        bool expected)
    {
        Assert.Equal(expected, TradeExecutionPolicy.CanClick(
            expectedItemId,
            actualItemId,
            buttonActive,
            buttonInteractable));
    }
    [Theory]
    [InlineData(true, false, true, TradeAvailability.SoldOut)]
    [InlineData(true, false, false, TradeAvailability.SoldOut)]
    [InlineData(false, true, true, TradeAvailability.Unavailable)]
    [InlineData(false, false, false, TradeAvailability.Unavailable)]
    [InlineData(false, false, true, TradeAvailability.Available)]
    [InlineData(null, false, true, TradeAvailability.Unavailable)]
    [InlineData(false, null, true, TradeAvailability.Unavailable)]
    [InlineData(false, false, null, TradeAvailability.Unavailable)]
    public void StockAndNativeRestrictionsOverrideAffordability(bool? soldout, bool? unchangeable, bool? materials, TradeAvailability expected)
        => Assert.Equal(expected, TradeExecutionPolicy.Availability(soldout, unchangeable, materials));

    [Fact]
    public void BecomingSoldOutBetweenSelectionAndConfirmationBlocksTheSecondCheck()
    {
        Assert.Equal(TradeAvailability.Available, TradeExecutionPolicy.Availability(false, false, true));
        Assert.Equal(TradeAvailability.SoldOut, TradeExecutionPolicy.Availability(true, false, true));
    }

    [Fact]
    public void EveryConfirmationPathChecksFreshStockAndOfferIdentity()
    {
        string source = System.IO.File.ReadAllText(System.IO.Path.Combine(LocalizationCatalogTests.FindRepositoryRoot(), "src", "CardTraderHandler.cs"));
        string select = source[source.IndexOf("private void SelectCurrentItem()")..source.IndexOf("private void PollTradeSelection()")];
        string poll = source[source.IndexOf("private void PollTradeSelection()")..source.IndexOf("private void CancelTradePoll()")];
        string confirm = source[source.IndexOf("private void OpenTradeConfirm(")..source.IndexOf("private void ConfirmTrade()")];
        Assert.Contains("BlockUnavailableTrade(item, checkMaterials: false)", select);
        Assert.Contains("BlockUnavailableTrade(_tradeIntendedItem, checkMaterials: true)", poll);
        Assert.Contains("SameTradeOffer(cur, _tradeIntendedItem)", poll);
        Assert.True(confirm.IndexOf("BlockUnavailableTrade") < confirm.IndexOf("_vc.OpenExchangeConfirmDialog(live)"));
        Assert.Contains("SameTradeOffer(_vc.getCurrentItem(), live)", confirm);
    }
    [Fact]
    public void MaterialsAreReadIndependentlyUsingTheirOwnIds()
    {
        var counts = new System.Collections.Generic.Dictionary<int, long> { [101] = 8, [202] = 496 };
        Assert.Equal(8L, TradeExecutionPolicy.ReadOwnedItem(101, id => counts[id]));
        Assert.Equal(496L, TradeExecutionPolicy.ReadOwnedItem(202, id => counts[id]));
    }
    [Fact]
    public void FailedQuantityLookupIsUnknownRatherThanInventedZero()
    {
        Assert.Null(TradeExecutionPolicy.ReadOwnedItem(101, _ => throw new System.Exception()));
        Assert.Null(TradeExecutionPolicy.ReadOwnedItem(101, _ => -1));
        Assert.Null(TradeExecutionPolicy.ReadOwnedItem(0, _ => 99));
        Assert.Equal(0L, TradeExecutionPolicy.ReadOwnedItem(101, _ => 0));
    }
}
