using Xunit;
namespace DuelLinksAccess.Tests;

public sealed class DuelPullDownPolicyTests
{
    [Theory]
    [InlineData(0, true, false)]
    [InlineData(1, true, true)]
    [InlineData(1, false, false)]
    [InlineData(-1, true, false)]
    public void SingleChoiceRequiresActualNativeIndex(int current, bool enabled, bool expected)
        => Assert.Equal(expected, DuelPullDownPolicy.CanConfirm(true, 1,
            new[] { false, true }, current, enabled, false, true));

    [Fact]
    public void UnknownPendingAndUnlabelledOptionsCannotConfirm()
    {
        Assert.False(DuelPullDownPolicy.CanConfirm(true, 1, null, 0, true, false, true));
        Assert.False(DuelPullDownPolicy.CanConfirm(true, 1, new[] { true }, 0, true, true, true));
        Assert.False(DuelPullDownPolicy.CanConfirm(true, 1, new[] { true }, 0, true, false, false));
        Assert.False(DuelPullDownPolicy.CanConfirm(true, 1, new[] { true, true }, 0, true, false, true));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void MultiChoiceRequiresNativeRequiredCount(int required, bool expected)
        => Assert.Equal(expected, DuelPullDownPolicy.CanConfirm(false, required,
            new[] { true, false, true }, -1, true, false, true));
}
