using Xunit;
namespace DuelLinksAccess.Tests;
public class TraderPresentationPolicyTests
{
    [Theory]
    [InlineData("Browse",false,false,false,false,false)]
    [InlineData("Browse",false,false,true,false,false)]
    [InlineData("Browse",true,false,true,true,true)]
    [InlineData("Browse",false,true,false,true,true)]
    [InlineData("TalkThanksWait",false,false,false,true,false)]
    [InlineData("TalkThanksEnd",false,false,false,true,false)]
    [InlineData("DataWait",false,false,false,false,false)]
    public void BlocksUnderlyingInputButRequiresVisibleResultBeforeNativeBack(string mode,bool bg,bool item,bool card,bool block,bool advance)
    {
        Assert.Equal(block,TraderPresentationPolicy.IsPresentation(mode,bg,item,card));
        Assert.Equal(advance,TraderPresentationPolicy.CanAdvance(bg,item,card));
    }
}
