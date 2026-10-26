using Xunit;
namespace DuelLinksAccess.Tests;

public sealed class ExtraBrowsePolicyTests
{
    [Theory]
    [InlineData(false, true, false, true, true)]
    [InlineData(true, true, false, true, false)]
    [InlineData(false, false, false, true, false)]
    [InlineData(false, true, true, true, false)]
    [InlineData(false, true, false, false, false)]
    public void SeparateNativeViewIsUsedOnlyWhenActiveWithoutAnEffectSelection(
        bool selection, bool active, bool closing, bool own, bool expected)
        => Assert.Equal(expected, ExtraBrowsePolicy.UseViewList(selection, active, closing, own));

    [Theory]
    [InlineData(true, 0, 10534, 10534)]
    [InlineData(true, 11320, 10534, 11320)]
    [InlineData(true, 0, 42, 0)]
    [InlineData(true, 0, -1, 0)]
    [InlineData(true, 0, 100000, 0)]
    [InlineData(false, 10534, 10534, 0)]
    public void NativeDatabaseIdsAreAcceptedOnlyForOwnExtraView(bool own, int cid, int mixed, int expected)
        => Assert.Equal(expected, ExtraBrowsePolicy.CardId(own, cid, mixed));

    [Theory]
    [InlineData(true, false, true, 42, 10534, true, true)]
    [InlineData(false, false, true, 42, 10534, true, false)]
    [InlineData(true, true, true, 42, 10534, true, false)]
    [InlineData(true, false, false, 42, 10534, true, false)]
    [InlineData(true, false, true, -1, 10534, true, false)]
    [InlineData(true, false, true, 42, 0, true, false)]
    [InlineData(true, false, true, 42, 10534, false, false)]
    public void SummonRequestRequiresLiveOwnIdentityAndMainPhase(bool active, bool closing,
        bool own, int uid, int cid, bool main, bool expected)
        => Assert.Equal(expected, ExtraBrowsePolicy.CanRequestSummon(active, closing, own, uid, cid, main));

    [Fact]
    public void DuplicateNamesStillRequireExactUidInReorderedLegalList()
    {
        // Native view contains two copies of 10534, UIDs 42 and 44.
        // Only UID 44 is legal now. Never substitute it for UID 42.
        Assert.Equal(-1, ExtraSummonPolicy.FindVisual(42, new[] {44}, new[] {0}, new[] {44}));
        Assert.Equal(1, ExtraSummonPolicy.FindVisual(42, new[] {44,42}, new[] {1,0}, new[] {42,44}));
    }
}
