using Xunit;
namespace DuelLinksAccess.Tests;
public sealed class ResultRewardCatalogTests
{
    private const string Row = "DuelResult.data.list.1.reward.list.0";
    [Fact]
    public void DuplicateTemplateNodesDoNotTripleOneReward()
    {
        var rows = new ResultRewardCatalog();
        for (int i = 0; i < 3; i++) rows.Add(Row, "item", 100, 8, _ => "傳送門鑰匙", "UNKNOWN");
        Assert.Equal(new[] {"傳送門鑰匙 ×8"}, rows.Labels);
    }
    [Fact]
    public void SeparateBoxesWithIdenticalItemsAreAllRetained()
    {
        var rows = new ResultRewardCatalog();
        for (int i = 0; i < 3; i++) rows.Add($"DuelResult.data.list.1.reward.list.{i}", "item", 100, 8,
            _ => "傳送門鑰匙", "UNKNOWN");
        Assert.Equal(new[] {"傳送門鑰匙 ×8","傳送門鑰匙 ×8","傳送門鑰匙 ×8"}, rows.Labels);
    }
    [Fact]
    public void NativeColorNamesArePreservedWithoutIdOrSpriteGuessing()
    {
        var rows = new ResultRewardCatalog();
        rows.Add(Row, "item", 999, 4, _ => "藍色傳送門鑰匙", "UNKNOWN");
        Assert.Equal(new[] {"藍色傳送門鑰匙 ×4"}, rows.Labels);
    }
    [Theory]
    [InlineData("DuelResult.data.list.0.reward", "item", 100, 8)]
    [InlineData("Chara.1.itemDic.39", "item", 100, 8)]
    [InlineData(Row, "card", 100, 8)]
    [InlineData(Row, "item", 0, 8)]
    [InlineData(Row, "item", 100, -1)]
    public void UnverifiedRewardDataDoesNotSuppressNativeText(string path, string type, int id, int count)
    {
        var rows = new ResultRewardCatalog();
        rows.Add(path, type, id, count, _ => "name", "UNKNOWN");
        Assert.Empty(rows.Labels);
        Assert.False(rows.Contains(path));
    }
    [Fact]
    public void LookupFailureKeepsQuantityAndExplicitUnknownName()
    {
        var rows = new ResultRewardCatalog();
        rows.Add(Row, "item", 100, 8, _ => throw new System.Exception(), "UNKNOWN");
        Assert.Equal(new[] {"UNKNOWN ×8"}, rows.Labels);
    }
    [Fact]
    public void ConflictingCopiesOfOneRowAreRejectedUntilNextSnapshot()
    {
        var rows = new ResultRewardCatalog();
        rows.Add(Row,"item",100,8,_=>"name","UNKNOWN");
        rows.Add(Row,"item",101,8,_=>"different","UNKNOWN");
        rows.Add(Row,"item",100,8,_=>"name","UNKNOWN");
        Assert.Empty(rows.Labels);
        Assert.False(rows.Contains(Row));
    }
    [Fact]
    public void CardRewardUsesCardLookupAndKeepsQuantity()
    {
        var rows = new ResultRewardCatalog();
        rows.AddCard(Row, 123, 2, id => id == 123 ? "偽陷阱" : "WRONG", "UNKNOWN");
        Assert.Equal(new[]{"偽陷阱 ×2"}, rows.Labels);
    }
    [Fact]
    public void RepeatedCardWidgetsDoNotDuplicateOneDrop()
    {
        var rows = new ResultRewardCatalog();
        for(int i=0;i<3;i++) rows.AddCard(Row,123,1,_=>"魔菌","UNKNOWN");
        Assert.Equal(new[]{"魔菌 ×1"}, rows.Labels);
    }
    [Fact]
    public void SeparateBoxesMayDropTheSameCard()
    {
        var rows = new ResultRewardCatalog();
        rows.AddCard(Row,123,1,_=>"魔菌","UNKNOWN");
        rows.AddCard("DuelResult.data.list.1.reward.list.1",123,1,_=>"魔菌","UNKNOWN");
        Assert.Equal(new[]{"魔菌 ×1","魔菌 ×1"}, rows.Labels);
    }
    [Fact]
    public void CardDropCannotComeFromPossibleRewardsOrFutureLevelUps()
    {
        var rows = new ResultRewardCatalog();
        rows.AddCard("Chara.1.itemDic.39",123,1,_=>"name","UNKNOWN");
        rows.AddCard("NpcReward.cardList.0",123,1,_=>"name","UNKNOWN");
        rows.AddCard(Row,123,0,_=>"name","UNKNOWN");
        Assert.Empty(rows.Labels);
    }
    [Fact]
    public void ConflictingItemAndCardTypesRejectTheRow()
    {
        var rows = new ResultRewardCatalog();
        rows.Add(Row,"item",123,1,_=>"item","UNKNOWN");
        rows.AddCard(Row,123,1,_=>"card","UNKNOWN");
        Assert.Empty(rows.Labels);
    }
    [Fact]
    public void UnknownCardKeepsExplicitUnknownAndActualCount()
    {
        var rows = new ResultRewardCatalog();
        rows.AddCard(Row,123,1,_=>throw new System.Exception(),"UNKNOWN");
        Assert.Equal(new[]{"UNKNOWN ×1"},rows.Labels);
    }
}
