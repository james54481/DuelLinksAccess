using Xunit;
namespace DuelLinksAccess.Tests;
public class TextEditorPolicyTests
{
    [Theory]
    [InlineData(null, true, true, false)]
    [InlineData("", true, true, true)]
    [InlineData("青眼", true, true, true)]
    [InlineData("青眼", false, true, false)]
    [InlineData("青眼", true, false, false)]
    public void CancelAndStaleDialogsCannotOverwriteText(string result, bool same, bool active, bool expected)
        => Assert.Equal(expected, TextEditorPolicy.CanApply(result, same, active));
    [Theory]
    [InlineData(TextEditPurpose.CardSearch, 0, 32767, 0)]
    [InlineData(TextEditPurpose.DeckSearch, 0, 32767, 0)]
    [InlineData(TextEditPurpose.DeckSearch, 40, 40, 0)]
    [InlineData(TextEditPurpose.CardSearch, 50, 50, 0)]
    [InlineData(TextEditPurpose.PlayerName, 0, 12, 3)]
    [InlineData(TextEditPurpose.FriendSearch, 0, 32767, 0)]
    public void CardSearchDoesNotInheritPlayerNameLimits(TextEditPurpose purpose, int native, int max, int min)
    {
        Assert.Equal(max, TextEditorPolicy.MaximumLength(purpose, native));
        Assert.Equal(min, TextEditorPolicy.MinimumLength(purpose));
    }
}
