using Xunit;
namespace DuelLinksAccess.Tests;

public sealed class ObservedCardIdentityStoreTests
{
    [Fact]
    public void NavigationChainIdentitySurvivesMissingTimingItemFields()
    {
        var store = new ObservedCardIdentityStore();
        store.Observe(ObservedCardIdentityStore.MatchedUid(0, 38, 38), 12470, true);
        Assert.Equal(12470, store.Resolve(ObservedCardIdentityStore.MatchedUid(0, 38, 38), true, false));
        Assert.Equal(0, store.Resolve(40, true, false));
    }

    [Theory]
    [InlineData(0, 38, 38, 38)]
    [InlineData(38, 38, 0, 38)]
    [InlineData(40, 38, 38, 0)]
    [InlineData(0, 38, 40, 0)]
    [InlineData(38, 0, 38, 0)]
    [InlineData(0, 0, 38, 0)]
    public void RequiresNativeVisualAndDataIdentityAgreement(int item, int visual, int mixed, int expected)
        => Assert.Equal(expected, ObservedCardIdentityStore.MatchedUid(item, visual, mixed));

    [Fact]
    public void HiddenOrUnrelatedListsCannotReadRecordedIdentities()
    {
        var store = new ObservedCardIdentityStore();
        store.Observe(38, 12470, true);
        Assert.Equal(0, store.Resolve(38, true, true));
        Assert.Equal(0, store.Resolve(38, false, false));
        store.Observe(40, 12470, false);
        Assert.Equal(0, store.Resolve(40, true, false));
    }

    [Fact]
    public void NewDuelCannotReusePreviousDuelUid()
    {
        var store = new ObservedCardIdentityStore();
        store.Observe(38, 12470, true);
        store.Reset();
        Assert.Equal(0, store.Resolve(38, true, false));
        store.Observe(38, 4041, true);
        Assert.Equal(4041, store.Resolve(38, true, false));
    }

    [Fact]
    public void ConflictingEvidenceFailsClosedUntilReset()
    {
        var store = new ObservedCardIdentityStore();
        store.Observe(38, 12470, true);
        store.Observe(38, 4041, true);
        store.Observe(38, 12470, true);
        Assert.Equal(0, store.Resolve(38, true, false));
    }

    [Theory]
    [InlineData(0, 12470)]
    [InlineData(38, 0)]
    [InlineData(38, 100000)]
    public void RejectsMissingAndInvalidIds(int uid, int id)
    {
        var store = new ObservedCardIdentityStore();
        store.Observe(uid, id, true);
        Assert.Equal(0, store.Resolve(uid, true, false));
    }
}
