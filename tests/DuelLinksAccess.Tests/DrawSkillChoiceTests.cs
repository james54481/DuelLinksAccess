using Xunit;
namespace DuelLinksAccess.Tests;

public sealed class DrawSkillChoiceTests
{
    [Fact]
    public void WaitingPastFallbackDeadlineCannotDrawBeforeNormalChoice()
    {
        var choice = new DrawSkillChoice();
        var automatic = new AutomaticDrawController();
        int commands = 0, gestures = 0;
        foreach (float time in new[] { 0f, 4f, 30f })
        {
            choice.Observe(true, true);
            if (choice.BlockDraw) automatic.Reset();
            else
            {
                automatic.Update(true, true, true, time, () => commands++);
                automatic.CompleteGesture(true, time, () => gestures++);
            }
        }
        Assert.Equal(0, commands);
        Assert.Equal(0, gestures);
        choice.ChooseNormal();
        choice.Observe(true, true);
        Assert.False(choice.BlockDraw);
        automatic.Update(true, true, true, 31f, () => commands++);
        automatic.CompleteGesture(true, 31f, () => gestures++);
        Assert.Equal(1, gestures);
        Assert.Equal(0, commands);
    }

    [Fact]
    public void AvailableSkillBlocksGestureFallbackAndRetryUntilPlayerChooses()
    {
        var choice = new DrawSkillChoice();
        choice.Observe(true, true);
        Assert.True(choice.Offered);
        Assert.True(choice.BlockDraw);
        choice.Observe(true, false);
        Assert.True(choice.BlockDraw); // A transient native flag must not discard the offer.
        choice.ChooseNormal();
        choice.Observe(true, true);
        Assert.False(choice.BlockDraw);
    }

    [Fact]
    public void SkillSubmissionAndCancellationNeverSilentlyDraw()
    {
        var choice = new DrawSkillChoice();
        choice.Observe(true, true);
        choice.SkillSubmitted();
        choice.Observe(true, false);
        Assert.True(choice.BlockDraw);
        choice.Reopen();
        choice.Observe(true, false);
        Assert.True(choice.BlockDraw);
        Assert.False(choice.Submitted);
    }

    [Fact]
    public void UnknownAvailabilityDoesNotSkipSkill()
    {
        var choice = new DrawSkillChoice();
        choice.Observe(true, null);
        Assert.True(choice.BlockDraw);
        choice.ChooseNormal();
        choice.Observe(true, null);
        Assert.False(choice.BlockDraw);
    }

    [Fact]
    public void NoSkillRetainsAutomaticDrawAndNewTurnCanOfferAgain()
    {
        var choice = new DrawSkillChoice();
        choice.Observe(true, false);
        Assert.False(choice.BlockDraw);
        choice.ChooseNormal();
        choice.Observe(false, false);
        choice.Observe(true, true);
        Assert.True(choice.BlockDraw);
        Assert.False(choice.NormalChosen);
    }
}
