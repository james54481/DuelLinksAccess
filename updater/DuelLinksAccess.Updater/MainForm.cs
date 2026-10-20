using System.Diagnostics;
using System.Windows.Forms.Automation;

namespace DuelLinksAccess.Updater;

internal sealed class MainForm : Form
{
    private readonly TextBox path = new() { AccessibleName = "Duel Links 遊戲安裝目錄", Dock = DockStyle.Fill };
    private readonly Label installedLabel = new() { Text = "目前模組版本：未選擇遊戲目錄", AutoSize = true };
    private readonly TextBox status = new() { AccessibleName = "更新狀態", ReadOnly = true, Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
    private readonly TextBox notes = new() { AccessibleName = "版本更新說明", ReadOnly = true, Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
    private readonly Button browse = new() { Text = "瀏覽(&B)", AutoSize = true };
    private readonly Button check = new() { Text = "檢查更新(&C)", AutoSize = true };
    private readonly Button update = new() { Text = "下載並更新(&U)", AutoSize = true, Enabled = false };
    private readonly Button restore = new() { Text = "還原上次備份(&R)", AutoSize = true };
    private readonly Button history = new() { Text = "歷史版本管理(&H)", AutoSize = true };
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(3) };
    private readonly string config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DuelLinksAccessUpdater", "settings.json");
    private readonly Installer installer = new(GameRunning);
    private CancellationTokenSource? cancel;
    private readonly ManualUpdateSession session;
    private readonly Dictionary<Version, string?> notesCache = new();
    private bool busy;
    private bool completionNotified;
    private Control? focusAfterOperation;
    private string? pendingAnnouncement;

    public MainForm(bool preview = false)
    {
        session = new ManualUpdateSession(new ReleaseClient(http));
        Text = "DuelLinksAccess 模組更新程式 0.1.2";
        if (preview) Text += "（介面預覽，不進行更新）";
        MinimumSize = new Size(650, 540);
        Size = new Size(820, 680);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Microsoft JhengHei UI", 11F);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = 10 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        for (int i = 0; i < 10; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles[5] = new RowStyle(SizeType.Percent, 40);
        layout.RowStyles[7] = new RowStyle(SizeType.Percent, 60);
        void Wide(Control control, int row) { layout.Controls.Add(control, 0, row); layout.SetColumnSpan(control, 2); }
        Wide(new Label { Text = "更新來源：github.com/" + ReleaseClient.Repository + "（正式 Release）", AutoSize = true }, 0);
        Wide(new Label { Text = "遊戲安裝目錄：", AutoSize = true }, 1);
        layout.Controls.Add(path, 0, 2); layout.Controls.Add(browse, 1, 2);
        Wide(installedLabel, 3);
        Wide(new Label { Text = "更新狀態：", AutoSize = true }, 4);
        Wide(status, 5);
        Wide(new Label { Text = "版本更新說明：", AutoSize = true }, 6);
        Wide(notes, 7);
        var buttons = new FlowLayoutPanel { AccessibleName = "更新操作", AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
        var close = new Button { Text = "關閉(&X)", AutoSize = true };
        buttons.Controls.AddRange([check, update, restore, history, close]); Wide(buttons, 8);
        if (preview)
        {
            var testNotification = new Button { Text = "測試完成通知(&T)", AutoSize = true };
            testNotification.Click += (_, _) => NotifyCompletion("測試通知：模組更新已完成。這只是通知測試，未更新任何檔案。");
            buttons.Controls.Add(testNotification);
        }
        Wide(new Label { Text = "只更新既有模組；不啟動遊戲。備份可用「還原上次備份」復原。", AutoSize = true }, 9);
        Controls.Add(layout);
        path.TabIndex = 0; browse.TabIndex = 1;
        status.TabIndex = 3; notes.TabIndex = 4; buttons.TabIndex = 5;
        check.TabIndex = 0; update.TabIndex = 1; restore.TabIndex = 2; history.TabIndex = 3; close.TabIndex = 4;
        browse.Click += (_, _) => Browse();
        check.Click += async (_, _) => await RunAsync(CheckAsync);
        update.Click += async (_, _) =>
        {
            var release = session.Available;
            if (release == null) return;
            if (MessageBox.Show(this, $"目前版本 {session.InstalledVersion}，目標版本 {release.Version}。\r\n確定要下載並安裝此版本嗎？",
                "確認下載安裝", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            { notes.Focus(); return; }
            await RunAsync(InstallAsync);
        };
        restore.Click += async (_, _) =>
        {
            try
            {
                var inventory = installer.GetBackups(path.Text);
                if (inventory.Backups.Count == 0) { Say("沒有有效備份可還原。"); status.Focus(); return; }
                var selected = inventory.Backups[0];
                if (ConfirmRestore(selected, this)) await RunAsync(() => RestoreSelectedAsync(selected));
            }
            catch (Exception ex) { Say("無法讀取備份：" + ex.Message); status.Focus(); }
        };
        history.Click += async (_, _) => await OpenHistoryAsync();
        path.TextChanged += (_, _) =>
        {
            session.Clear(); update.Enabled = false; notes.Clear();
            installedLabel.Text = "目前模組版本：請確認遊戲目錄";
        };
        close.Click += (_, _) => Close();
        FormClosing += (_, e) => { if (busy) { e.Cancel = true; cancel?.Cancel(); Say("正在取消，完成後可關閉視窗。"); } };
        Shown += (_, _) =>
        {
            try
            {
                if (preview)
                {
                    path.Text = "介面測試：未選擇遊戲資料夾";
                    check.Enabled = update.Enabled = restore.Enabled = history.Enabled = browse.Enabled = path.Enabled = false;
                    Say("介面預覽模式：不連線、不下載、不修改遊戲。正式啟動時只讀取本機版本，按檢查更新後才連線。");
                    BeginInvoke(new Action(() => status.Focus()));
                    return;
                }
                if (File.Exists(config))
                {
                    path.Text = UpdaterPreferences.Parse(File.ReadAllText(config)).GamePath ?? "";
                }
                if (string.IsNullOrWhiteSpace(path.Text)) { Say("請選擇已安裝模組的遊戲資料夾，再按檢查更新。"); return; }
                var installed = ReadInstalledVersion();
                Say($"目前模組版本 {installed}。請按檢查更新，程式不會自動連線或安裝。");
            }
            catch (Exception ex) { Say("讀取設定失敗：" + ex.Message); }
        };
        FormClosed += (_, _) => { http.Dispose(); cancel?.Dispose(); };
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (busy) return;
        busy = true; completionNotified = false; focusAfterOperation = null; pendingAnnouncement = null; cancel = new CancellationTokenSource();
        check.Enabled = update.Enabled = restore.Enabled = history.Enabled = browse.Enabled = path.Enabled = false;
        try { await action(); }
        catch (OperationCanceledException) { Say("作業已取消，尚未替換的檔案保持原樣。"); }
        catch (UnauthorizedAccessException) { Say("沒有寫入權限。請先關閉遊戲，再手動以系統管理員身分開啟本更新程式；程式不會自行提權。"); }
        catch (Exception ex) { Say("作業未完成：" + ex.Message); }
        finally
        {
            busy = false;
            check.Enabled = restore.Enabled = history.Enabled = browse.Enabled = path.Enabled = true;
            update.Enabled = session.Available != null;
            cancel.Dispose(); cancel = null;
            if (focusAfterOperation != null)
            {
                focusAfterOperation.Focus(); notes.Select(0, 0);
                if (pendingAnnouncement != null) RaiseNotification(pendingAnnouncement);
            }
            else if (!completionNotified) status.Focus();
        }
    }

    private async Task CheckAsync()
    {
        var installed = ReadInstalledVersion();
        SavePreferences();
        notes.Clear();
        Say($"目前模組版本 {installed}。正在檢查更新……");
        var release = await session.CheckAsync(path.Text, cancel!.Token);
        installedLabel.Text = $"目前模組版本：{session.InstalledVersion}";
        notes.Text = release == null ? "" : $"版本 {release.Version}\r\n\r\n{ReleaseNotesText.ForDisplay(release.Notes)}";
        if (release == null) { Say("倉庫目前沒有可用的正式 Release，尚未更新任何檔案。"); return; }
        notesCache[ReleaseClient.Normalize(release.Version)] = release.Notes;
        if (session.Available == null)
        { Say($"目前已經是最新版本，目前版本 {installed}。"); return; }
        const string prompt = "發現新版本，請先閱讀更新說明，再決定是否下載安裝";
        Say($"找到新版 {release.Version}，大小 {release.Size:N0} 位元組。{prompt}。");
        notes.Text = prompt + $"。\r\n版本 {release.Version}\r\n\r\n" + ReleaseNotesText.ForDisplay(release.Notes);
        focusAfterOperation = notes; pendingAnnouncement = prompt;
    }

    private async Task InstallAsync()
    {
        if (session.Available == null) return;
        if (GameRunning()) throw new InvalidOperationException("請先完全關閉 Duel Links，再更新。");
        Say("正在下載新版並驗證，請稍候……");
        var release = session.Available;
        byte[] bytes = await new ReleaseClient(http).DownloadAsync(release, cancel!.Token);
        cancel.Token.ThrowIfCancellationRequested();
        string backup = installer.Install(path.Text, bytes, release);
        ReadInstalledVersion();
        Say($"已更新至 {release.Version}，檔案驗證通過。原版備份：{backup}");
        NotifyCompletion($"DuelLinksAccess 模組更新完成，目前版本 {release.Version}。");
    }

    private async Task RestoreSelectedAsync(BackupInfo selected)
    {
        var restored = installer.Restore(path.Text, selected);
        ReadInstalledVersion();
        string result = $"已還原至模組版本 {restored.Version}。目前版本另已備份至：{restored.BackupPath}";
        Say(result);
        NotifyCompletion($"DuelLinksAccess 模組已還原至版本 {restored.Version}。");
        // Emergency restore is fully offline, including release notes.
        notes.Text = $"版本 {restored.Version}\r\n\r\n" +
            (notesCache.TryGetValue(ReleaseClient.Normalize(restored.Version), out var text)
                ? ReleaseNotesText.ForDisplay(text) : "離線還原完成；本次工作階段未快取此版本的更新說明。");
        await Task.CompletedTask;
    }

    private void Browse()
    {
        using var dialog = new FolderBrowserDialog { Description = "選擇含 dlpc.exe 的遊戲安裝資料夾", UseDescriptionForTitle = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            path.Text = dialog.SelectedPath;
            try { var version = ReadInstalledVersion(); SavePreferences(); Say($"目前模組版本 {version}。請按檢查更新。"); }
            catch (Exception ex) { Say("無法讀取模組版本：" + ex.Message); }
        }
    }

    private Version ReadInstalledVersion()
    {
        var installed = session.LoadInstalled(path.Text);
        installedLabel.Text = $"目前模組版本：{installed}";
        update.Enabled = false;
        return installed;
    }

    private void SavePreferences()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(config)!);
        File.WriteAllText(config, new UpdaterPreferences(path.Text).ToJson());
    }

    private bool ConfirmRestore(BackupInfo selected, IWin32Window owner)
    {
        string mods = ModFile.ValidateGameFolder(path.Text);
        var current = ModFile.ReadVersion(File.ReadAllBytes(Path.Combine(mods, ReleaseClient.DllName)));
        return MessageBox.Show(owner, $"目前版本 {current}，目標版本 {selected.Version}。\r\n備份時間 {selected.SavedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}。\r\n確定要還原此版本嗎？目前版本會先備份。",
            "確認還原版本", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    }

    private async Task OpenHistoryAsync()
    {
        IReadOnlyList<HistoricalRelease>? releases = null;
        await RunAsync(async () =>
        {
            Say("正在連接 GitHub 取得正式歷史版本……");
            releases = await new ReleaseClient(http).GetHistoryAsync(cancel!.Token);
            foreach (var release in releases) notesCache[ReleaseClient.Normalize(release.Version)] = release.Notes;
            Say($"已取得 {releases.Count} 個正式模組版本，尚未下載或安裝。");
        });
        if (releases == null) { Say("無法取得 GitHub 歷史版本清單。現有 DLL 未修改。\r\n" + status.Text); status.Focus(); return; }
        using var dialog = new HistoryForm(releases, ConfirmHistoricalInstall);
        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.SelectedRelease is { } selected)
            await RunAsync(() => InstallHistoricalAsync(selected));
    }

    private bool ConfirmHistoricalInstall(UpdateRelease selected, IWin32Window owner)
    {
        try
        {
            string mods = ModFile.ValidateGameFolder(path.Text);
            var current = ModFile.ReadVersion(File.ReadAllBytes(Path.Combine(mods, ReleaseClient.DllName)));
            return MessageBox.Show(owner, $"目前版本 {current}，目標版本 {selected.Version}。\r\n確定要下載並安裝此版本嗎？允許降版或重新安裝相同版本，目前版本會先備份。",
                "確認安裝歷史版本", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner, "無法確認目前模組版本，未開始安裝。\r\n" + ex.Message,
                "無法安裝", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private async Task InstallHistoricalAsync(UpdateRelease release)
    {
        if (GameRunning()) throw new InvalidOperationException("請先完全關閉 Duel Links，再安裝。");
        Say($"正在下載並驗證版本 {release.Version}……");
        byte[] bytes = await new ReleaseClient(http).DownloadAsync(release, cancel!.Token);
        cancel.Token.ThrowIfCancellationRequested();
        string backup = installer.InstallSelected(path.Text, bytes, release);
        ReadInstalledVersion();
        notes.Text = $"版本 {release.Version}\r\n\r\n" + ReleaseNotesText.ForDisplay(release.Notes);
        Say($"已安裝版本 {release.Version}，檔案驗證通過。原版備份：{backup}");
        NotifyCompletion($"DuelLinksAccess 模組安裝完成，目前版本 {release.Version}。");
    }

    private void Say(string text) { status.Text = text; status.SelectionStart = 0; status.SelectionLength = 0; }

    private void NotifyCompletion(string message)
    {
        bool sent = RaiseNotification(message);
        completionNotified = true;
        if (!sent)
            MessageBox.Show(this, message, "作業完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private bool RaiseNotification(string message)
    {
        try
        {
            return status.AccessibilityObject.RaiseAutomationNotification(
                AutomationNotificationKind.ActionCompleted,
                AutomationNotificationProcessing.ImportantMostRecent, message);
        }
        catch { return false; }
    }

    private static bool GameRunning()
    {
        var processes = Process.GetProcessesByName("dlpc");
        bool running = processes.Length > 0;
        foreach (var process in processes) process.Dispose();
        return running;
    }

}
