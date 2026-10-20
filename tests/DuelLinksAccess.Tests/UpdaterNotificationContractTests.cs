using Xunit;
namespace DuelLinksAccess.Tests;

public sealed class UpdaterNotificationContractTests
{
    private static string Source => File.ReadAllText(Path.Combine(LocalizationCatalogTests.FindRepositoryRoot(),
        "updater", "DuelLinksAccess.Updater", "MainForm.cs"));

    [Fact]
    public void CompletedInstallationRaisesNotificationOnlyAfterSuccessfulReplacement()
    {
        string install = Source[Source.IndexOf("private async Task InstallAsync()")..Source.IndexOf("private async Task RestoreSelectedAsync(")];
        Assert.True(install.IndexOf("installer.Install(") < install.IndexOf("NotifyCompletion("));
        Assert.Contains("模組更新完成，目前版本 {release.Version}", install);
    }

    [Fact]
    public void RestoredVersionIsAnnouncedWithOfflineNotes()
    {
        string restore = Source[Source.IndexOf("private async Task RestoreSelectedAsync(")..Source.IndexOf("private void Browse()")];
        Assert.True(restore.IndexOf("installer.Restore(") < restore.IndexOf("NotifyCompletion("));
        Assert.DoesNotContain("GetVersionNotesAsync(", restore);
        Assert.Contains("模組已還原至版本 {restored.Version}", restore);
        Assert.Contains("notesCache.TryGetValue", restore);
    }

    [Fact]
    public void CompletionUsesWindowsNotificationAndDoesNotRequireRefocusingStatus()
    {
        Assert.Contains("RaiseAutomationNotification(", Source);
        Assert.Contains("AutomationNotificationKind.ActionCompleted", Source);
        Assert.Contains("AutomationNotificationProcessing.ImportantMostRecent", Source);
        Assert.Contains("if (!completionNotified) status.Focus();", Source);
        Assert.Contains("if (!sent)", Source);
        Assert.Contains("MessageBox.Show(this, message", Source);
    }
}
