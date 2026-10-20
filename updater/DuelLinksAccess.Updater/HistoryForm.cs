namespace DuelLinksAccess.Updater;

internal sealed class HistoryForm : Form
{
    private readonly ListBox releases = new() { AccessibleName = "GitHub 正式模組版本", Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly TextBox notes = new() { AccessibleName = "選定版本更新說明", ReadOnly = true, Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
    public UpdateRelease? SelectedRelease { get; private set; }

    public HistoryForm(IReadOnlyList<HistoricalRelease> history, Func<UpdateRelease, IWin32Window, bool> confirm)
    {
        Text = "歷史版本管理（GitHub Releases）";
        Size = new Size(830, 690); MinimumSize = new Size(650, 540);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Microsoft JhengHei UI", 11F); AutoScaleMode = AutoScaleMode.Dpi;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 5 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { AutoSize = true, Text = $"正式模組版本 {history.Count} 個。上下鍵選擇，Tab 閱讀說明，再 Tab 至下載按鈕。" }, 0, 0);
        layout.Controls.Add(releases, 0, 1);
        layout.Controls.Add(new Label { Text = "選定版本的更新說明：", AutoSize = true }, 0, 2);
        layout.Controls.Add(notes, 0, 3);
        var actions = new FlowLayoutPanel { AccessibleName = "歷史版本操作", Dock = DockStyle.Fill, AutoSize = true };
        var install = new Button { Text = "下載並安裝選定版本(&I)", AutoSize = true, Enabled = false };
        var close = new Button { Text = "關閉(&X)", AutoSize = true, DialogResult = DialogResult.Cancel };
        actions.Controls.AddRange([install, close]); layout.Controls.Add(actions, 0, 4); Controls.Add(layout);
        releases.TabIndex = 0; notes.TabIndex = 1; actions.TabIndex = 2;
        install.TabIndex = 0; close.TabIndex = 1; CancelButton = close; AcceptButton = install;
        releases.FormattingEnabled = true;
        releases.Format += (_, e) =>
        {
            if (e.ListItem is HistoricalRelease r)
                e.Value = $"版本 {r.Version}，發布日期 {r.PublishedAt.ToLocalTime():yyyy-MM-dd HH:mm}" + (r.Asset == null ? "，無法安裝" : "");
        };
        foreach (var release in history) releases.Items.Add(release);
        releases.SelectedIndexChanged += (_, _) =>
        {
            if (releases.SelectedItem is not HistoricalRelease selected) return;
            notes.Text = $"版本 {selected.Version}\r\n發布日期 {selected.PublishedAt.ToLocalTime():yyyy-MM-dd HH:mm}\r\n\r\n"
                + (string.IsNullOrWhiteSpace(selected.Notes) ? "此版本未提供更新說明。" : ReleaseNotesText.ForDisplay(selected.Notes))
                + (selected.Asset == null ? "\r\n\r\n無法安裝：" + selected.UnavailableReason : "");
            notes.Select(0, 0); install.Enabled = selected.Asset != null;
        };
        install.Click += (_, _) =>
        {
            if (releases.SelectedItem is not HistoricalRelease selected || selected.Asset == null || !confirm(selected.Asset, this)) return;
            SelectedRelease = selected.Asset; DialogResult = DialogResult.OK; Close();
        };
        Shown += (_, _) =>
        {
            if (releases.Items.Count > 0) releases.SelectedIndex = 0;
            else notes.Text = "倉庫目前沒有正式發布的模組版本。";
            releases.Focus();
        };
    }
}
