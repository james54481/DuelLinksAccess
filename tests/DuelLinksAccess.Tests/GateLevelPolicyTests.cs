using Xunit;
namespace DuelLinksAccess.Tests;
public sealed class GateLevelPolicyTests
{
    [Theory]
    [InlineData("等級:20", 20)]
    [InlineData("Lv. 10", 10)]
    [InlineData("等級20", 20)]
    [InlineData("Lv20", 20)]
    [InlineData("レベル：40", 40)]
    [InlineData("40", 40)]
    public void ReadsNativeTabLevel(string label, int level) => Assert.Equal(level, GateLevelPolicy.ReadLevel(label));
    [Theory]
    [InlineData("TabButton20")]
    [InlineData("Tab(Clone)")]
    [InlineData("")]
    [InlineData(null)]
    public void DoesNotInventLevelsFromNames(string text) => Assert.Null(GateLevelPolicy.ReadLevel(text));
    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    public void HiddenOrDisabledLevelsAreNotSelectable(bool active, bool disabled, bool expected)
        => Assert.Equal(expected, GateLevelPolicy.CanSelect(active, disabled));
}
