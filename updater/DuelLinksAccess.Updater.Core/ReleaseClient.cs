using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DuelLinksAccess.Updater;

public sealed record UpdateRelease(Version Version, long Size, string Sha256, Uri DownloadUrl, string Notes);

public sealed class ReleaseClient(HttpClient http)
{
    public const string Repository = "james54481/DuelLinksAccess";
    public const string DllName = "DuelLinksAccess.dll";
    public const long MaxDllSize = 32 * 1024 * 1024;

    public async Task<string?> GetNotesAsync(Version version, CancellationToken ct)
    {
        var normalized = Normalize(version);
        string[] versions = normalized.Revision == 0
            ? [normalized.ToString(3), normalized.ToString(4)] : [normalized.ToString(4)];
        foreach (string tag in versions)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.github.com/repos/{Repository}/releases/tags/v{tag}");
            request.Headers.UserAgent.ParseAdd("DuelLinksAccess-Updater/0.1.1");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");
            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.StatusCode == HttpStatusCode.NotFound) continue;
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await ReadLimitedAsync(response, 1024 * 1024, ct));
            var root = json.RootElement;
            string returnedTag = root.GetProperty("tag_name").GetString() ?? "";
            if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean()
                || !Version.TryParse(returnedTag.TrimStart('v'), out var actual)
                || !SameVersion(actual, version))
                throw new InvalidDataException("更新說明與還原版本不一致。");
            return root.TryGetProperty("body", out var body) ? body.GetString() : null;
        }
        return null;
    }

    public async Task<UpdateRelease?> CheckAsync(CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.github.com/repos/{Repository}/releases/latest");
        request.Headers.UserAgent.ParseAdd("DuelLinksAccess-Updater/0.1.1");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
            throw new InvalidOperationException("GitHub 暫時限制查詢次數，請稍後再試。");
        response.EnsureSuccessStatusCode();
        var bytes = await ReadLimitedAsync(response, 1024 * 1024, ct);
        return Parse(bytes);
    }

    public static UpdateRelease Parse(byte[] bytes)
    {
        using var json = JsonDocument.Parse(bytes);
        var root = json.RootElement;
        if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean())
            throw new InvalidDataException("只接受正式發布版。");
        string tag = root.GetProperty("tag_name").GetString() ?? "";
        if (!Regex.IsMatch(tag, @"^v?\d+\.\d+\.\d+(\.\d+)?$")
            || !Version.TryParse(tag.TrimStart('v'), out var version))
            throw new InvalidDataException("發布標籤必須使用 v1.4.3 形式的數字版本。");
        var assets = root.GetProperty("assets").EnumerateArray()
            .Where(a => a.GetProperty("name").GetString() == DllName).ToArray();
        if (assets.Length != 1) throw new InvalidDataException("此版本尚未提供唯一的 DuelLinksAccess.dll，請等待維護者補齊。");
        var asset = assets[0];
        long size = asset.GetProperty("size").GetInt64();
        string digest = asset.TryGetProperty("digest", out var d) ? d.GetString() ?? "" : "";
        if (size <= 0 || size > MaxDllSize || !Regex.IsMatch(digest, "^sha256:[a-fA-F0-9]{64}$"))
            throw new InvalidDataException("發布檔案缺少有效大小或 GitHub SHA-256，停止更新。");
        var url = new Uri(asset.GetProperty("browser_download_url").GetString() ?? "");
        string prefix = $"/{Repository}/releases/download/";
        if (url.Scheme != "https" || url.Host != "github.com" || !string.IsNullOrEmpty(url.UserInfo)
            || !url.AbsolutePath.StartsWith(prefix, StringComparison.Ordinal)
            || Uri.UnescapeDataString(url.Segments.Last()) != DllName)
            throw new InvalidDataException("下載位置不屬於指定倉庫。");
        return new(version, size, digest[7..].ToUpperInvariant(), url,
            root.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "");
    }

    public async Task<byte[]> DownloadAsync(UpdateRelease release, CancellationToken ct)
    {
        using var response = await http.GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        var bytes = await ReadLimitedAsync(response, release.Size, ct);
        ValidateDownload(bytes, release);
        return bytes;
    }

    public static void ValidateDownload(byte[] bytes, UpdateRelease release)
    {
        if (bytes.LongLength != release.Size
            || !Convert.ToHexString(SHA256.HashData(bytes)).Equals(release.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("下載檔案大小或 SHA-256 不符，原有模組未替換。");
        var version = ModFile.ReadVersion(bytes);
        if (!SameVersion(version, release.Version)) throw new InvalidDataException("DLL 版本與發布標籤不符。");
    }

    public static bool SameVersion(Version a, Version b) => Normalize(a) == Normalize(b);
    public static Version Normalize(Version v) => new(v.Major, v.Minor, Math.Max(0, v.Build), Math.Max(0, v.Revision));

    private static async Task<byte[]> ReadLimitedAsync(HttpResponseMessage response, long limit, CancellationToken ct)
    {
        if (response.Content.Headers.ContentLength > limit) throw new InvalidDataException("下載大小超出上限。");
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) != 0)
        {
            if (buffer.Length + read > limit) throw new InvalidDataException("下載大小超出上限。");
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }
}
