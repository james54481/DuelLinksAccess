using Xunit;

namespace DuelLinksAccess.Tests;

public class BoostItemPolicyTests
{
    [Theory]
    [InlineData("Response.data.item.0", 301, true)]
    [InlineData("Response.data.item.3", 304, true)]
    [InlineData("Response.data.item.3", 212, false)]
    [InlineData("Response.data.item.3.note", 304, false)]
    [InlineData("Other.data.item.3", 304, false)]
    public void RestrictsLabelsToVerifiedBoostItemRows(string path, int id, bool expected)
        => Assert.Equal(expected, BoostItemPolicy.IsRow(path, id));

    [Theory]
    [InlineData("1/9", "1/9")]
    [InlineData(" 1 ／ 16 ", "1/16")]
    [InlineData("1/9/10", null)]
    [InlineData("pointer123456", null)]
    [InlineData(null, null)]
    public void PreservesOnlyNativeQuantityRatios(string text, string expected)
        => Assert.Equal(expected, BoostItemPolicy.Quantity(text));
}
