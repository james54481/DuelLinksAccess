using Xunit;
namespace DuelLinksAccess.Tests;
public sealed class DuelChainPolicyTests
{
    [Theory]
    [InlineData(1638401, 1)]
    [InlineData(327681, 1)]
    [InlineData(3866625, 1)]
    [InlineData(2031618, 2)]
    [InlineData(1, 1)]
    public void RecordedPackedUidDoesNotBecomeTheSpokenLink(int packed, int expected)
        => Assert.Equal(expected, DuelChainPolicy.DecodeLink(packed));
    [Theory]
    [InlineData(0)]
    [InlineData(2031616)]
    public void MissingLinkUsesGenericAnnouncement(int packed)
        => Assert.Null(DuelChainPolicy.DecodeLink(packed));
    [Fact]
    public void SignedUpperWordCannotChangeTheLinkNumber()
        => Assert.Equal(2, DuelChainPolicy.DecodeLink(unchecked((int)0x80000002)));
}
