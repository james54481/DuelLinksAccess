using Xunit;
namespace DuelLinksAccess.Tests;

public sealed class CharacterSelectionContractTests
{
    private static string Source => File.ReadAllText(Path.Combine(
        LocalizationCatalogTests.FindRepositoryRoot(), "src", "DialogHandler.cs"));

    [Fact]
    public void SingleCharacterSelectorsUseNativeIdentitiesInsteadOfRecycledRowOrdinals()
    {
        string code = Source;
        Assert.Contains("if (vc.isCharaSelect || vc.isGate)", code);
        string path = code[code.IndexOf("private void BuildCharacterChoices(")..
            code.IndexOf("/// <summary>Resolves a character id")];
        Assert.Contains("vc.GetCurrentCharaList()", path);
        Assert.Contains("CharacterPortraitBindings.Select(vc, cid", path);
        Assert.DoesNotContain("vc.OnClickIcon(cid)", path);
        Assert.DoesNotContain("row.buttons", path);
        Assert.Contains("CharaFilterDialogViewController.grayedList.Contains(cid)", path);
    }

    [Fact]
    public void ConfirmationRequiresSelectionAndRemainsASeparateUserAction()
    {
        string path = Source[Source.IndexOf("private void BuildCharacterChoices(")..
            Source.IndexOf("/// <summary>Resolves a character id")];
        Assert.Contains("CharacterChoicePolicy.ReadSingle(ReadCharacterSelection(vc))", path);
        Assert.Contains("CharacterChoicePolicy.CanConfirm(selected, actual", path);
        Assert.Contains("LiveLabel = () => ReadCharacterActionLabel(vc)", path);
        Assert.Contains("vc.okButtonLabel?.text", Source);
        Assert.Contains("_items.Insert(_focusIndex + 1, confirm)", path);
        Assert.Contains("vc.OnClick();", path);
        Assert.Contains("GetTopVcInstanceId(_activeManager) != item.Go.GetInstanceID()", Source);
    }
}
