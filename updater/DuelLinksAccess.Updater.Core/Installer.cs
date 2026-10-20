[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DuelLinksAccess.Tests")]

namespace DuelLinksAccess.Updater;

public sealed record RestoreResult(Version Version, string BackupPath);
public sealed record BackupInfo(string Path, Version Version, DateTime SavedAtUtc, string Sha256);
public sealed record BackupInventory(IReadOnlyList<BackupInfo> Backups, int Skipped);

public sealed class Installer(Func<bool> gameRunning)
{
    private readonly Action<string>? verifyReplacement;
    // Fault-injection seam to verify the existing rollback path without touching real installations.
    internal Installer(Func<bool> gameRunning, Action<string> verifyReplacement) : this(gameRunning)
        => this.verifyReplacement = verifyReplacement;

    public BackupInventory GetBackups(string gameFolder)
    {
        string mods = ModFile.ValidateGameFolder(gameFolder);
        var backups = new List<BackupInfo>();
        int skipped = 0;
        foreach (string path in Directory.EnumerateFiles(mods, "DuelLinksAccess.backup.*.bak"))
        {
            try { backups.Add(ReadBackup(mods, path)); }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or BadImageFormatException or ArgumentException)
            { skipped++; }
        }
        return new BackupInventory(backups.OrderByDescending(b => b.SavedAtUtc)
            .ThenByDescending(b => b.Path, StringComparer.Ordinal).ToArray(), skipped);
    }

    private static BackupInfo ReadBackup(string mods, string path)
    {
        string full = System.IO.Path.GetFullPath(path);
        if (!string.Equals(System.IO.Path.GetDirectoryName(full), mods, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("備份不在目前遊戲的 Mods 資料夾內。");
        var name = System.Text.RegularExpressions.Regex.Match(System.IO.Path.GetFileName(full),
            @"^DuelLinksAccess\.backup\.(\d{21})\.[a-fA-F0-9]{32}\.bak$");
        if (!name.Success || !DateTime.TryParseExact(name.Groups[1].Value, "yyyyMMddHHmmssfffffff",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
            out var saved)) throw new InvalidDataException("備份名稱或時間無效。");
        ModFile.RejectLinks(full);
        ModFile.RejectLinks(full + ".sha256");
        long size = new FileInfo(full).Length;
        if (size <= 0 || size > ReleaseClient.MaxDllSize) throw new InvalidDataException("備份大小無效。");
        if (new FileInfo(full + ".sha256").Length > 1024) throw new InvalidDataException("備份摘要檔過大。");
        string hash = File.ReadAllText(full + ".sha256").Trim();
        if (!System.Text.RegularExpressions.Regex.IsMatch(hash, "^[a-fA-F0-9]{64}$"))
            throw new InvalidDataException("備份缺少有效的 SHA-256。");
        byte[] bytes = File.ReadAllBytes(full);
        if (!ModFile.Hash(bytes).Equals(hash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("備份驗證失敗。");
        return new BackupInfo(full, ModFile.ReadVersion(bytes), saved, hash.ToUpperInvariant());
    }

    public string Install(string gameFolder, byte[] bytes, UpdateRelease release)
    {
        ReleaseClient.ValidateDownload(bytes, release);
        string mods = ModFile.ValidateGameFolder(gameFolder);
        string target = Path.Combine(mods, ReleaseClient.DllName);
        var current = ModFile.ReadVersion(File.ReadAllBytes(target));
        if (ReleaseClient.Normalize(current) >= ReleaseClient.Normalize(release.Version))
            throw new InvalidOperationException("已安裝相同或較新版本，不會降版或重複替換。");
        return Replace(mods, bytes, release.Version);
    }

    // Invoked only after the history UI confirms downgrade or reinstallation.
    public string InstallSelected(string gameFolder, byte[] bytes, UpdateRelease release)
    {
        ReleaseClient.ValidateDownload(bytes, release);
        string mods = ModFile.ValidateGameFolder(gameFolder);
        ModFile.ReadVersion(File.ReadAllBytes(Path.Combine(mods, ReleaseClient.DllName)));
        return Replace(mods, bytes);
    }

    public RestoreResult Restore(string gameFolder)
    {
        var inventory = GetBackups(gameFolder);
        if (inventory.Backups.Count == 0)
        {
            if (inventory.Skipped != 0) throw new InvalidDataException("備份驗證失敗，沒有有效備份可還原。");
            throw new InvalidOperationException("沒有可還原的更新程式備份。");
        }
        return Restore(gameFolder, inventory.Backups[0]);
    }

    public RestoreResult Restore(string gameFolder, BackupInfo selected)
    {
        string mods = ModFile.ValidateGameFolder(gameFolder);
        var verified = ReadBackup(mods, selected.Path);
        if (!verified.Sha256.Equals(selected.Sha256, StringComparison.OrdinalIgnoreCase)
            || !ReleaseClient.SameVersion(verified.Version, selected.Version)
            || verified.SavedAtUtc != selected.SavedAtUtc)
            throw new InvalidDataException("選定備份已變更，請重新開啟歷史版本管理。");
        byte[] bytes = File.ReadAllBytes(verified.Path);
        if (!ModFile.Hash(bytes).Equals(verified.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("選定備份已變更，未還原。");
        string previousBackup = Replace(mods, bytes);
        return new RestoreResult(verified.Version, previousBackup);
    }

    private string Replace(string mods, byte[] bytes, Version? updateVersion = null)
    {
        EnsureClosed();
        string lockPath = Path.Combine(mods, ".DuelLinksAccess.update.lock");
        ModFile.RejectLinks(lockPath);
        using var updateLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        string target = Path.Combine(mods, ReleaseClient.DllName);
        if (updateVersion != null && ReleaseClient.Normalize(ModFile.ReadVersion(File.ReadAllBytes(target)))
            >= ReleaseClient.Normalize(updateVersion)) throw new InvalidOperationException("版本已更新，請重新檢查。");
        string stage = Path.Combine(mods, ".DuelLinksAccess." + Guid.NewGuid().ToString("N") + ".tmp");
        string backup = Path.Combine(mods, "DuelLinksAccess.backup." + DateTime.UtcNow.ToString("yyyyMMddHHmmssfffffff")
            + "." + Guid.NewGuid().ToString("N") + ".bak");
        try
        {
            using (var file = new FileStream(stage, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                file.Write(bytes);
                file.Flush(true);
            }
            if (ModFile.Hash(File.ReadAllBytes(stage)) != ModFile.Hash(bytes)) throw new IOException("暫存檔驗證失敗。");
            EnsureClosed();
            ModFile.RejectLinks(target);
            string oldHash = ModFile.Hash(File.ReadAllBytes(target));
            // Both files are on the same volume. Failure leaves the original target intact.
            File.Replace(stage, target, backup);
            try
            {
                verifyReplacement?.Invoke(target);
                File.WriteAllText(backup + ".sha256", oldHash);
                if (ModFile.Hash(File.ReadAllBytes(target)) != ModFile.Hash(bytes))
                    throw new IOException("替換後驗證失敗。");
            }
            catch
            {
                File.Replace(backup, target, null);
                throw new IOException("更新未完成，已還原原檔。");
            }
            return backup;
        }
        finally { if (File.Exists(stage)) File.Delete(stage); }
    }

    private void EnsureClosed()
    {
        if (gameRunning()) throw new InvalidOperationException("請先完全關閉 Duel Links，再進行更新或還原。");
    }
}
