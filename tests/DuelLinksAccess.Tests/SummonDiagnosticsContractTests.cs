using System.Text.RegularExpressions;
using Xunit;

namespace DuelLinksAccess.Tests;

public sealed class SummonDiagnosticsContractTests
{
    private static string Source => File.ReadAllText(Path.Combine(
        LocalizationCatalogTests.FindRepositoryRoot(), "src", "SummonDiagnostics.cs"));

    [Fact]
    public void ObserversCannotSkipOriginalOrRewriteArguments()
    {
        string source = Source;
        var observers = Regex.Matches(source, @"nameof\((?<name>\w+)\)");
        Assert.Equal(10, observers.Count);
        foreach (Match observer in observers)
            Assert.Matches(@"private static void\s+" + observer.Groups["name"].Value + @"\(", source);
        Assert.DoesNotMatch(@"\b(ref|out)\s+\w+\s+(__\w+|player|index)", source);
        Assert.DoesNotMatch(@"__result|__runOriginal|__\d+\s*=(?!=)", source);
    }

    [Fact]
    public void DiagnosticCodeNeverDispatchesGameCommandsOrInput()
    {
        // Quoted method names register observers; actual calls would mutate play.
        Assert.DoesNotMatch(@"\.\s*(OnDoCardCommand|OnCommand|OnClickCard|SelectIndex|OnDecide|OnCancel|OnTapLocator|DLL_DuelComDoCommand|DLL_DuelListSetIndex)\s*\(", Source);
        Assert.DoesNotMatch(@"ScreenReader\.|SendInput|File\.Write|WebRequest|HttpClient", Source);
        Assert.DoesNotMatch(@"\[HarmonyPatch", Source);
    }

    [Fact]
    public void DiagnosticsAreBoundedAndDoNotClaimUnknownCallersAreMouseInput()
    {
        Assert.Contains("Math.Min(16,", Source);
        Assert.Contains("_fieldReadsRemaining = 3", Source);
        Assert.Contains("UNKNOWN-use-test-order", Source);
        Assert.Contains("UNAVAILABLE", Source);
    }
}
