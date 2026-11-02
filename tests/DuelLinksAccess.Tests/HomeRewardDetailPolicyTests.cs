using Xunit;
namespace DuelLinksAccess.Tests;
public class HomeRewardDetailPolicyTests
{
    [Theory]
    [InlineData(false,0,100,0,true)]
    [InlineData(true,100,200,0,true)]
    [InlineData(true,100,100,0,false)]
    [InlineData(true,100,100,100,true)]
    [InlineData(true,100,200,100,false)]
    [InlineData(false,0,0,0,false)]
    [InlineData(false,0,-1,0,false)]
    public void RejectsStaleOrWrongCard(bool visible,int before,int after,int expected,bool accepted)
        => Assert.Equal(accepted,HomeRewardDetailPolicy.CanRead(visible,before,after,expected));
}
