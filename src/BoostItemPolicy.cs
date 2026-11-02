using System;
using System.Text.RegularExpressions;

namespace DuelLinksAccess
{
    internal static class BoostItemPolicy
    {
        public static bool IsRow(string path, int id) => id >= 301 && id <= 304 && path != null
            && Regex.IsMatch(path, @"^Response\.data\.item\.[0-9]{1,3}$");

        // Preserve the game's ratio display without claiming which side means
        // ownership, cost, or maximum. No unboxing via Object.ToString().
        public static string Quantity(string text)
        {
            if (text == null) return null;
            var match = Regex.Match(text.Trim(), @"^([0-9]+)\s*[/／]\s*([0-9]+)$");
            return match.Success ? match.Groups[1].Value + "/" + match.Groups[2].Value : null;
        }
    }
}
