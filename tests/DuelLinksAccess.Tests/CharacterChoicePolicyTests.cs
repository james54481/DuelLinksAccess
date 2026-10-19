using Xunit;
namespace DuelLinksAccess.Tests;

public sealed class CharacterChoicePolicyTests
{
    [Fact]
    public void PortraitIdentityUsesTheNativeRowBindingInsteadOfTheGlobalListOrdinal()
    {
        Assert.Equal(2, CharacterChoicePolicy.FindPortraitIndex(40, new[] {101,3,40,2}));
        Assert.Equal(0, CharacterChoicePolicy.FindPortraitIndex(40, new[] {40,101,3,2}));
    }

    [Fact]
    public void RecycledRowMustNotClickItsPreviousCharacter()
    {
        Assert.Equal(1, CharacterChoicePolicy.FindPortraitIndex(40, new[] {3,40}));
        Assert.Equal(-1, CharacterChoicePolicy.FindPortraitIndex(40, new[] {101,102}));
    }

    [Fact]
    public void UnknownMissingOrAmbiguousPortraitsNeverDefaultToTheFirstCell()
    {
        Assert.Equal(-1, CharacterChoicePolicy.FindPortraitIndex(40, null));
        Assert.Equal(-1, CharacterChoicePolicy.FindPortraitIndex(40, new int[0]));
        Assert.Equal(-1, CharacterChoicePolicy.FindPortraitIndex(40, new[] {40,40}));
        Assert.Equal(-1, CharacterChoicePolicy.FindPortraitIndex(0, new[] {0,40}));
    }

    [Fact]
    public void OnlyTheRequestedNativeCharacterCanBeConfirmed()
    {
        Assert.True(CharacterChoicePolicy.CanConfirm(2, new[] {2}, true, true));
        Assert.False(CharacterChoicePolicy.CanConfirm(2, new[] {1}, true, true));
        Assert.False(CharacterChoicePolicy.CanConfirm(2, new[] {1,2}, true, true));
        Assert.False(CharacterChoicePolicy.CanConfirm(2, new int[0], true, true));
    }

    [Fact]
    public void UnavailableDisabledOrUnknownCharactersAreRejected()
    {
        Assert.False(CharacterChoicePolicy.CanConfirm(2, new[] {2}, false, true));
        Assert.False(CharacterChoicePolicy.CanConfirm(2, new[] {2}, true, false));
        Assert.False(CharacterChoicePolicy.CanConfirm(-1, new[] {-1}, true, true));
        Assert.False(CharacterChoicePolicy.CanConfirm(2, null, true, true));
    }
}
