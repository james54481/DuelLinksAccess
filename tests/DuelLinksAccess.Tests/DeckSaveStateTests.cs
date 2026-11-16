using Xunit;
namespace DuelLinksAccess.Tests;
public class DeckSaveStateTests
{
    [Fact]
    public void RequestAloneIsNotSuccessAndCompletionMustMatchDeck()
    {
        var state = new DeckSaveState();
        state.Begin(1, 0, true);
        Assert.False(state.Complete(2, false));
        Assert.False(state.Complete(1, true));
        Assert.True(state.Complete(1, false));
        Assert.False(state.Complete(1, false));
        Assert.False(state.Expired(100));
    }
    [Fact]
    public void TimeoutIsReportedOnceAndDoesNotBecomeLateSuccess()
    {
        var state = new DeckSaveState(); state.Begin(1, 10, true);
        Assert.False(state.Expired(39)); Assert.True(state.Expired(40));
        Assert.False(state.Expired(41)); Assert.False(state.Complete(1, false));
    }
    [Fact]
    public void UnchangedDeckAndUnsolicitedCallbackDoNotClaimSave()
    {
        var state = new DeckSaveState();
        Assert.False(state.Complete(1, false));
        state.Begin(1, 0, false);
        Assert.False(state.Complete(1, false)); Assert.False(state.Expired(100));
    }
    [Fact]
    public void NewDeckRequestDoesNotUsePreviousDeckCompletion()
    {
        var state = new DeckSaveState(); state.Begin(1, 0, true); state.Begin(2, 5, true);
        Assert.False(state.Complete(1, false)); Assert.True(state.Complete(2, false));
    }
}
