using Xunit;
namespace DuelLinksAccess.Tests;
public sealed class ResultScanStateTests
{
    [Fact]
    public void MissingUiDoesNotScanEveryFrameButCanAppearLater()
    {
        var state = new ResultScanState();
        Assert.True(state.TryBegin(10));
        for (int i = 1; i < 30; i++) Assert.False(state.TryBegin(10 + i / 60f));
        Assert.True(state.TryBegin(10.5f));
    }
    [Fact]
    public void RepeatedFailureDelaysRetriesAndLogsOnlyChangedError()
    {
        var state = new ResultScanState();
        Assert.True(state.Failed(10, "unstripping"));
        Assert.False(state.TryBegin(10.5f));
        Assert.True(state.TryBegin(11));
        Assert.False(state.Failed(11, "unstripping"));
        Assert.False(state.TryBegin(12));
        Assert.True(state.TryBegin(13));
        Assert.True(state.Failed(13, "different failure"));
    }
    [Fact]
    public void NewDuelResetsDelayAndErrorSuppression()
    {
        var state = new ResultScanState();
        state.Failed(100, "failure");
        state.Reset();
        Assert.True(state.TryBegin(0));
        Assert.True(state.Failed(0, "failure"));
    }
    [Fact]
    public void PersistentFailureRemainsRetryableWithBoundedDelay()
    {
        var state = new ResultScanState();
        for (int i = 0; i < 20; i++) state.Failed(10, "failure");
        Assert.False(state.TryBegin(14.9f));
        Assert.True(state.TryBegin(15));
    }
}
