using System.Diagnostics;
using System.Text.Json;
using System.Windows.Forms.Automation;

namespace DuelLinksAccess.Updater;

internal sealed class MainForm : Form
{
    private readonly TextBox path = new() { AccessibleName = "Duel Links 遊戲安裝目錄", Dock = DockStyle.Fill };
    private readonly CheckBox automatic = new() { Text = "啟動時自動更新（遊戲關閉時）(&A)", Checked = true, AutoSize = true };
    private readonly TextBox status = new() { AccessibleName = "更新狀態", ReadOnly = true, Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
    private readonly TextBox notes = new() { AccessibleName = "版本更新說明", ReadOnly = true, Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
    private readonly Button browse = new() { Text = "瀏覽(&B)", AutoSize = true };
    private readonly Button check = new() { Text = "檢查更新(&C)", AutoSize = true };
    private readonly Button update = new() { Text = "下載並更新(&U)", AutoSize = true, Enabled = false };
    private readonly Button restore = new() { Text = "還原上次備份(&R)", AutoSize = true };
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(3) };
    private readonly string config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DuelLinksAccessUpdater", "settings.json");
    private readonly Installer installer = new(GameRunning);
    private CancellationTokenSource? cancel;
    private UpdateRelease? latest;
    private bool busy;
    private bool completionNotified;

    public MainForm(bool preview = false)
    {
        Text = "DuelLinksAccess 模組更新程式 0.1.1";
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
        Wide(automatic, 3);
        Wide(new Label { Text = "更新狀態：", AutoSize = true }, 4);
        Wide(status, 5);
        Wide(new Label { Text = "版本更新說明：", AutoSize = true }, 6);
        Wide(notes, 7);
        var buttons = new FlowLayoutPanel { AccessibleName = "更新操作", AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
        var close = new Button { Text = "關閉(&X)", AutoSize = true };
        buttons.Controls.AddRange([check, update, restore, close]); Wide(buttons, 8);
        if (preview)
        {
            var testNotification = new Button { Text = "測試完成通知(&T)", AutoSize = true };
            testNotification.Click += (_, _) => NotifyCompletion("測試通知：模組更新已完成。這只是通知測試，未更新任何檔案。");
            buttons.Controls.Add(testNotification);
        }
        Wide(new Label { Text = "只更新既有模組；不啟動遊戲。備份可用「還原上次備份」復原。", AutoSize = true }, 9);
        Controls.Add(layout);
        path.TabIndex = 0; browse.TabIndex = 1; automatic.TabIndex = 2;
        status.TabIndex = 3; notes.TabIndex = 4; buttons.TabIndex = 5;
        check.TabIndex = 0; update.TabIndex = 1; restore.TabIndex = 2; close.TabIndex = 3;
        browse.Click += (_, _) => Browse();
        check.Click += async (_, _) => await RunAsync(CheckAsync);
        update.Click += async (_, _) => await RunAsync(InstallAsync);
        restore.Click += async (_, _) =>
        {
            if (MessageBox.Show(this, "要還原最近的模組備份嗎？目前版本也會先備份。", "還原模組", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                await RunAsync(RestoreAsync);
        };
        close.Click += (_, _) => Close();
        FormClosing += (_, e) => { if (busy) { e.Cancel = true; cancel?.Cancel(); Say("正在取消，完成後可關閉視窗。"); } };
        Shown += async (_, _) =>
        {
            try
            {
                if (preview)
                {
                    path.Text = "介面測試：未選擇遊戲資料夾";
                    check.Enabled = update.Enabled = restore.Enabled = browse.Enabled = path.Enabled = automatic.Enabled = false;
                    Say("介面預覽模式：不連線、不下載、不修改遊戲。正式啟動時可自動偵測遊戲目錄並檢查新版。");
                    BeginInvoke(new Action(() => status.Focus()));
                    return;
                }
                if (File.Exists(config))
                {
                    var saved = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(config));
                    path.Text = saved?.GamePath ?? ""; automatic.Checked = saved?.Automatic ?? true;
                }
                if (string.IsNullOrWhiteSpace(path.Text)) path.Text = GameLocator.Find().FirstOrDefault() ?? "";
                if (string.IsNullOrWhiteSpace(path.Text)) { Say("請選擇已安裝模組的遊戲資料夾，再按檢查更新。"); return; }
                await RunAsync(CheckAsync);
            }
            catch (Exception ex) { Say("讀取設定失敗：" + ex.Message); }
        };
        FormClosed += (_, _) => { http.Dispose(); cancel?.Dispose(); };
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (busy) return;
        busy = true; completionNotified = false; cancel = new CancellationTokenSource();
        check.Enabled = update.Enabled = restore.Enabled = browse.Enabled = path.Enabled = automatic.Enabled = false;
        try { await action(); }
        catch (OperationCanceledException) { Say("作業已取消，尚未替換的檔案保持原樣。"); }
        catch (UnauthorizedAccessException) { Say("沒有寫入權限。請先關閉遊戲，再手動以系統管理員身分開啟本更新程式；程式不會自行提權。"); }
        catch (Exception ex) { Say("作業未完成：" + ex.Message); }
        finally
        {
            busy = false;
            check.Enabled = restore.Enabled = browse.Enabled = path.Enabled = automatic.Enabled = true;
            update.Enabled = latest != null;
            cancel.Dispose(); cancel = null;
            if (!completionNotified) status.Focus();
        }
    }

    private async Task CheckAsync()
    {
        latest = null;
        string mods = ModFile.ValidateGameFolder(path.Text);
        var installed = ModFile.ReadVersion(File.ReadAllBytes(Path.Combine(mods, ReleaseClient.DllName)));
        Directory.CreateDirectory(Path.GetDirectoryName(config)!);
        File.WriteAllText(config, JsonSerializer.Serialize(new Preferences(path.Text, automatic.Checked)));
        Say($"目前模組版本 {installed}。正在檢查更新……");
        var release = await new ReleaseClient(http).CheckAsync(cancel!.Token);
        notes.Text = release == null ? "" : $"版本 {release.Version}\r\n\r\n{release.Notes}";
        if (release == null) { Say("倉庫目前沒有可用的正式 Release，尚未更新任何檔案。"); return; }
        if (ReleaseClient.Normalize(installed) >= ReleaseClient.Normalize(release.Version))
        { Say($"目前版本 {installed}，不需要更新。"); return; }
        latest = release;
        Say($"找到新版 {release.Version}，大小 {release.Size:N0} 位元組。");
        if (automatic.Checked && !GameRunning()) await InstallAsync();
        else Say(status.Text + (GameRunning() ? " 請先關閉遊戲，再按下載並更新。" : " 按下載並更新即可安裝。"));
    }

    private async Task InstallAsync()
    {
        if (latest == null) return;
        if (GameRunning()) throw new InvalidOperationException("請先完全關閉 Duel Links，再更新。");
        Say("正在下載新版並驗證，請稍候……");
        var release = latest;
        byte[] bytes = await new ReleaseClient(http).DownloadAsync(release, cancel!.Token);
        cancel.Token.ThrowIfCancellationRequested();
        string backup = installer.Install(path.Text, bytes, release);
        latest = null;
        Say($"已更新至 {release.Version}，檔案驗證通過。原版備份：{backup}");
        NotifyCompletion($"DuelLinksAccess 模組更新完成，目前版本 {release.Version}。");
    }

    private async Task RestoreAsync()
    {
        var restored = installer.Restore(path.Text);
        latest = null;
        string result = $"已還原至模組版本 {restored.Version}。目前版本另已備份至：{restored.BackupPath}";
        Say(result);
        NotifyCompletion($"DuelLinksAccess 模組已還原至版本 {restored.Version}。");
        // Clear the newer release's text immediately, even if the network fails.
        notes.Text = $"版本 {restored.Version}\r\n\r\n正在取得此版本的更新說明……";
        try
        {
            string? text = await new ReleaseClient(http).GetNotesAsync(restored.Version, cancel!.Token);
            notes.Text = $"版本 {restored.Version}\r\n\r\n"
                + (string.IsNullOrWhiteSpace(text) ? "此版本未提供更新說明。" : text);
            Say(result + (string.IsNullOrWhiteSpace(text) ? " 此版本未提供更新說明。" : " 已顯示還原版本的更新說明。"));
        }
        catch (Exception ex)
        {
            notes.Text = $"版本 {restored.Version}\r\n\r\n無法取得此版本的更新說明。";
            Say(result + " 還原已成功，但更新說明未能取得：" + ex.Message);
        }
    }

    private void Browse()
    {
        using var dialog = new FolderBrowserDialog { Description = "選擇含 dlpc.exe 的遊戲安裝資料夾", UseDescriptionForTitle = true };
        if (dialog.ShowDialog(this) == DialogResult.OK) { path.Text = dialog.SelectedPath; latest = null; update.Enabled = false; }
    }

    private void Say(string text) { status.Text = text; status.SelectionStart = 0; status.SelectionLength = 0; }

    private void NotifyCompletion(string message)
    {
        bool sent = false;
        try
        {
            sent = status.AccessibilityObject.RaiseAutomationNotification(
                AutomationNotificationKind.ActionCompleted,
                AutomationNotificationProcessing.ImportantMostRecent, message);
        }
        catch { /* The persistent status remains available if UIA is unavailable. */ }
        completionNotified = true;
        if (!sent)
            MessageBox.Show(this, message, "作業完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static bool GameRunning()
    {
        var processes = Process.GetProcessesByName("dlpc");
        bool running = processes.Length > 0;
        foreach (var process in processes) process.Dispose();
        return running;
    }

    private sealed record Preferences(string GamePath, bool Automatic);
}
