using System;
using System.Text.RegularExpressions;

namespace DuelLinksAccess
{
    public static class GateLevelPolicy
    {
        // Read only visible game text, never infer a level from hierarchy IDs.
        public static int? ReadLevel(string label)
        {
            var match = Regex.Match(label ?? "",
                @"^\s*(?:(?:等級|等级|Level|Lv\.?|レベル)\s*[:：]?\s*)?([1-9]\d{0,2})\s*$",
                RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out int value) ? value : (int?)null;
        }

        public static bool CanSelect(bool active, bool disabled) => active && !disabled;
    }
}
