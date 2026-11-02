using Xunit;
namespace DuelLinksAccess.Tests;
public class GateNavigationLabelPolicyTests
{
    [Theory]
    [InlineData("Gate", "ArrowL", "gate_previous_duelist")]
    [InlineData("Gate", "Back", "gate_previous_duelist")]
    [InlineData("Gate", "Next", "gate_next_duelist")]
    [InlineData("Gate", "CurrentSeriesLogo", null)]
    [InlineData("Other", "CurrentSeriesLogo", null)]
    [InlineData("Gate", "CharaList(Clone)", "gate_change_style")]
    [InlineData("Gate", "BackButton", "screen_back")]
    [InlineData("Gate", "switch", null)]
    [InlineData("Gate", "DuelButton", null)]
    [InlineData("Other", "Next", null)]
    [InlineData("GateOver", "Back", null)]
    public void LabelsOnlyGateNavigation(string view, string name, string expected)
        => Assert.Equal(expected, GateNavigationLabelPolicy.ButtonKey(view, name));

    [Theory]
    [InlineData("Gate", "CurrentSeriesLogo", true, true)]
    [InlineData("Gate", "CurrentSeriesLogo", false, false)]
    [InlineData("Other", "CurrentSeriesLogo", true, false)]
    [InlineData("Gate", "ChangeButton", true, false)]
    public void DuplicateIconRequiresAnAlternativeEntry(string view, string name, bool alternative, bool hidden)
        => Assert.Equal(hidden, GateNavigationLabelPolicy.HideDuplicate(view, name, alternative));

    [Theory]
    [InlineData("Gate", "switch", true, 0, true)]
    [InlineData("Gate", "switch", true, 1, false)]
    [InlineData("Gate", "switch", true, null, false)]
    [InlineData("Gate", "switch", false, 0, false)]
    [InlineData("Other", "switch", true, 0, false)]
    [InlineData("Gate", "mission", true, 0, false)]
    public void HideOnlyConfirmedEmptyLiveMissionSwitch(string view, string name, bool live, int? count, bool hidden)
        => Assert.Equal(hidden, GateNavigationLabelPolicy.HideEmptyMissionSwitch(view, name, live, count));
}
