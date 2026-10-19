namespace DuelLinksAccess.Updater;

public sealed record RestoreResult(Version Version, string BackupPath);

public sealed class Installer(Func<bool> gameRunning)
{
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

    public RestoreResult Restore(string gameFolder)
    {
        string mods = ModFile.ValidateGameFolder(gameFolder);
        // A backup is accepted only together with its original checksum.
        string? backup = Directory.EnumerateFiles(mods, "DuelLinksAccess.backup.*.bak")
            .Where(p => File.Exists(p + ".sha256")).OrderDescending(StringComparer.Ordinal).FirstOrDefault();
        if (backup == null) throw new InvalidOperationException("沒有可還原的更新程式備份。");
        ModFile.RejectLinks(backup);
        ModFile.RejectLinks(backup + ".sha256");
        byte[] bytes = File.ReadAllBytes(backup);
        if (!ModFile.Hash(bytes).Equals(File.ReadAllText(backup + ".sha256").Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("備份驗證失敗，未還原。");
        var version = ModFile.ReadVersion(bytes);
        string previousBackup = Replace(mods, bytes);
        return new RestoreResult(version, previousBackup);
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
