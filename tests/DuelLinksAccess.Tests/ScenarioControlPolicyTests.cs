using Xunit;
namespace DuelLinksAccess.Tests;
public class ScenarioControlPolicyTests
{
    [Theory]
    [InlineData("FastForwardButton(Clone)", "scenario_fast_forward")]
    [InlineData("SkipButton(Clone)", "scenario_skip")]
    [InlineData("AutoButton(Clone)", "scenario_auto")]
    [InlineData("LogButton", "scenario_log")]
    [InlineData("Playmaker", null)]
    public void PlaybackControlsHaveIndependentLabels(string name, string expected)
        => Assert.Equal(expected, ScenarioControlPolicy.ButtonKey("ScenarioPlayerPart", name));
    [Theory]
    [InlineData("Single")]
    [InlineData("HtjsonDialog")]
    [InlineData(null)]
    public void DoesNotRelabelOtherScreens(string screen)
        => Assert.Null(ScenarioControlPolicy.ButtonKey(screen, "AutoButton(Clone)"));
}
