using Xunit;
namespace DuelLinksAccess.Tests;
public class GateRewardPolicyTests
{
    [Fact]
    public void RejectsMissingOrInvalidIdentity()
    {
        Assert.False(GateRewardPolicy.Valid(null));
        Assert.False(GateRewardPolicy.Valid(new int[0]));
        Assert.False(GateRewardPolicy.Valid(new[] { 1, 0 }));
        Assert.False(GateRewardPolicy.Valid(new[] { -1 }));
        Assert.True(GateRewardPolicy.Valid(new[] { 123, 456 }));
    }
    [Fact]
    public void ChangedOrderOrIdentityInvalidatesSnapshot()
    {
        Assert.True(GateRewardPolicy.Same(new[] { 1, 2 }, new[] { 1, 2 }));
        Assert.False(GateRewardPolicy.Same(new[] { 1, 2 }, new[] { 2, 1 }));
        Assert.False(GateRewardPolicy.Same(new[] { 1, 2 }, new[] { 1, 3 }));
        Assert.False(GateRewardPolicy.Same(new[] { 1, 2 }, new[] { 1 }));
    }
    [Theory]
    [InlineData(0, -1, 3, 2)]
    [InlineData(2, 1, 3, 0)]
    [InlineData(0, 1, 1, 0)]
    [InlineData(0, 1, 0, 0)]
    public void WrapsWithinNativeList(int index, int delta, int count, int expected)
        => Assert.Equal(expected, GateRewardPolicy.Move(index, delta, count));
}
