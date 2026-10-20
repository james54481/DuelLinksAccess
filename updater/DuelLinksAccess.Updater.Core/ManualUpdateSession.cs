namespace DuelLinksAccess.Updater;

// Reading an installation is local-only. A release lookup happens solely when
// the UI explicitly invokes CheckAsync; this class never installs anything.
public sealed class ManualUpdateSession(ReleaseClient client)
{
    public Version? InstalledVersion { get; private set; }
    public UpdateRelease? Available { get; private set; }

    public Version LoadInstalled(string gameFolder)
    {
        Clear();
        string mods = ModFile.ValidateGameFolder(gameFolder);
        return InstalledVersion = ModFile.ReadVersion(File.ReadAllBytes(Path.Combine(mods, ReleaseClient.DllName)));
    }

    public async Task<UpdateRelease?> CheckAsync(string gameFolder, CancellationToken ct)
    {
        var installed = LoadInstalled(gameFolder);
        var release = await client.CheckAsync(ct);
        Available = release != null && ReleaseClient.Normalize(release.Version) > ReleaseClient.Normalize(installed)
            ? release : null;
        return release;
    }

    public void Clear() { InstalledVersion = null; Available = null; }
}
