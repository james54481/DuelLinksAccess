using System.Net;
using System.Text;
using System.Text.Json;
using DuelLinksAccess.Updater;
using Xunit;

namespace DuelLinksAccess.Tests;

public sealed class UpdaterTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "DLA-Updater-Tests-" + Guid.NewGuid().ToString("N"));
    private static byte[] Fixture(string kind)
    {
        using var stream = typeof(UpdaterTests).Assembly.GetManifestResourceStream("UpdaterFixture." + kind);
        using var output = new MemoryStream(); stream.CopyTo(output); return output.ToArray();
    }
    private static UpdateRelease Release(byte[] bytes) => new(new Version(1, 4, 3), bytes.Length, ModFile.Hash(bytes),
        new Uri("https://github.com/james54481/DuelLinksAccess/releases/download/v1.4.3/DuelLinksAccess.dll"), "測試更新");
    private static byte[] Json(string tag = "v1.4.3", string digest = null, string url = null, bool prerelease = false) =>
        JsonSerializer.SerializeToUtf8Bytes(new
        {
            draft = false, prerelease, tag_name = tag, body = "新版說明",
            assets = new[] { new { name = "DuelLinksAccess.dll", size = 4000,
                digest = digest ?? "sha256:" + new string('A', 64),
                browser_download_url = url ?? "https://github.com/james54481/DuelLinksAccess/releases/download/v1.4.3/DuelLinksAccess.dll" } }
        });
    private string Target => Path.Combine(root, "Mods", "DuelLinksAccess.dll");
    private void Game()
    {
        Directory.CreateDirectory(Path.Combine(root, "MelonLoader"));
        Directory.CreateDirectory(Path.Combine(root, "Mods"));
        File.WriteAllText(Path.Combine(root, "dlpc.exe"), "test marker only; never run");
        File.WriteAllBytes(Target, Fixture("Old"));
    }

    [Fact] public void LatestReleaseIncludesVerifiedAssetAndNotes()
    {
        var release = ReleaseClient.Parse(Json());
        Assert.Equal(new Version(1, 4, 3), release.Version);
        Assert.Equal("新版說明", release.Notes);
    }
    [Theory]
    [InlineData("v1.4.3-beta")]
    [InlineData("main")]
    public void NonNumericVersionsAreRejected(string tag) => Assert.Throws<InvalidDataException>(() => ReleaseClient.Parse(Json(tag)));
    [Fact] public void PrereleasesAreRejected() => Assert.Throws<InvalidDataException>(() => ReleaseClient.Parse(Json(prerelease: true)));
    [Theory]
    [InlineData("")]
    [InlineData("sha256:123")]
    [InlineData("md5:1234567890")]
    public void MissingOrInvalidChecksumIsRejected(string digest) => Assert.Throws<InvalidDataException>(() => ReleaseClient.Parse(Json(digest: digest)));
    [Theory]
    [InlineData("https://example.com/DuelLinksAccess.dll")]
    [InlineData("http://github.com/james54481/DuelLinksAccess/releases/download/v1.4.3/DuelLinksAccess.dll")]
    [InlineData("https://github.com/other/DuelLinksAccess/releases/download/v1.4.3/DuelLinksAccess.dll")]
    public void OnlyPinnedRepositoryDownloadsAreAccepted(string url) => Assert.Throws<InvalidDataException>(() => ReleaseClient.Parse(Json(url: url)));
    [Fact] public void DllMetadataIsReadWithoutLoadingTheAssembly() => Assert.Equal(new Version(1, 4, 3, 0), ModFile.ReadVersion(Fixture("New")));
    [Fact] public void TruncatedDownloadIsRejected()
    {
        byte[] bytes = Fixture("New"); Assert.Throws<InvalidDataException>(() => ReleaseClient.ValidateDownload(bytes[..100], Release(bytes)));
    }
    [Fact] public void SameSizeCorruptionIsRejected()
    {
        byte[] bytes = Fixture("New"); var release = Release(bytes); bytes[100] ^= 1;
        Assert.Throws<InvalidDataException>(() => ReleaseClient.ValidateDownload(bytes, release));
    }
    [Fact] public void AssemblyVersionMustMatchReleaseTag()
    {
        byte[] bytes = Fixture("Old"); Assert.Throws<InvalidDataException>(() => ReleaseClient.ValidateDownload(bytes, Release(bytes)));
    }
    [Fact] public void DifferentAssemblyNameIsRejected() => Assert.Throws<InvalidDataException>(() =>
        ModFile.ReadVersion(File.ReadAllBytes(typeof(UpdaterTests).Assembly.Location)));
    [Fact] public void InstallCreatesVerifiedNonDllBackupAndRestoreWorks()
    {
        Game(); byte[] bytes = Fixture("New"); var installer = new Installer(() => false);
        string backup = installer.Install(root, bytes, Release(bytes));
        Assert.Equal(bytes, File.ReadAllBytes(Target));
        Assert.Equal(Fixture("Old"), File.ReadAllBytes(backup));
        Assert.EndsWith(".bak", backup);
        Assert.Single(Directory.GetFiles(Path.Combine(root, "Mods"), "*.dll"));
        installer.Restore(root);
        Assert.Equal(Fixture("Old"), File.ReadAllBytes(Target));
        Assert.Empty(Directory.GetFiles(Path.Combine(root, "Mods"), "*.tmp"));
    }
    [Fact] public void RunningGameLeavesOriginalUntouched()
    {
        Game(); byte[] bytes = Fixture("New");
        Assert.Throws<InvalidOperationException>(() => new Installer(() => true).Install(root, bytes, Release(bytes)));
        Assert.Equal(Fixture("Old"), File.ReadAllBytes(Target));
    }
    [Fact] public void GameStartingDuringStagingPreventsReplacement()
    {
        Game(); int checks = 0; byte[] bytes = Fixture("New");
        Assert.Throws<InvalidOperationException>(() => new Installer(() => ++checks > 1).Install(root, bytes, Release(bytes)));
        Assert.Equal(Fixture("Old"), File.ReadAllBytes(Target));
        Assert.Empty(Directory.GetFiles(Path.Combine(root, "Mods"), "*.tmp"));
    }
    [Fact] public void SameVersionNeverReplacesExistingMod()
    {
        Game(); byte[] bytes = Fixture("New"); File.WriteAllBytes(Target, bytes);
        Assert.Throws<InvalidOperationException>(() => new Installer(() => false).Install(root, bytes, Release(bytes)));
        Assert.Empty(Directory.GetFiles(Path.Combine(root, "Mods"), "*.bak"));
    }
    [Fact] public void BadHashNeverReplacesExistingMod()
    {
        Game(); byte[] bytes = Fixture("New");
        Assert.Throws<InvalidDataException>(() => new Installer(() => false).Install(root, bytes, Release(bytes) with { Sha256 = new string('0', 64) }));
        Assert.Equal(Fixture("Old"), File.ReadAllBytes(Target));
    }
    [Fact] public void TamperedBackupCannotBeRestored()
    {
        Game(); byte[] bytes = Fixture("New"); var installer = new Installer(() => false);
        string backup = installer.Install(root, bytes, Release(bytes)); File.WriteAllBytes(backup, bytes);
        Assert.Throws<InvalidDataException>(() => installer.Restore(root)); Assert.Equal(bytes, File.ReadAllBytes(Target));
    }
    [Fact] public void ConcurrentUpdaterLockPreventsReplacement()
    {
        Game(); byte[] bytes = Fixture("New");
        using var held = new FileStream(Path.Combine(root, "Mods", ".DuelLinksAccess.update.lock"), FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        Assert.Throws<IOException>(() => new Installer(() => false).Install(root, bytes, Release(bytes)));
        Assert.Equal(Fixture("Old"), File.ReadAllBytes(Target));
    }
    [Fact] public void MissingGameOrMissingModIsRejected()
    {
        Directory.CreateDirectory(root); Assert.Throws<InvalidDataException>(() => ModFile.ValidateGameFolder(root));
        Game(); File.Delete(Target); Assert.Throws<InvalidDataException>(() => ModFile.ValidateGameFolder(root));
    }
    [Fact] public void LockedTargetLeavesOriginalAndCleansStagingFile()
    {
        Game(); byte[] bytes = Fixture("New");
        using var held = new FileStream(Target, FileMode.Open, FileAccess.Read, FileShare.Read);
        Assert.Throws<IOException>(() => new Installer(() => false).Install(root, bytes, Release(bytes)));
        Assert.Equal(Fixture("Old"), File.ReadAllBytes(Target));
        Assert.Empty(Directory.GetFiles(Path.Combine(root, "Mods"), "*.tmp"));
    }
    [Fact] public async Task NoReleaseReturnsInformativeEmptyResult()
    {
        using var http = new HttpClient(new Handler(_ => new(HttpStatusCode.NotFound)));
        Assert.Null(await new ReleaseClient(http).CheckAsync(default));
    }
    [Fact] public async Task RateLimitHasReadableError()
    {
        using var http = new HttpClient(new Handler(_ => new(HttpStatusCode.Forbidden)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ReleaseClient(http).CheckAsync(default));
    }
    [Fact] public async Task MockGithubDownloadVerifiesBinaryEndToEnd()
    {
        byte[] bytes = Fixture("New");
        using var http = new HttpClient(new Handler(_ => new(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) }));
        Assert.Equal(bytes, await new ReleaseClient(http).DownloadAsync(Release(bytes), default));
    }
    [Fact] public async Task OversizedResponseIsRejectedBeforeInstall()
    {
        byte[] bytes = Fixture("New");
        using var http = new HttpClient(new Handler(_ => new(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[bytes.Length + 1]) }));
        await Assert.ThrowsAsync<InvalidDataException>(() => new ReleaseClient(http).DownloadAsync(Release(bytes), default));
    }
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => Task.FromResult(callback(request));
    }
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
