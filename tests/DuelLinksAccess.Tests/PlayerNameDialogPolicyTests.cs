using Xunit;
namespace DuelLinksAccess.Tests;
public sealed class PlayerNameDialogPolicyTests
{
    [Theory]
    [InlineData("TutorialNameDialog", "輸入你的玩家名稱。決鬥者。接受並繼續")]
    [InlineData("TutorialNameDialog", "Enter your name")]
    [InlineData("NameDialog", "玩家名稱輸入")]
    public void BothRegistrationAndExistingRenameUseAccessibleEditor(string root, string text)
        => Assert.True(PlayerNameDialogPolicy.IsMatch(root, text));
    [Theory]
    [InlineData("Friend", "輸入名稱或 ID")]
    [InlineData("DeckNameDialog", "牌組名稱")]
    public void UnrelatedInputsAreNotTreatedAsPlayerNames(string root, string text)
        => Assert.False(PlayerNameDialogPolicy.IsMatch(root, text));
    [Theory]
    [InlineData("接受並繼續", true)]
    [InlineData("確定", true)]
    [InlineData("重要事項", false)]
    public void ReturningFromEditorCanFocusTheNativeConfirmation(string label, bool expected)
        => Assert.Equal(expected, PlayerNameDialogPolicy.IsConfirmation(label));
    [Fact]
    public void InitialChoicesUseNativeClicksWithoutInventedIdsOrAutomaticSubmission()
    {
        string source = File.ReadAllText(Path.Combine(LocalizationCatalogTests.FindRepositoryRoot(), "src", "DialogHandler.cs"));
        string method = source[source.IndexOf("private void ApplyTutorialCharacterChoices(")..source.IndexOf("private void BuildCharacterChoices(")];
        Assert.Contains("button.onClick.Invoke()", method);
        Assert.Contains("ResolveCharaName(vc.cid)", method);
        Assert.DoesNotContain("OnCharaSelect(", method);
        Assert.DoesNotContain("OnCharaSubmit(", method);
    }
}
