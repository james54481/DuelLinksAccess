using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace DuelLinksAccess.Updater;

public static class ModFile
{
    public static Version ReadVersion(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, false);
        using var pe = new PEReader(stream);
        if (!pe.HasMetadata) throw new InvalidDataException("檔案不是有效的模組 DLL。");
        var metadata = pe.GetMetadataReader();
        if (!metadata.IsAssembly) throw new InvalidDataException("檔案缺少組件資訊。");
        var assembly = metadata.GetAssemblyDefinition();
        if (metadata.GetString(assembly.Name) != "DuelLinksAccess")
            throw new InvalidDataException("組件名稱不是 DuelLinksAccess。");
        return assembly.Version;
    }

    public static string ValidateGameFolder(string path)
    {
        string root = Path.GetFullPath(path.Trim().Trim('"'));
        if (!File.Exists(Path.Combine(root, "dlpc.exe")) || !Directory.Exists(Path.Combine(root, "MelonLoader")))
            throw new InvalidDataException("請選擇含 dlpc.exe 與 MelonLoader 資料夾的遊戲目錄。");
        string mods = Path.Combine(root, "Mods");
        if (!Directory.Exists(mods) || !File.Exists(Path.Combine(mods, ReleaseClient.DllName)))
            throw new InvalidDataException("尚未找到已安裝的 DuelLinksAccess.dll；本程式只更新既有模組。");
        RejectLinks(root);
        RejectLinks(mods);
        RejectLinks(Path.Combine(mods, ReleaseClient.DllName));
        return mods;
    }

    public static void RejectLinks(string path)
    {
        for (string? p = Path.GetFullPath(path); p != null; p = Path.GetDirectoryName(p))
            if ((File.Exists(p) || Directory.Exists(p)) && (File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("更新位置含符號連結或重新解析點，請選擇實際安裝路徑。");
    }

    public static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}
