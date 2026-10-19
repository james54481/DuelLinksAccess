using Xunit;
namespace DuelLinksAccess.Tests;

public sealed class CharacterPortraitContractTests
{
    private static string Read(string file) => File.ReadAllText(Path.Combine(
        LocalizationCatalogTests.FindRepositoryRoot(), "src", file));

    [Fact]
    public void NativeBindingsAreCapturedAfterPopulationAndReplacedWhenRowsAreRecycled()
    {
        string source = Read("CharacterPortraitBindings.cs");
        Assert.Contains("postfix: new HarmonyMethod(observer)", source);
        string capture = source[source.IndexOf("private static void Capture(")..
            source.IndexOf("private static bool TryClick(")];
        Assert.Contains("Rows.Remove(rowId)", capture);
        Assert.Contains("ids[i] = __0[i]", capture);
        Assert.Contains("DialogId = __5.GetInstanceID()", capture);
        Assert.DoesNotContain(".Invoke()", capture);
        Assert.DoesNotContain(".OnClickIcon(", capture);
        Assert.Contains("CharacterPortraitBindings.Register(harmony)", Read("HarmonyPatches.cs"));
    }

    [Fact]
    public void NativeButtonClickChecksDialogIdentityLiveBindingAndEnabledState()
    {
        string source = Read("CharacterPortraitBindings.cs");
        Assert.Contains("row.DialogId != vc.GetInstanceID()", source);
        Assert.Contains("CharacterChoicePolicy.FindPortraitIndex(cid, row.CharacterIds)", source);
        Assert.Contains("button.GetInstanceID() != row.ButtonIds[column]", source);
        Assert.Contains("!button.IsInteractable()", source);
        Assert.Contains("button.onClick.Invoke()", source);
        Assert.DoesNotContain("vc.OnClickIcon(", source);
        Assert.DoesNotContain("selectBase.position =", source);
    }

    [Fact]
    public void ScrollSearchIsBoundedAndNeverSendsAnImplicitConfirmation()
    {
        string source = Read("CharacterPortraitBindings.cs");
        Assert.Contains("step <= 7 && isCurrentDialog()", source);
        Assert.Contains("verticalNormalizedPosition = originalScroll", source);
        Assert.Contains("finished(clicked && isCurrentDialog())", source);
        Assert.DoesNotContain("vc.OnClick()", source);
        Assert.Contains("if (_characterSelectionBusy) return", Read("DialogHandler.cs"));
    }
}
