using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace DuelLinksAccess.Tests;

public sealed class LocalizationSupportTests
{
    [Theory]
    [InlineData("en", "1.5")]
    [InlineData("zh-TW", "1.5")]
    public void CatalogInitializationAndFormatting_DoNotRequireNamedCultures(string locale, string expected)
    {
        var catalog = LocalizationCatalog.Load(locale);
        Assert.Equal(locale, catalog.Locale);
        Assert.Equal(expected, catalog.Get("{0:0.0}", 1.5m));
    }

    [Theory]
    [InlineData("手牌、怪獸區、魔法與陷阱區、墓地、除外區、額外牌組")]
    [InlineData("任务、牌组、对手、生命值")]
    [InlineData("卡片詳情：青眼白龍，攻擊力 3000")]
    [InlineData("漢字")]
    [InlineData("㐀")]
    [InlineData("Enter 返回")]
    public void ChineseLabels_AreNotRejectedAsJapanese(string label)
        => Assert.False(LabelLanguagePolicy.ContainsJapanese(label));

    [Theory]
    [InlineData("こんにちは")]
    [InlineData("カタカナ")]
    [InlineData("ｶｰﾄﾞ")]
    [InlineData("ㇰ")]
    public void JapaneseKana_AreStillDetected(string label)
        => Assert.True(LabelLanguagePolicy.ContainsJapanese(label));

    [Fact]
    public void LabelExtractor_UsesTestedPolicyForAllExistingCallers()
    {
        string source = File.ReadAllText(Path.Combine(
            LocalizationCatalogTests.FindRepositoryRoot(), "src", "LabelExtractor.cs"));
        Assert.Matches(@"public static bool ContainsJapanese\(string s\)\s*=> LabelLanguagePolicy.ContainsJapanese\(s\);", source);
    }

    [Fact]
    public void ZhTW_ReturnsTranslatedTextAndFormatsGameTextWithoutTranslation()
    {
        var catalog = LocalizationCatalog.Load("zh-TW");
        Assert.Equal("手牌", catalog.Get("duel_zone_hand"));
        Assert.Equal("對手", catalog.Get("duel_opponent"));
        Assert.Equal("召喚了青眼白龍", catalog.Get("duel_summoned", "青眼白龍"));
        Assert.Equal("YourGameText，第 2 張，共 5 張",
            catalog.Get("duel_card_position", 2, 5, "YourGameText"));
        Assert.Contains("操作說明", catalog.Get("help_text"));
    }

    [Fact]
    public void MissingTranslation_UsesCompleteEnglishFallback()
    {
        // Simulate a future incomplete locale without altering shipped resources.
        var translated = LocalizationCatalog.ReadCatalog("zh-TW");
        translated.Remove("shop_gems");
        translated.Remove("ticket_count");
        var catalog = new LocalizationCatalog("zh-TW", LocalizationCatalog.ReadCatalog("en"), translated);
        Assert.Equal("Gems", catalog.Get("shop_gems"));
        Assert.Equal("7 tickets remaining", catalog.Get("ticket_count", 7));
        Assert.Equal("Hand", LocalizationCatalog.Load("en").Get("duel_zone_hand"));
        Assert.Equal("en", LocalizationCatalog.Load("unknown-locale").Locale);
        Assert.Equal("Hand", LocalizationCatalog.Load("unknown-locale").Get("duel_zone_hand"));
    }

    [Fact]
    public void CompleteZhTW_CoversEveryEnglishKeyAndNewModules()
    {
        var english = LocalizationCatalog.ReadCatalog("en");
        var translated = LocalizationCatalog.ReadCatalog("zh-TW");
        Assert.Equal(english.Keys.OrderBy(k => k), translated.Keys.OrderBy(k => k));
        Assert.Equal("寶石", translated["shop_gems"]);
        Assert.Equal("主牌組", translated["deck_zone_main_name"]);
        Assert.Equal("卡片商人", translated["trader_entered"]);
        Assert.Equal("任務", translated["home_missions"]);
        Assert.Equal("剩餘 7 張票券", LocalizationCatalog.Load("zh-TW").Get("ticket_count", 7));
        Assert.Contains("左、右鍵", translated["transfer_entered"]);
    }

    [Fact]
    public void EveryOriginalEnglishValue_IsPreservedExactly()
    {
        string root = LocalizationCatalogTests.FindRepositoryRoot();
        var baseline = JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(Path.Combine(root, "tests", "fixtures", "english-v1.4.2.json")));
        Assert.Equal(443, baseline.Count);
        var english = LocalizationCatalog.ReadCatalog("en");
        foreach (var entry in baseline) Assert.Equal(entry.Value, english[entry.Key]);
    }

    [Fact]
    public void Translations_HaveIdenticalFormatArgumentIndices()
    {
        var english = LocalizationCatalog.ReadCatalog("en");
        var translated = LocalizationCatalog.ReadCatalog("zh-TW");
        var pattern = new Regex(@"(?<!\{)\{(\d+)(?:[^{}]*)\}(?!\})");
        foreach (var entry in translated)
        {
            Assert.True(english.ContainsKey(entry.Key), entry.Key);
            string[] Indices(string value) => pattern.Matches(value)
                .Select(match => match.Groups[1].Value).Distinct().OrderBy(x => x).ToArray();
            Assert.Equal(Indices(english[entry.Key]), Indices(entry.Value));
        }
    }

    [Fact]
    public void AllDuelAndGeneralOperationKeys_HavePhaseOneTranslations()
    {
        var english = LocalizationCatalog.ReadCatalog("en");
        var translated = LocalizationCatalog.ReadCatalog("zh-TW");
        string[] requiredPrefixes = { "duel_", "surrender_", "screen_", "dialog_", "tutorial_", "ageverify_" };
        foreach (string key in english.Keys.Where(key => requiredPrefixes.Any(key.StartsWith)))
            Assert.True(translated.ContainsKey(key), "Missing phase-one translation: " + key);
        Assert.True(translated.ContainsKey("mod_loaded"));
        Assert.True(translated.ContainsKey("help_text"));
    }
}
