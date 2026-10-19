using Xunit;
namespace DuelLinksAccess.Tests;

public sealed class DownloadSettingsContractTests
{
    [Fact]
    public void DownloadEntryHasAShortNameAndRetainsTheRealNativeButton()
    {
        Assert.Equal("語言與下載", LocalizationCatalog.Load("zh-TW").Get("settings_download"));
        string settings = File.ReadAllText(Path.Combine(LocalizationCatalogTests.FindRepositoryRoot(), "src", "DialogHandler.Settings.cs"));
        Assert.True(settings.IndexOf("FindDownloadSectionButtons(vc, _items)") < settings.IndexOf("_items.Clear()"));
        Assert.Contains("vc.voiceText.transform.IsChildOf(section)", settings);
        Assert.Contains("vc.cardText.transform.IsChildOf(section)", settings);
        Assert.Contains("section != vc.content0.transform", settings);
        Assert.Contains("item.SettingsTarget = item.Go", settings);
        Assert.Contains("if (downloadButtons.Count == 0)", settings);
    }
    private static string Source => File.ReadAllText(Path.Combine(
        LocalizationCatalogTests.FindRepositoryRoot(), "src", "DialogHandler.DownloadSettings.cs"));

    [Fact]
    public void LanguageAndVoiceUseSeparateNativeSelectorsAndGameProvidedNames()
    {
        Assert.Contains("vc.languageText.text", Source);
        Assert.Contains("vc.voiceText.text", Source);
        Assert.Contains("() => vc.OnLanguage()", Source);
        Assert.Contains("() => vc.OnVoice()", Source);
        Assert.DoesNotContain("vc.langId =", Source);
        Assert.DoesNotContain("vc.voiceId =", Source);
    }

    [Fact]
    public void DownloadRequiresItsOwnNativeButtonActionAndCurrentDialog()
    {
        Assert.Contains("GetTopVcInstanceId(_activeManager) == root.GetInstanceID()", Source);
        Assert.Contains("!selectable.IsInteractable()", Source);
        Assert.Contains("vc.downloadButton.OnPointerClick", Source);
        Assert.DoesNotContain("vc.OnOkButton()", Source);
        Assert.Contains("vc.OnBack(vc.cancelButton)", Source);
    }
}
