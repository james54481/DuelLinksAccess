using Xunit;

namespace DuelLinksAccess.Tests;

public sealed class DuelConfirmationStateTests
{
    [Fact]
    public void IdenticalTributePromptCanBeAnsweredAgainAfterCooldown()
    {
        var state = new DuelConfirmationState();
        Assert.True(state.Observe(true, "釋放2隻怪獸？", 1f, out var announce));
        Assert.True(announce);
        state.Responded(1f);
        Assert.True(state.Observe(true, "釋放2隻怪獸？", 1.2f, out announce));
        Assert.False(announce);
        Assert.False(state.CanRespond(1.2f));
        Assert.True(state.Observe(true, "釋放2隻怪獸？", 1.6f, out announce));
        Assert.True(announce);
        Assert.True(state.CanRespond(1.6f));
    }

    [Fact]
    public void HiddenContentDoesNotBlockMaterialSelectionDespiteStaleText()
    {
        var state = new DuelConfirmationState();
        state.Observe(true, "是否召喚？", 1f, out _);
        state.Responded(1f);
        Assert.False(state.Observe(false, "是否召喚？", 1.1f, out _));
        Assert.True(state.Observe(true, "是否召喚？", 1.2f, out var announce));
        Assert.True(announce);
        Assert.True(state.CanRespond(1.2f));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void EmptyDormantPromptDoesNotCaptureKeys(string text)
    {
        Assert.False(new DuelConfirmationState().Observe(true, text, 0f, out _));
    }

    [Fact]
    public void ChangedPromptIsAnnouncedAndUnchangedPromptIsNotRepeatedEveryFrame()
    {
        var state = new DuelConfirmationState();
        state.Observe(true, "是否召喚？", 1f, out _);
        state.Observe(true, "是否召喚？", 1.1f, out var announce);
        Assert.False(announce);
        state.Observe(true, "是否發動？", 1.2f, out announce);
        Assert.True(announce);
    }
}
