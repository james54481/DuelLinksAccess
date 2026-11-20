using Xunit;
namespace DuelLinksAccess.Tests;
public class DailyRewardPolicyTests
{
    [Theory]
    [InlineData("每日獎勵", true)]
    [InlineData("Daily Reward", true)]
    [InlineData("Daily Rewards", true)]
    [InlineData("Daily reward campaign banner", false)]
    [InlineData(null, false)]
    public void ARewardPromotionIsNotAConfirmedPrompt(string text, bool expected)
        => Assert.Equal(expected, DailyRewardPolicy.IsCaption(text));
    [Theory]
    [InlineData(true, false, true, true, true)]
    [InlineData(true, false, true, false, false)]
    [InlineData(true, false, false, true, false)]
    [InlineData(true, true, true, true, false)]
    [InlineData(false, false, true, true, false)]
    public void HandlingRequiresConfirmedCaptionAndItsBlockingOverlay(bool home, bool dialog, bool caption, bool ownsHit, bool expected)
        => Assert.Equal(expected, DailyRewardPolicy.CanHandle(home, dialog, caption, ownsHit));
}
