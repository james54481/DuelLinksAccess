using System;
using System.Text.RegularExpressions;
namespace DuelLinksAccess
{
    internal static class AlbumTextPolicy
    {
        internal static string Category(string nativeLabel)
            => (nativeLabel ?? "").Trim().TrimStart('▼', '▶', '►', '▽', '△').Trim();
        internal static string Title(string raw, Func<int, string> characterName)
        {
            string result = raw ?? "";
            var matches = Regex.Matches(result, @"<chara\s+id=['"" ]?(\d+)['""]?\s+get=['""]name['""]\s*/>");
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                var match = matches[i];
                string name = int.TryParse(match.Groups[1].Value, out int id)
                    ? characterName(id) ?? "UNKNOWN" : "UNKNOWN";
                result = result.Remove(match.Index, match.Length).Insert(match.Index, name);
            }
            return result;
        }
        internal static bool IsPlayPath(string path) => path != null && Regex.IsMatch(path,
            @"^Album\.data\.slist\.\d+\.scenario\.\d+\.list\.\d+$");
        internal static bool IsCategoryPath(string path) => path != null && Regex.IsMatch(path,
            @"^Album\.data\.slist\.\d+$");
        internal static bool Cost(string visible, out long needed, out long held)
        {
            needed = held = 0;
            var match = Regex.Match(visible ?? "", @"(?<![\d,])(\d[\d,]*)\s*/\s*(\d[\d,]*)(?![\d,])");
            return match.Success && long.TryParse(match.Groups[1].Value.Replace(",", ""), out needed)
                && long.TryParse(match.Groups[2].Value.Replace(",", ""), out held);
        }
    }
}
