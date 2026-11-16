using Xunit;
namespace DuelLinksAccess.Tests;
public class DeckCopyPolicyTests
{
    [Theory]
    [InlineData(true, "CopySrc", DeckCopyStage.SourceSelection)]
    [InlineData(true, "CopyDest", DeckCopyStage.DestinationSelection)]
    [InlineData(false, "DeckCopySrc", DeckCopyStage.SourcePreview)]
    [InlineData(false, "DeckCopyDest", DeckCopyStage.DestinationPreview)]
    [InlineData(true, "DeckEdit", DeckCopyStage.None)]
    [InlineData(false, "Replay", DeckCopyStage.None)]
    [InlineData(true, "UNKNOWN", DeckCopyStage.None)]
    public void OnlyVerifiedNativeCopyModesHaveStages(bool select, string mode, DeckCopyStage expected)
        => Assert.Equal(expected, DeckCopyPolicy.Stage(select, mode));
    [Fact]
    public void LeavingCopyClearsTargetsEvenWhenTheSameControllerIsReused()
    {
        var s = new DeckCopyState(); s.BeginSource(1); s.Source("a", "甲"); s.Destination("b", "乙");
        s.End(); s.BeginSource(1); Assert.False(s.Matches("a", "b"));
    }
    [Fact]
    public void UnknownDestinationSlotCannotAuthorizeCopy()
    {
        var s = new DeckCopyState(); s.BeginSource(1); s.Source("a", "來源");
        s.Destination("b", "目的欄位 UNKNOWN", slotKnown: false);
        Assert.False(s.Matches("a", "b"));
    }
    [Fact]
    public void ConfirmationRequiresMatchingSourceAndDestination()
    {
        var s = new DeckCopyState(); s.BeginSource(1); s.Source("a", "甲角色第1欄");
        Assert.False(s.Matches("a", "b"));
        s.Destination("b", "乙角色第2欄"); Assert.True(s.Matches("a", "b"));
        Assert.False(s.Matches("a", "c"));
        s.Source("new", "新來源"); Assert.False(s.Matches("new", "b"));
    }
    [Fact]
    public void RescanKeepsCurrentSessionButNewCopyClearsOldTargets()
    {
        var s = new DeckCopyState(); s.BeginSource(1); s.Source("a", "甲"); s.Destination("b", "乙");
        s.BeginSource(1); Assert.True(s.Matches("a", "b"));
        s.BeginSource(2); Assert.False(s.Matches("a", "b"));
    }
}
