using Xunit;
namespace DuelLinksAccess.Tests;
public sealed class ResultPagePolicyTests
{
    [Theory]
    [InlineData("下一個")]
    [InlineData("確定")]
    [InlineData("Next")]
    [InlineData("OK")]
    public void LocalizedAdvanceLabelsAreRecognized(string label)
        => Assert.True(ResultPagePolicy.IsAdvance(label));
    [Fact]
    public void ExperienceAndOneDigitQuantitiesArePreserved()
        => Assert.Equal("角色等級. 39. 11925. (+750). 4. 4",
            ResultPagePolicy.Compose(new[] {"角色等級","39","11925","(+750)","4","4","下一個"}));
    [Theory]
    [InlineData("角色升級獎勵")]
    [InlineData("Character Level-Up Rewards")]
    public void FutureMilestonesAreNotAnnouncedAsThisDuelsLoot(string heading)
        => Assert.Equal("39. 11925. (+750)", ResultPagePolicy.Compose(
            new[] {"39","11925","(+750)",heading,"250","等級:39","100000","等級:30","下一個"}));
    [Fact]
    public void AssessmentKeepsEachScoreEvenWhenValuesRepeat()
        => Assert.Equal("決鬥勝利. 2000. 發動魔法卡. 100. 發動陷阱卡. 100. 分數. 2600",
            ResultPagePolicy.Compose(new[] {"決鬥勝利","2000","發動魔法卡","100","發動陷阱卡","100","分數","2600"}));
    [Fact]
    public void AnimationMustSettleAndUnchangedTextIsNotRepeated()
    {
        var state = new ResultPageAnnouncement();
        Assert.False(state.Observe("分數. 1632", 0));
        Assert.False(state.Observe("分數. 2600", 0.3f));
        Assert.False(state.Observe("分數. 2600", 0.8f));
        Assert.True(state.Observe("分數. 2600", 1.2f));
        Assert.False(state.Observe("分數. 2600", 3));
        state.Reset();
        Assert.False(state.Observe("分數. 2600", 4));
        Assert.True(state.Observe("分數. 2600", 5));
    }
}
