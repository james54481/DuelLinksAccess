using Xunit;
namespace DuelLinksAccess.Tests;
public sealed class CardSelectionPolicyTests
{
    [Theory]
    [InlineData(0, 2, 3, 2)]
    [InlineData(2, 0, 3, 0)]
    [InlineData(0, -1, 3, -1)]
    [InlineData(0, 3, 3, -1)]
    [InlineData(-1, 0, 3, -1)]
    public void SortedVisualsUseGameDataIndex(int visual, int mapped, int count, int expected)
        => Assert.Equal(expected, CardSelectionPolicy.ResolveDataIndex(visual, mapped, count));

    [Theory]
    [InlineData(11125, 11125, 70, 70, 3, true)]
    [InlineData(11125, 12451, 70, 70, 3, false)]
    [InlineData(11125, 11125, 70, 72, 3, false)]
    [InlineData(11125, 11125, 0, 0, 3, false)]
    [InlineData(11125, 11125, 70, 70, -1, false)]
    public void ExtraSummonRequiresSameLiveCard(int expected, int actual, int uid, int actualUid, int index, bool allowed)
        => Assert.Equal(allowed, CardSelectionPolicy.CanDispatchExtra(expected, actual, uid, actualUid, index));
}
