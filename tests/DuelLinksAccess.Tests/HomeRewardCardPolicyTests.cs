using Xunit;
namespace DuelLinksAccess.Tests;
public class HomeRewardCardPolicyTests
{
    [Theory]
    [InlineData("6086",6086)]
    [InlineData("12579",12579)]
    [InlineData("ItemA0001",0)]
    [InlineData("Font Texture",0)]
    [InlineData("sactx-0-2048x2048-Uncompressed-UIMenu-a5210ef5",0)]
    [InlineData("6086.png",0)]
    [InlineData("+6086",0)]
    [InlineData(" 6086",0)]
    [InlineData("6086 ",0)]
    [InlineData("06086",0)]
    [InlineData("0",0)]
    [InlineData("2147483648",0)]
    [InlineData("６０８６",0)]
    [InlineData("",0)]
    [InlineData(null,0)]
    public void AcceptsOnlyCanonicalCardTextureNames(string name,int expected)
        => Assert.Equal(expected,HomeRewardCardPolicy.Resolve(new[]{name}));
    [Fact] public void RejectsAmbiguousImages()
        => Assert.Equal(0,HomeRewardCardPolicy.Resolve(new[]{"6086","12579"}));
    [Fact] public void RepeatedSameImageIsNotAmbiguous()
        => Assert.Equal(6086,HomeRewardCardPolicy.Resolve(new[]{"6086","6086","Font Texture"}));
    [Fact] public void RejectsMissingSource()
        => Assert.Equal(0,HomeRewardCardPolicy.Resolve(null));
    [Fact] public void FreshTextureIdentityAllowsSameAlreadyOpenCard()
    {
        int expected=HomeRewardCardPolicy.Resolve(new[]{"6086"});
        Assert.True(HomeRewardDetailPolicy.CanRead(true,6086,6086,expected));
        Assert.False(HomeRewardDetailPolicy.CanRead(true,6086,12579,expected));
    }
}
