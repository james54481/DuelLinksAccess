using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace DuelLinksAccess.Updater;

internal static class GameLocator
{
    public static IEnumerable<string> Find()
    {
        var steamRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string key in new[] { @"HKEY_CURRENT_USER\Software\Valve\Steam", @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam" })
        {
            foreach (string value in new[] { "SteamPath", "InstallPath" })
                if (Registry.GetValue(key, value, null) is string path && Directory.Exists(path)) steamRoots.Add(path);
        }
        steamRoots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"));
        var libraries = new HashSet<string>(steamRoots, StringComparer.OrdinalIgnoreCase);
        foreach (string steam in steamRoots)
        {
            string vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdf)) continue;
            foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                libraries.Add(m.Groups[1].Value.Replace(@"\\", @"\"));
        }
        foreach (string library in libraries)
        {
            string manifest = Path.Combine(library, "steamapps", "appmanifest_601510.acf");
            if (!File.Exists(manifest)) continue;
            var match = Regex.Match(File.ReadAllText(manifest), "\"installdir\"\\s+\"([^\"]+)\"");
            if (!match.Success) continue;
            string folder = Path.Combine(library, "steamapps", "common", match.Groups[1].Value);
            if (File.Exists(Path.Combine(folder, "dlpc.exe"))) yield return folder;
        }
    }
}
