using Xunit;
namespace DuelLinksAccess.Tests;
public class ChronicleItemPolicyTests
{
    [Theory]
    [InlineData("4/4",-1,-1,true,4,4)]
    [InlineData("0/1",-1,-1,true,0,1)]
    [InlineData(" 3 / 4 ",-1,-1,true,3,4)]
    [InlineData("",4,4,true,4,4)]
    [InlineData("",-1,4,false,-1,4)]
    [InlineData("5/4",-1,-1,false,5,4)]
    [InlineData("×2",-1,-1,false,-1,-1)]
    public void UsesRealRemainingAndTotal(string raw,int number,int max,bool valid,int expected,int expectedMax)
    {
        Assert.Equal(valid,ChronicleItemPolicy.Counts(raw,number,max,out int remaining,out int total));
        Assert.Equal(expected,remaining); Assert.Equal(expectedMax,total);
    }
    [Theory]
    [InlineData("Response.data.box.list.0",true)]
    [InlineData("Response.data.box.list.19",true)]
    [InlineData("Response.data.box.list.0.item",false)]
    [InlineData("Response.data.other.list.0",false)]
    [InlineData("Response.data.box.list.-1",false)]
    [InlineData(null,false)]
    public void LimitsLabelsToVerifiedRowData(string path,bool expected)
        => Assert.Equal(expected,ChronicleItemPolicy.IsRowPath(path));
}
