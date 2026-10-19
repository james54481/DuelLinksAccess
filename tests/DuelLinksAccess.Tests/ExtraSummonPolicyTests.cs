using Xunit;
namespace DuelLinksAccess.Tests;

public sealed class ExtraSummonPolicyTests
{
    [Fact]
    public void ChoosesIdentityInNativeSortedListInsteadOfOriginalExtraDeckOrdinal()
        => Assert.Equal(1, ExtraSummonPolicy.FindVisual(74, new[] {72,74}, new[] {1,0}, new[] {74,72}));

    [Fact]
    public void RefusesDifferentCardAtRecycledVisualIndex()
        => Assert.Equal(-1, ExtraSummonPolicy.FindVisual(74, new[] {74}, new[] {1}, new[] {74,72}));

    [Fact]
    public void MissingWantedCardNeverDefaultsToFirstLegalMonster()
        => Assert.Equal(-1, ExtraSummonPolicy.FindVisual(74, new[] {72}, new[] {0}, new[] {72}));

    [Fact]
    public void InvalidPooledEntriesDoNotOverrideLiveSelection()
        => Assert.Equal(1, ExtraSummonPolicy.FindVisual(74, new[] {74,74,72}, new[] {-1,0,24}, new[] {74,72}));

    [Fact]
    public void UnknownIdentityAndUnpopulatedListAreRejected()
    {
        Assert.Equal(-1, ExtraSummonPolicy.FindVisual(0, new[] {0}, new[] {0}, new[] {0}));
        Assert.Equal(-1, ExtraSummonPolicy.FindVisual(74, null, new[] {0}, new[] {74}));
        Assert.Equal(-1, ExtraSummonPolicy.FindVisual(74, new[] {74}, new[] {0}, null));
    }

    [Fact]
    public void ExtraEntryRequestsNativeListInsteadOfSendingCardOrdinal()
    {
        string source = File.ReadAllText(Path.Combine(LocalizationCatalogTests.FindRepositoryRoot(), "src", "DuelFieldNavigator.cs"));
        int start = source.IndexOf("private void SummonReadExtraCard(", StringComparison.Ordinal);
        int end = source.IndexOf("private void OpenActions(", start, StringComparison.Ordinal);
        string path = source[start..end];
        Assert.Contains("worker.OnDoCardCommand(player, LocateExtra, ExtraSummonPolicy.PileEntryIndex,", path);
        Assert.DoesNotContain("worker.OnDoCardCommand(player, LocateExtra, commandIndex", path);
        Assert.DoesNotContain("worker.OnDoCardCommand(player, LocateExtra, browseSlot", path);
        Assert.DoesNotContain("worker.OnTapLocator(", path);
        Assert.Contains("MenuActType.MainPhase", path);
        Assert.Contains("awaiting user Enter", path);
    }
}
