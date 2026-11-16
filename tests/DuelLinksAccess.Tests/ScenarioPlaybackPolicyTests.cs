using Xunit;
namespace DuelLinksAccess.Tests;
public class ScenarioPlaybackPolicyTests
{
    [Theory]
    [InlineData(1, 0, true, true)]
    [InlineData(2, 1, true, true)]
    [InlineData(1, 1, true, false)]
    [InlineData(1, 0, false, false)]
    [InlineData(0, 0, true, false)]
    public void OnlyNewReadyControllersAreInitialized(long current, long previous, bool ready, bool expected)
        => Assert.Equal(expected, ScenarioPlaybackPolicy.Initialize(current, previous, ready));
}
