using System.Net;
using System.Text;
using System.Text.Json;
using DuelLinksAccess.Updater;
using Xunit;

namespace DuelLinksAccess.Tests;

public sealed class UpdaterTests : IDisposable
{
    [Theory]
    [InlineData("第一行\n第二行")]
    [InlineData("第一行\r第二行")]
    [InlineData("第一行\r\n第二行")]
    public void NotesPreserveLineBoundariesForNativeArrowKeyReading(string text)
        => Assert.Equal("第一行\r\n第二行", ReleaseNotesText.ForDisplay(text));
    [Fact] public void MissingNotesHaveAnEmptyDisplayValue() => Assert.Equal("", ReleaseNotesText.ForDisplay(null));
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
                browser_download_url = url ?? "https://github.com/james54481/DuelLinksAccess/releases/download/" + tag + "/DuelLinksAccess.dll" } }
        });
    private string Target => Path.Combine(root, "Mods", "DuelLinksAccess.dll");
    private void Game()
    {
        Directory.CreateDirectory(Path.Combine(root, "MelonLoader"));
        Directory.CreateDirectory(Path.Combine(root, "Mods"));
        File.WriteAllText(Path.Combine(root, "dlpc.exe"), "test marker only; never run");
        File.WriteAllBytes(Target, Fixture("Old"));
    }

    [Fact] public void LegacyAutomaticPreferenceIsIgnoredAndRemovedWhenSaved()
    {
        var preferences = UpdaterPreferences.Parse("{\"GamePath\":\"C:/test\",\"Automatic\":true}");
        Assert.Equal("C:/test", preferences.GamePath);
        Assert.DoesNotContain("Automatic", preferences.ToJson());
    }
    [Fact] public void LoadingInstalledVersionNeverContactsGithubOrChangesFiles()
    {
        Game();
        using var http = new HttpClient(new Handler(_ => throw new Exception("Startup must not connect")));
        var session = new ManualUpdateSession(new ReleaseClient(http));
        Assert.Equal(new Version(1,4,2,0), session.LoadInstalled(root));
        Assert.Null(session.Available);
        Assert.Equal(Fixture("Old"), File.ReadAllBytes(Target));
        Assert.Single(Directory.GetFiles(Path.Combine(root,"Mods")));
    }
    [Fact] public async Task ManualCheckFetchesOnlyMetadataAndNeverDownloadsOrInstalls()
    {
        Game(); int requests = 0;
        using var http = new HttpClient(new Handler(request =>
        {
            requests++;
            Assert.EndsWith("/releases/latest", request.RequestUri.AbsolutePath);
            return new(HttpStatusCode.OK) { Content = new ByteArrayContent(Json()) };
        }));
        var session = new ManualUpdateSession(new ReleaseClient(http));
        session.LoadInstalled(root);
        Assert.Equal(0, requests);
        var result = await session.CheckAsync(root, default);
        Assert.Equal("新版說明", result.Notes);
        Assert.NotNull(session.Available);
        Assert.Equal(1, requests);
        Assert.Equal(Fixture("Old"), File.ReadAllBytes(Target));
        Assert.Single(Directory.GetFiles(Path.Combine(root,"Mods")));
        session.Clear(); Assert.Null(session.Available);
    }
    [Fact] public async Task AlreadyLatestVersionCannotEnableInstallation()
    {
        Game();
        using var http = new HttpClient(new Handler(_ => new(HttpStatusCode.OK) { Content = new ByteArrayContent(Json(tag:"v1.4.2")) }));
        var session = new ManualUpdateSession(new ReleaseClient(http));
        await session.CheckAsync(root, default);
        Assert.Null(session.Available);
    }
    [Fact] public void HistoryListsVersionsTimesAndValidatesEveryBackupWithoutWriting()
    {
        Game(); byte[] bytes = Fixture("New"); var installer = new Installer(() => false);
        installer.Install(root, bytes, Release(bytes)); installer.Restore(root);
        var before = Directory.GetFiles(Path.Combine(root,"Mods"));
        var inventory = installer.GetBackups(root);
        Assert.Equal(2, inventory.Backups.Count); Assert.Equal(0, inventory.Skipped);
        Assert.Contains(inventory.Backups, b => b.Version == new Version(1,4,2,0));
        Assert.Contains(inventory.Backups, b => b.Version == new Version(1,4,3,0));
        Assert.True(inventory.Backups[0].SavedAtUtc >= inventory.Backups[1].SavedAtUtc);
        Assert.All(inventory.Backups, b => Assert.Equal(DateTimeKind.Utc, b.SavedAtUtc.Kind));
        Assert.Equal(before.Order(), Directory.GetFiles(Path.Combine(root,"Mods")).Order());
    }
    [Fact] public void SelectedOlderBackupIsRestoredInsteadOfNewestBackup()
    {
        Game(); byte[] bytes = Fixture("New"); var installer = new Installer(() => false);
        installer.Install(root, bytes, Release(bytes)); installer.Restore(root);
        var selected = installer.GetBackups(root).Backups.Single(b => b.Version == new Version(1,4,2,0));
        File.WriteAllBytes(Target, bytes);
        var result = installer.Restore(root, selected);
        Assert.Equal(new Version(1,4,2,0), result.Version);
        Assert.Equal(Fixture("Old"), File.ReadAllBytes(Target));
        Assert.Equal(bytes, File.ReadAllBytes(result.BackupPath));
    }
    [Fact] public void InvalidHistoryEntriesAreSkippedWithoutBeingDeleted()
    {
        Game(); byte[] bytes = Fixture("New"); var installer = new Installer(() => false);
        string backup = installer.Install(root,bytes,Release(bytes));
        File.WriteAllText(backup + ".sha256",new string('0',64));
        var inventory = installer.GetBackups(root);
        Assert.Empty(inventory.Backups); Assert.Equal(1,inventory.Skipped);
        Assert.True(File.Exists(backup)); Assert.Equal(bytes,File.ReadAllBytes(Target));
    }
    [Fact] public void SelectedBackupChangedAfterListingCannotBeRestored()
    {
        Game(); byte[] bytes = Fixture("New"); var installer = new Installer(() => false);
        installer.Install(root,bytes,Release(bytes));
        var selected = installer.GetBackups(root).Backups.Single();
        File.WriteAllBytes(selected.Path, bytes);
        File.WriteAllText(selected.Path + ".sha256", ModFile.Hash(bytes));
        Assert.Throws<InvalidDataException>(() => installer.Restore(root,selected));
        Assert.Equal(bytes,File.ReadAllBytes(Target));
    }
    [Fact] public void BackupFromAnotherFolderCannotBeSelectedForRestore()
    {
        Game(); byte[] bytes=Fixture("New"); var installer=new Installer(()=>false);
        installer.Install(root,bytes,Release(bytes));
        var selected=installer.GetBackups(root).Backups.Single() with { Path=Path.Combine(root,"outside.bak") };
        Assert.Throws<InvalidDataException>(()=>installer.Restore(root,selected));
        Assert.Equal(bytes,File.ReadAllBytes(Target));
    }
    [Fact] public void SelectedRestoreStillBlocksWhenGameIsRunning()
    {
        Game(); byte[] bytes=Fixture("New"); var installer=new Installer(()=>false);
        installer.Install(root,bytes,Release(bytes));
        var selected=installer.GetBackups(root).Backups.Single();
        Assert.Throws<InvalidOperationException>(()=>new Installer(()=>true).Restore(root,selected));
        Assert.Equal(bytes,File.ReadAllBytes(Target));
    }

    [Fact] public void LatestReleaseIncludesVerifiedAssetAndNotes()
    {
        var release = ReleaseClient.Parse(Json());
        Assert.Equal(new Version(1, 4, 3), release.Version);
        Assert.Equal("新版說明", release.Notes);
    }
    [Fact] public void CommunityFourPartReleaseIsNewerThanUpstreamBase()
    {
        var release = ReleaseClient.Parse(Json(tag: "v1.4.2.1"));
        Assert.Equal(new Version(1, 4, 2, 1), release.Version);
        Assert.True(ReleaseClient.Normalize(release.Version) > ReleaseClient.Normalize(new Version(1, 4, 2)));
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
        var restored = installer.Restore(root);
        Assert.Equal(new Version(1, 4, 2, 0), restored.Version);
        Assert.Equal(bytes, File.ReadAllBytes(restored.BackupPath));
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
    [Fact] public async Task RestoredVersionFetchesItsOwnNotesInsteadOfLatest()
    {
        using var http = new HttpClient(new Handler(request =>
        {
            Assert.EndsWith("/releases/tags/v1.4.2.1", request.RequestUri.AbsolutePath);
            return new(HttpStatusCode.OK) { Content = new ByteArrayContent(Json(tag: "v1.4.2.1")) };
        }));
        Assert.Equal("新版說明", await new ReleaseClient(http).GetNotesAsync(new Version(1,4,2,1), default));
    }
    [Fact] public async Task OriginalThreePartVersionUsesCanonicalTag()
    {
        using var http = new HttpClient(new Handler(request =>
        {
            Assert.EndsWith("/tags/v1.4.2", request.RequestUri.AbsolutePath);
            return new(HttpStatusCode.OK) { Content = new ByteArrayContent(Json(tag: "v1.4.2")) };
        }));
        Assert.Equal("新版說明", await new ReleaseClient(http).GetNotesAsync(new Version(1,4,2,0), default));
    }
    [Fact] public async Task MissingOlderReleaseReturnsNoNotes()
    {
        using var http = new HttpClient(new Handler(_ => new(HttpStatusCode.NotFound)));
        Assert.Null(await new ReleaseClient(http).GetNotesAsync(new Version(1,4,2,1), default));
    }
    [Fact] public async Task ZeroRevisionFallsBackToFourPartTag()
    {
        int requests = 0;
        using var http = new HttpClient(new Handler(request =>
        {
            requests++;
            if (requests == 1) return new(HttpStatusCode.NotFound);
            Assert.EndsWith("/tags/v1.4.2.0", request.RequestUri.AbsolutePath);
            return new(HttpStatusCode.OK) { Content = new ByteArrayContent(Json(tag: "v1.4.2.0")) };
        }));
        Assert.Equal("新版說明", await new ReleaseClient(http).GetNotesAsync(new Version(1,4,2,0), default));
        Assert.Equal(2, requests);
    }
    [Fact] public async Task NewerReleaseNotesCannotBeShownForOlderVersion()
    {
        using var http = new HttpClient(new Handler(_ => new(HttpStatusCode.OK) { Content = new ByteArrayContent(Json(tag: "v1.4.2.2")) }));
        await Assert.ThrowsAsync<InvalidDataException>(() => new ReleaseClient(http).GetNotesAsync(new Version(1,4,2,1), default));
    }
    [Fact] public async Task NetworkFailureWhileFetchingNotesDoesNotUndoSuccessfulRestore()
    {
        Game(); byte[] bytes = Fixture("New"); var installer = new Installer(() => false);
        installer.Install(root, bytes, Release(bytes));
        var restored = installer.Restore(root);
        using var http = new HttpClient(new Handler(_ => throw new HttpRequestException("offline")));
        await Assert.ThrowsAsync<HttpRequestException>(() => new ReleaseClient(http).GetNotesAsync(restored.Version, default));
        Assert.Equal(Fixture("Old"), File.ReadAllBytes(Target));
        Assert.Equal(new Version(1,4,2,0), restored.Version);
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
    [Theory]
    [InlineData("Old", "New", "1.4.3")]
    [InlineData("New", "Old", "1.4.2.0")]
    [InlineData("New", "New", "1.4.3")]
    public async Task ConfirmedHistoryInstallUpgradesDowngradesAndReinstalls(string initial, string target, string version)
    {
        Game(); byte[] before = Fixture(initial), bytes = Fixture(target);
        File.WriteAllBytes(Target, before);
        var release = Release(bytes) with { Version = new Version(version) };
        using var http = new HttpClient(new Handler(_ => new(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) }));
        var downloaded = await new ReleaseClient(http).DownloadAsync(release, default);
        var installer = new Installer(() => false);
        string backup = installer.InstallSelected(root, downloaded, release);
        Assert.Equal(bytes, File.ReadAllBytes(Target));
        Assert.Equal(before, File.ReadAllBytes(backup));
        Assert.Equal(ModFile.Hash(before), File.ReadAllText(backup + ".sha256"));
        installer.Restore(root);
        Assert.Equal(before, File.ReadAllBytes(Target));
        Assert.Empty(Directory.GetFiles(Path.Combine(root, "Mods"), "*.tmp"));
    }
    [Theory]
    [InlineData("Old", "New", "1.4.3")]
    [InlineData("New", "Old", "1.4.2.0")]
    [InlineData("New", "New", "1.4.3")]
    public void FailureAfterHistoryReplacementRollsBackOriginal(string initial, string target, string version)
    {
        Game(); byte[] before = Fixture(initial), bytes = Fixture(target);
        File.WriteAllBytes(Target, before);
        bool reachedReplacement = false;
        var installer = new Installer(() => false, path =>
        {
            reachedReplacement = true;
            Assert.Equal(bytes, File.ReadAllBytes(path));
            throw new IOException("Injected post-replacement verification failure");
        });
        var ex = Assert.Throws<IOException>(() => installer.InstallSelected(root, bytes, Release(bytes) with { Version = new Version(version) }));
        Assert.True(reachedReplacement);
        Assert.Contains("已還原原檔", ex.Message);
        Assert.Equal(before, File.ReadAllBytes(Target));
        Assert.Empty(Directory.GetFiles(Path.Combine(root, "Mods"), "*.tmp"));
    }
    [Fact] public void HistoryInstallationRetainsHashAndRunningGameGuards()
    {
        Game(); byte[] bytes = Fixture("New");
        Assert.Throws<InvalidDataException>(() => new Installer(() => false).InstallSelected(root, bytes, Release(bytes) with { Sha256 = new string('0', 64) }));
        Assert.Throws<InvalidOperationException>(() => new Installer(() => true).InstallSelected(root, bytes, Release(bytes)));
        Assert.Equal(Fixture("Old"), File.ReadAllBytes(Target));
        Assert.Empty(Directory.GetFiles(Path.Combine(root, "Mods"), "*.bak"));
    }
    private static object HistoryRow(string tag, bool draft = false, bool prerelease = false, bool valid = true, string date = "2026-10-20T00:00:00Z")
        => new { draft, prerelease, tag_name = tag, published_at = date, body = "說明 " + tag,
            assets = new[] { new { name = "DuelLinksAccess.dll", size = 4000,
                digest = valid ? "sha256:" + new string('A', 64) : "",
                browser_download_url = "https://github.com/james54481/DuelLinksAccess/releases/download/" + tag + "/DuelLinksAccess.dll" } } };
    [Fact] public async Task HistoryExcludesDraftPrereleaseAndUpdaterTagsAndSortsByVersionNotDate()
    {
        using var http = new HttpClient(new Handler(request =>
        {
            Assert.Equal("https://api.github.com/repos/james54481/DuelLinksAccess/releases?per_page=100&page=1", request.RequestUri.ToString());
            return new(HttpStatusCode.OK) { Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(new[] {
                HistoryRow("v1.4.2.2"), HistoryRow("v1.4.10", date:"2025-01-01T00:00:00Z"),
                HistoryRow("v1.4.3", draft:true), HistoryRow("v1.4.4", prerelease:true), HistoryRow("updater-v0.1.1"), HistoryRow("v1.4.2.1", valid:false) })) };
        }));
        var rows = await new ReleaseClient(http).GetHistoryAsync(default);
        Assert.Equal(new[] { "1.4.10", "1.4.2.2", "1.4.2.1" }, rows.Select(r => r.Version.ToString()));
        Assert.NotNull(rows[0].Asset); Assert.Equal("說明 v1.4.10", rows[0].Notes);
        Assert.Equal(2025, rows[0].PublishedAt.Year);
        Assert.Null(rows[2].Asset); Assert.NotEmpty(rows[2].UnavailableReason);
    }
    [Fact] public async Task HistoryReadsAllPagesWithoutDownloadingAssetsOrWritingFiles()
    {
        Game(); int requests = 0;
        using var http = new HttpClient(new Handler(request =>
        {
            requests++;
            Assert.Contains("/releases?per_page=100&page=" + requests, request.RequestUri.ToString());
            object[] rows = requests == 1 ? Enumerable.Range(0, 100).Select(i => HistoryRow("v1.0." + i)).ToArray() : new[] { HistoryRow("v2.0.0") };
            return new(HttpStatusCode.OK) { Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(rows)) };
        }));
        var rows = await new ReleaseClient(http).GetHistoryAsync(default);
        Assert.Equal(2, requests); Assert.Equal(101, rows.Count); Assert.Equal(new Version(2,0,0), rows[0].Version);
        Assert.Equal(Fixture("Old"), File.ReadAllBytes(Target)); Assert.Single(Directory.GetFiles(Path.Combine(root,"Mods")));
    }
    [Fact] public async Task HistoryConnectionFailureLeavesInstalledDllUntouched()
    {
        Game();
        using var http = new HttpClient(new Handler(_ => throw new HttpRequestException("offline")));
        await Assert.ThrowsAsync<HttpRequestException>(() => new ReleaseClient(http).GetHistoryAsync(default));
        Assert.Equal(Fixture("Old"), File.ReadAllBytes(Target)); Assert.Single(Directory.GetFiles(Path.Combine(root,"Mods")));
    }
    [Fact] public async Task HistoryRateLimitReportsReadableFailure()
    {
        using var http = new HttpClient(new Handler(_ => new(HttpStatusCode.Forbidden)));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => new ReleaseClient(http).GetHistoryAsync(default));
        Assert.Contains("限制查詢次數", error.Message);
    }
    [Fact] public async Task DownloadRejectsForeignRepositoryBeforeSendingRequest()
    {
        using var http = new HttpClient(new Handler(_ => throw new Exception("Must not request foreign asset")));
        await Assert.ThrowsAsync<InvalidDataException>(() => new ReleaseClient(http).DownloadAsync(Release(Fixture("New")) with { DownloadUrl = new Uri("https://example.com/DuelLinksAccess.dll") }, default));
    }
    [Fact] public void AssetFromDifferentTagIsRejected()
        => Assert.Throws<InvalidDataException>(() => ReleaseClient.Parse(Json(tag: "v1.4.2", url: "https://github.com/james54481/DuelLinksAccess/releases/download/v1.4.3/DuelLinksAccess.dll")));
    [Fact] public async Task FailedHistoryDownloadNeverReplacesInstalledDll()
    {
        Game(); byte[] bytes = Fixture("New");
        using var http = new HttpClient(new Handler(_ => new(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes[..100]) }));
        await Assert.ThrowsAsync<InvalidDataException>(() => new ReleaseClient(http).DownloadAsync(Release(bytes), default));
        Assert.Equal(Fixture("Old"), File.ReadAllBytes(Target));
        Assert.Single(Directory.GetFiles(Path.Combine(root, "Mods")));
    }
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => Task.FromResult(callback(request));
    }
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
