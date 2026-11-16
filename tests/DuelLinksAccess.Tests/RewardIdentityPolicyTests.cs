using Xunit;
namespace DuelLinksAccess.Tests;
public class RewardIdentityPolicyTests
{
    [Theory] [InlineData("1", 1)] [InlineData("207", 207)]
    public void LegacyNumericItemsRemainSupported(string value, int expected)
        => Assert.Equal(expected, RewardIdentityPolicy.LegacyId(value));
    [Theory] [InlineData(null)] [InlineData("")] [InlineData("0")]
    [InlineData("-1")] [InlineData("2147483648")]
    [InlineData("iid/Tc-11-300121~551300121")]
    public void StructuredOrInvalidIdsAreNeverMisreadAsIntegers(string value)
        => Assert.Null(RewardIdentityPolicy.LegacyId(value));
    [Fact] public void VerifiedTicketIconHasItsOwnLabel()
        => Assert.Equal("reward_sr_ur_choice_ticket", RewardIdentityPolicy.SpriteKey("ItemTc0061"));
    [Theory] [InlineData("ItemTc0062")] [InlineData("ItemTc0061_extra")] [InlineData(null)]
    public void UnknownTicketIconsAreNotGuessed(string sprite)
        => Assert.Null(RewardIdentityPolicy.SpriteKey(sprite));
}
