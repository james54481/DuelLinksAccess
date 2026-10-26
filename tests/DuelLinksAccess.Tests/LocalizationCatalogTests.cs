using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace DuelLinksAccess.Tests;

public sealed class LocalizationCatalogTests
{
    private static readonly Regex LiteralLookupRegex = new(
        "Loc\\.Get\\(\\s*\"(?<key>[^\"]+)\"", RegexOptions.Compiled);

    [Theory]
    [InlineData("en", 547)]
    [InlineData("zh-TW", 547)]
    public void Catalog_HasNoDuplicateDefinitions(string locale, int count)
    {
        // ReadCatalog rejects duplicate JSON keys rather than silently overwriting.
        Assert.Equal(count, LocalizationCatalog.ReadCatalog(locale).Count);
    }

    [Fact]
    public void Catalog_DefinesEveryLiteralLookup()
    {
        string root = FindRepositoryRoot();
        var definitions = LocalizationCatalog.ReadCatalog("en");
        string[] missing = Directory.GetFiles(Path.Combine(root, "src"), "*.cs",
                SearchOption.AllDirectories)
            .SelectMany(path => LiteralLookupRegex.Matches(File.ReadAllText(path)))
            .Select(match => match.Groups["key"].Value).Distinct(StringComparer.Ordinal)
            .Where(key => !definitions.ContainsKey(key)).OrderBy(key => key).ToArray();
        Assert.Empty(missing);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("zh-TW")]
    public void Catalog_HasValidCompositeFormatStrings(string locale)
    {
        object[] arguments = Enumerable.Range(0, 100).Cast<object>().ToArray();
        var invalid = LocalizationCatalog.ReadCatalog(locale)
            .Where(entry => !CanFormat(entry.Value, arguments)).Select(entry => entry.Key);
        Assert.Empty(invalid);
    }

    private static bool CanFormat(string template, object[] arguments)
    {
        try { string.Format(CultureInfo.InvariantCulture, template, arguments); return true; }
        catch (FormatException) { return false; }
    }

    internal static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DuelLinksAccess.csproj")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Repository root not found");
    }
}
