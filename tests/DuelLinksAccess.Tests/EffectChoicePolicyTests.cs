using Xunit;
namespace DuelLinksAccess.Tests;

public sealed class EffectChoicePolicyTests
{
    [Theory]
    [InlineData("光：送入墓地", "光：送入墓地", true)]
    [InlineData("暗：加入手牌", "暗：加入手牌", true)]
    [InlineData("光：送入墓地", "暗：加入手牌", false)]
    [InlineData("", "", false)]
    [InlineData(null, "光：送入墓地", false)]
    public void ConfirmationRequiresTheSameGameDescriptionThatWasAnnounced(string spoken, string live, bool expected)
        => Assert.Equal(expected, EffectChoicePolicy.DescriptionMatches(spoken, live));

    [Theory]
    [InlineData(2, 0, 1, 1)]
    [InlineData(2, 1, 1, 0)]
    [InlineData(2, 0, -1, 1)]
    [InlineData(0, 0, 1, -1)]
    [InlineData(2, -1, 1, -1)]
    public void NavigationWrapsBetweenBothEffectsAndRejectsUnknownSelection(int count, int current, int direction, int expected)
        => Assert.Equal(expected, EffectChoicePolicy.Move(count, current, direction));

    [Fact]
    public void SelectingDarkCannotConfirmNativeDefaultLight()
        => Assert.False(EffectChoicePolicy.CanConfirm(2, 1, 0, true, true, false, false, true));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void VerifiedLightAndDarkCanBothBeConfirmed(int index)
        => Assert.True(EffectChoicePolicy.CanConfirm(2, index, index, true, true, false, false, true));

    [Theory]
    [InlineData(0, 0, 0, true, true, false, false, true)]
    [InlineData(2, -1, -1, true, true, false, false, true)]
    [InlineData(2, 2, 2, true, true, false, false, true)]
    [InlineData(2, 1, 1, false, true, false, false, true)]
    [InlineData(2, 1, 1, true, false, false, false, true)]
    [InlineData(2, 1, 1, true, true, true, false, true)]
    [InlineData(2, 1, 1, true, true, false, true, true)]
    [InlineData(2, 1, 1, true, true, false, false, false)]
    public void MissingDisabledPendingViewingOrUnverifiedChoicesNeverConfirm(
        int count, int requested, int actual, bool text, bool enabled, bool pending, bool viewing, bool verified)
        => Assert.False(EffectChoicePolicy.CanConfirm(count, requested, actual, text, enabled, pending, viewing, verified));

    [Fact]
    public void NativeTabsAreDetectedAndActivatedWithoutWritingNativeSelectionFields()
    {
        string source = File.ReadAllText(Path.Combine(LocalizationCatalogTests.FindRepositoryRoot(), "src", "DuelHandler.cs"));
        string path = source[source.IndexOf("private bool HandleSelectEffectDialog()")..
            source.IndexOf("private static int SafeSelectedIdx(")];
        Assert.Contains("GetComponentsInChildren<Il2CppYgomSystem.UI.YgomTabButton>(false)", path);
        Assert.Contains("nativeTab.OnPointerClick(eventData)", path);
        Assert.Contains("EffectChoicePolicy.CanConfirm(", path);
        Assert.Contains("dlg.OnBack()", path);
        Assert.DoesNotContain("dlg.selectedIdx =", path);
        Assert.DoesNotContain("toggle.isOn =", path);
        Assert.True(source.IndexOf("if (HandleSelectEffectDialog()) return;")
            < source.IndexOf("if (_eventLog.IsBrowsing)", source.IndexOf("HandleDuelYesNoDialog()) return;")));
    }
}
