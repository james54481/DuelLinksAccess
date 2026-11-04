using System;
using MelonLoader;
using UnityEngine.UI;

namespace DuelLinksAccess
{
    internal static class AccessibleTextEditor
    {
        private static System.Threading.Tasks.Task<string> _pending;
        private static InputField _input;
        private static Func<bool> _isCurrent;
        private static Action _onApplied;

        internal static void Open(InputField input, bool playerName, Func<bool> isCurrent, Action onApplied = null)
        {
            if (_pending != null || input == null) return;
            string initial = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(input.text ?? ""));
            string script = @"
Add-Type -AssemblyName System.Windows.Forms
$form = New-Object Windows.Forms.Form
$form.Text = '" + (playerName ? "玩家名稱輸入" : "好友搜尋") + @"'
$form.Width = 420
$form.Height = 140
$form.StartPosition = 'CenterScreen'
$form.TopMost = $true
$box = New-Object Windows.Forms.TextBox
$box.AccessibleName = '" + (playerName ? "玩家名稱" : "玩家名稱或決鬥者 ID") + @"'
$box.MaxLength = " + (input.characterLimit > 0 ? input.characterLimit : (playerName ? 12 : 32767)) + @"
$box.SetBounds(12,12,375,24)
$box.Text = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + initial + @"'))
$ok = New-Object Windows.Forms.Button
$ok.Text = '填回'
$ok.DialogResult = 'OK'
$ok.SetBounds(190,48,95,26)
$cancel = New-Object Windows.Forms.Button
$cancel.Text = '取消'
$cancel.DialogResult = 'Cancel'
$cancel.SetBounds(292,48,95,26)
$form.Controls.AddRange(@($box,$ok,$cancel))
$form.AcceptButton = $ok
$form.CancelButton = $cancel
$minimum = " + (playerName ? 3 : 0) + @"
if ($minimum -gt 0) {
    $box.AccessibleDescription = '請輸入 3 至 12 個字元'
    $ok.Enabled = $box.Text.Trim().Length -ge $minimum
    $box.Add_TextChanged({ $ok.Enabled = $box.Text.Trim().Length -ge $minimum })
}
$form.ActiveControl = $box
try {
    if ($form.ShowDialog() -eq 'OK') {
        [Console]::WriteLine('OK:' + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($box.Text)))
    }
} finally { $form.Dispose() }
";
            string encoded = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));
            _isCurrent = isCurrent;
            _onApplied = onApplied;
            _input = input;
            _pending = System.Threading.Tasks.Task.Run(() =>
            {
                string executable = System.IO.Path.Combine(Environment.SystemDirectory,
                    @"WindowsPowerShell\v1.0\powershell.exe");
                using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = "-NoProfile -NonInteractive -STA -EncodedCommand " + encoded,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                });
                if (process == null) return null;
                string result = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return process.ExitCode == 0 && result.StartsWith("OK:", StringComparison.Ordinal)
                    ? System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(result.Substring(3)))
                    : null;
            });
        }

        internal static void Poll()
        {
            if (_pending == null || !_pending.IsCompleted) return;
            var completed = _pending;
            _pending = null;
            var input = _input;
            _input = null;
            var isCurrent = _isCurrent;
            var onApplied = _onApplied;
            _isCurrent = null;
            _onApplied = null;
            if (completed.IsFaulted)
            {
                MelonLogger.Warning($"[TextEditor] Search editor failed: {completed.Exception?.GetBaseException().Message}");
                return;
            }
            if (completed.Result == null || isCurrent?.Invoke() != true
                || input == null || !input.gameObject.activeInHierarchy)
                return;
            input.text = completed.Result;
            onApplied?.Invoke();
        }

    }
}

