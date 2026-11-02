using Xunit;
namespace DuelLinksAccess.Tests;
public class GateCostPolicyTests
{
    [Theory]
    [InlineData("ItemA0100", 100)]
    [InlineData("ItemA0114", 114)]
    [InlineData("ItemA0548", 548)]
    [InlineData("ItemA0100_other", null)]
    [InlineData("icon", null)]
    public void ReadsOnlyExactNativeItemIcons(string sprite, int? id)
        => Assert.Equal(id, GateCostPolicy.ItemId(sprite));
    [Theory]
    [InlineData("48", "/545", true, 48, 545)]
    [InlineData("80", "/545", true, 80, 545)]
    [InlineData("40", "/0", true, 40, 0)]
    [InlineData("0", "/212", true, 0, 212)]
    [InlineData("-1", "/545", false, 0, 0)]
    [InlineData("48", "545", false, 0, 0)]
    public void PreservesRequiredAndOwnedOrder(string left, string right, bool valid, int need, int owned)
    {
        Assert.Equal(valid, GateCostPolicy.Counts(left, right, out int actualNeed, out int actualOwned));
        if (valid) { Assert.Equal(need, actualNeed); Assert.Equal(owned, actualOwned); }
    }
}
