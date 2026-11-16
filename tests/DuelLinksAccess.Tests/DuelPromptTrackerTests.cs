using Xunit;
namespace DuelLinksAccess.Tests;
public sealed class DuelPromptTrackerTests
{
    [Fact]
    public void EmptyThenLateStableTextIsReadOnce()
    {
        var p = new DuelPromptTracker(); p.Request(0);
        Assert.Null(p.Observe(0.2f, null, false));
        Assert.Null(p.Observe(0.4f, "是否發動效果？", false));
        Assert.Equal("是否發動效果？", p.Observe(0.6f, "是否發動效果？", false));
        Assert.Null(p.Observe(0.8f, "是否發動效果？", false));
    }
    [Fact]
    public void RepeatedEventsCannotKeepAnEmptyPromptAlive()
    {
        var p = new DuelPromptTracker(); p.Request(0); p.Request(0.9f);
        Assert.Null(p.Observe(1.1f, null, false)); Assert.False(p.Pending);
    }
    [Fact]
    public void DedicatedPickerOrYesNoOwnsItsPrompt()
    {
        var p = new DuelPromptTracker(); p.Request(0);
        Assert.Null(p.Observe(0.2f, "舊提示", true)); Assert.False(p.Pending);
    }
    [Fact]
    public void ChangingTextMustSettleBeforeSpeech()
    {
        var p = new DuelPromptTracker(); p.Request(0);
        Assert.Null(p.Observe(0.2f, "舊提示", false));
        Assert.Null(p.Observe(0.3f, "新提示", false));
        Assert.Equal("新提示", p.Observe(0.5f, "新提示", false));
    }
    [Theory]
    [InlineData(true, true, true, false)]
    [InlineData(true, false, true, true)]
    [InlineData(true, false, false, false)]
    [InlineData(false, false, true, false)]
    public void OwnInputModalsAndUnknownWaitsAreSilent(bool active, bool mine, bool opp, bool modal)
    {
        var p = new OpponentWaitTracker();
        Assert.False(p.Observe(0, active, mine, opp, modal));
        Assert.False(p.Observe(2, active, mine, opp, modal));
    }
    [Fact]
    public void ConfirmedOpponentWaitIsDebouncedAndAnnouncedOncePerEpisode()
    {
        var p = new OpponentWaitTracker();
        Assert.False(p.Observe(0, true, false, true, false));
        Assert.True(p.Observe(1, true, false, true, false));
        Assert.False(p.Observe(2, true, false, true, false));
        Assert.False(p.Observe(3, true, true, false, false));
        Assert.False(p.Observe(4, true, false, true, false));
        Assert.True(p.Observe(5, true, false, true, false));
    }
}
