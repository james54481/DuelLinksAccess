using Xunit;
namespace DuelLinksAccess.Tests;
public sealed class OrbExchangePolicyTests
{
    [Theory]
    [InlineData("30/25",true,30,25)]
    [InlineData(" 30 ／ 25 ",true,30,25)]
    [InlineData("30",true,30,-1)]
    [InlineData("0/25",true,0,25)]
    [InlineData("UNKNOWN",false,-1,-1)]
    [InlineData("30/",false,-1,-1)]
    [InlineData("-30/25",false,-1,-1)]
    public void ReadsNativeCostWithoutDependingOnUninitializedModeFields(string text,bool valid,int cost,long balance)
    {
        bool actual = OrbExchangePolicy.TryReadCostText(text,out int parsedCost,out long parsedBalance);
        Assert.Equal(valid,actual);
        if(valid) { Assert.Equal(cost,parsedCost); Assert.Equal(balance,parsedBalance); }
    }
    [Theory]
    [InlineData(30,25,5)]
    [InlineData(30,30,0)]
    [InlineData(30,40,0)]
    [InlineData(30,0,30)]
    [InlineData(-1,25,-1)]
    [InlineData(30,-1,-1)]
    [InlineData(0,25,0)]
    [InlineData(int.MaxValue,long.MaxValue,0)]
    public void ReportsInsufficientBalanceWithoutTreatingUnknownAsAvailable(int required, long owned, long expected)
        => Assert.Equal(expected, OrbExchangePolicy.Shortfall(required,owned));
}
