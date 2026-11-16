using Xunit;
namespace DuelLinksAccess.Tests;
public sealed class DuelPromptSpeechPolicyTests
{
    [Theory]
    [InlineData("已選擇「」。")]
    [InlineData("已選擇「  」。")]
    [InlineData("這是準備階段。 是否確認場地？")]
    [InlineData("目前並無可由連鎖發動的卡。 是否確認場地？")]
    [InlineData("怪獸被戰鬥破壞。 是否確認場地？")]
    [InlineData("There is no card that can be activated by a chain. Check the field?")]
    public void RecordedEmptyAndFieldInspectionInformationIsNotSpoken(string text)
        => Assert.True(DuelPromptSpeechPolicy.IsInformationalNoise(text));
    [Theory]
    [InlineData("是否發動卡的效果？")]
    [InlineData("這是準備階段。 是否發動卡的效果？")]
    [InlineData("請選擇攻擊目標")]
    [InlineData("請選擇表示形式")]
    [InlineData("選擇「黑魔導」作為對象。")]
    [InlineData("選擇「王之棺」的位置。 是否發動卡的效果？")]
    [InlineData("已選擇「黑魔導」。")]
    [InlineData("黑色魔術師－黑魔導已加至手牌。")]
    [InlineData("是否發動效果？ 是否確認場地？")]
    public void ConcreteInstructionsAndNamedResultsRemainAvailable(string text)
        => Assert.False(DuelPromptSpeechPolicy.IsInformationalNoise(text));
    [Theory]
    [InlineData("選擇「王之棺」的位置。")]
    [InlineData("選擇「卡諾匹斯的保衛者」的位置。")]
    [InlineData("選擇「荷魯斯的榮光－伊姆賽特」的位置。")]
    [InlineData("選擇「咒魂的假面」的位置。")]
    [InlineData("選擇「黑魔導」的位置。")]
    public void AutomaticPlacementInformationIsOmitted(string text)
        => Assert.True(DuelPromptSpeechPolicy.IsInformationalNoise(text));
    [Fact]
    public void HalfSecondDuplicateIsSuppressedEvenWithInterveningText()
    {
        var p = new DuelPromptSpeechPolicy();
        Assert.True(p.TryAccept("這是準備階段。 是否發動卡的效果？", 0));
        Assert.True(p.TryAccept("另一段提示", 0.2f));
        Assert.False(p.TryAccept("這是準備階段。  是否發動卡的效果？", 0.43f));
        Assert.True(p.TryAccept("這是準備階段。 是否發動卡的效果？", 2.1f));
    }
    [Fact]
    public void SessionResetAllowsTheSamePromptAgain()
    {
        var p = new DuelPromptSpeechPolicy();
        Assert.True(p.TryAccept("是否發動效果？", 0));
        p.Reset(); Assert.True(p.TryAccept("是否發動效果？", 0.1f));
    }
}
