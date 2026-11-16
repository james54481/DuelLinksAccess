using Xunit;
namespace DuelLinksAccess.Tests;

public sealed class DuelDestructionPolicyTests
{
    [Theory]
    [InlineData(2, 17, "沉默魔術師・零")]
    [InlineData(3, 29, "黑魔導")]
    public void RecordedPvpDestructionNeverLooksUpThePositionAsUid(int locate, int uid, string expected)
    {
        var requested = new List<int>();
        string result = DuelDestructionPolicy.ResolveName(locate, uid, 0, null, candidate =>
        {
            requested.Add(candidate);
            return candidate == uid ? expected : "錯誤的牌組卡片";
        });
        Assert.Equal(expected, result);
        Assert.Equal(new[] { uid }, requested);
    }

    [Fact]
    public void MatchingVisibleIdentityWinsWithoutEngineLookup()
    {
        Assert.Equal("黑魔導", DuelDestructionPolicy.ResolveName(3, 29, 29, "黑魔導",
            _ => throw new Exception("must not query another source")));
    }

    [Theory]
    [InlineData(3, 29, 17)]
    [InlineData(3, 0, 0)]
    [InlineData(13, 29, 29)]
    public void ConflictingMissingOrNonFieldTargetsRemainUnknown(int locate, int uid, int observed)
    {
        Assert.Null(DuelDestructionPolicy.ResolveName(locate, uid, observed, "其他卡片",
            _ => throw new Exception("must not guess")));
    }

    [Fact]
    public void MissingTargetDoesNotFallBackToPosition()
    {
        var requested = new List<int>();
        Assert.Null(DuelDestructionPolicy.ResolveName(2, 17, 0, null, uid =>
        {
            requested.Add(uid);
            return uid == 2 ? "錯誤卡名" : null;
        }));
        Assert.Equal(new[] { 17 }, requested);
    }
}
