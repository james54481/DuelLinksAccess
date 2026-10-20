using Xunit;
namespace DuelLinksAccess.Tests;

public sealed class ManualUpdaterContractTests
{
    private static string Read(string name) => File.ReadAllText(Path.Combine(LocalizationCatalogTests.FindRepositoryRoot(),
        "updater", "DuelLinksAccess.Updater", name));

    [Fact]
    public void StartupHasNoGithubRequestAutomaticControlOrInstallation()
    {
        string source = Read("MainForm.cs");
        string startup = source[source.IndexOf("Shown +=")..source.IndexOf("FormClosed +=")];
        Assert.DoesNotContain("CheckAsync(", startup);
        Assert.DoesNotContain("GetNotesAsync(", startup);
        Assert.DoesNotContain("InstallAsync(", startup);
        Assert.DoesNotContain("SavePreferences(", startup);
        Assert.Contains("ReadInstalledVersion()", startup);
        Assert.DoesNotContain("CheckBox", source);
        Assert.DoesNotContain("automatic", source);
    }

    [Fact]
    public void CheckOnlyShowsNotesAndFocusesThemWithoutDownloadConfirmation()
    {
        string source = Read("MainForm.cs");
        string check = source[source.IndexOf("private async Task CheckAsync()")..source.IndexOf("private async Task InstallAsync()")];
        Assert.Contains("focusAfterOperation = notes", check);
        Assert.Contains("發現新版本，請先閱讀更新說明，再決定是否下載安裝", check);
        Assert.DoesNotContain("MessageBox.Show", check);
        Assert.DoesNotContain("InstallAsync(", check);
        Assert.DoesNotContain("DownloadAsync(", check);
    }

    [Fact]
    public void DownloadRequiresExplicitYesAndNoKeepsNotes()
    {
        string source = Read("MainForm.cs");
        string action = source[source.IndexOf("update.Click +=")..source.IndexOf("restore.Click +=")];
        Assert.Contains("確定要下載並安裝此版本嗎？", action);
        Assert.Contains("MessageBoxDefaultButton.Button2", action);
        Assert.Contains("!= DialogResult.Yes", action);
        Assert.Contains("notes.Focus(); return;", action);
        Assert.True(action.IndexOf("DialogResult.Yes") < action.IndexOf("RunAsync(InstallAsync)"));
    }

    [Fact]
    public void HistoryInstallationRequiresConfirmationAndSupportsNativeKeyboardControls()
    {
        string source = Read("MainForm.cs"), history = Read("HistoryForm.cs");
        Assert.Contains("目前版本 {current}，目標版本 {selected.Version}", source);
        Assert.Contains("目前版本會先備份", source);
        Assert.Contains("!confirm(selected.Asset, this)", history);
        Assert.Contains("SelectedRelease = selected.Asset", history);
        Assert.Contains("MessageBoxButtons.YesNoCancel", source);
        Assert.Contains("releases.TabIndex = 0; notes.TabIndex = 1", history);
        Assert.Contains("CancelButton = close; AcceptButton = install", history);
        Assert.DoesNotContain("installer.", history);
        Assert.Contains("selected.Asset != null", history);
    }

    [Fact]
    public void HistoryUsesGithubWhileEmergencyRestoreStaysOffline()
    {
        string source = Read("MainForm.cs");
        string history = source[source.IndexOf("private async Task OpenHistoryAsync()")..source.IndexOf("private bool ConfirmHistoricalInstall")];
        Assert.Contains("GetHistoryAsync(", history);
        Assert.DoesNotContain("GetBackups", history);
        string restore = source[source.IndexOf("private async Task RestoreSelectedAsync(")..source.IndexOf("private void Browse()")];
        Assert.DoesNotContain("GetNotesAsync", restore);
        Assert.DoesNotContain("ReleaseClient(http)", restore);
        Assert.Contains("notesCache.TryGetValue", restore);
    }
}
