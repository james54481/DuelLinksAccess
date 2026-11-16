using Xunit;
namespace DuelLinksAccess.Tests;
public sealed class DuelFlipPolicyTests
{
    [Theory]
    [InlineData(16403)]
    [InlineData(19)]
    [InlineData(49156)]
    public void RecordedAnimationEventsNeverMeanConfirmedReveal(int packed)
    {
        int locate = (packed >> 1) & 31;
        Assert.False(DuelFlipPolicy.CanAnnounce("CardFlipTurn", true, locate, 17, 17, true));
    }
    [Fact]
    public void MatchingLocalMonsterAfterExplicitFlipCanBeAnnounced()
        => Assert.True(DuelFlipPolicy.CanAnnounce("CutinFlip", true, 2, 17, 17, true));
    [Theory]
    [InlineData(false, 2, 17, 17, true)]
    [InlineData(true, 9, 17, 17, true)]
    [InlineData(true, 2, 17, 29, true)]
    [InlineData(true, 2, 0, 0, true)]
    [InlineData(true, 2, 17, 17, false)]
    public void OpponentSpellsChangedTargetsUnknownAndFaceDownAreNotReveals(
        bool local, int locate, int expected, int actual, bool faceUp)
        => Assert.False(DuelFlipPolicy.CanAnnounce("CutinFlip", local, locate, expected, actual, faceUp));
}
