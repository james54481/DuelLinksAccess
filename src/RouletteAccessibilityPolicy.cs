using System;
using System.Text.RegularExpressions;
namespace DuelLinksAccess
{
    public static class RouletteAccessibilityPolicy
    {
        public static int? ResultIndex(bool duelReady, long result, int count)
            => duelReady && result >= 0 && result < count && result <= int.MaxValue ? (int)result : null;
        // Zero means no Bonus label. The event UI uses x2 and x3; keep
        // unrecognised values unknown rather than treating them as normal.
        public static int? ResultMultiplier(long bonus) => bonus switch
        { 0 => 1, 2 => 2, 3 => 3, _ => null };
        public static long? ResultSteps(long baseSteps, int? multiplier)
        {
            if (baseSteps < 0 || !multiplier.HasValue || multiplier.Value <= 0
                || baseSteps > long.MaxValue / multiplier.Value) return null;
            return baseSteps * multiplier.Value;
        }
        public static string Phase(bool duel, bool cost, bool spinning)
            => duel ? "duel" : cost ? (spinning ? "spinning" : "ready") : "unknown";
        public static bool IsDifficultyRow(string path)
            => Regex.IsMatch(path ?? "", @"^Event\.Roulette\.dialog\.level_select\.vals\.buttonlist\.\d+$");
        // Verified against the native Start wheel without activating it. Its
        // child texts are the two displayed numbers separated by a slash.
        public static bool IsStartControl(string name, string map, string lot, string text)
            => name == "btn" && map == "Event.Roulette.data.map" && lot == "Event.Roulette.data.lot"
                && TryCost(text, out _, out _);
        public static bool TryCost(string text, out int required, out int owned)
        {
            required = owned = -1;
            var match = Regex.Match(text ?? "", @"^\s*([0-9]+|[0-9]{1,3}(?:,[0-9]{3})+)[\s。]*[/／][\s。]*([0-9]+|[0-9]{1,3}(?:,[0-9]{3})+)\s*$");
            return match.Success && int.TryParse(match.Groups[1].Value.Replace(",", ""), out required)
                && int.TryParse(match.Groups[2].Value.Replace(",", ""), out owned);
        }
        public static int MapPosition(string path)
        {
            var match = Regex.Match(path ?? "", @"^Event\.Roulette\.data\.map\.list\.(\d+)$");
            return match.Success && int.TryParse(match.Groups[1].Value, out int index) && index < int.MaxValue
                ? index + 1 : -1;
        }
        public static string BalloonText(string link)
        {
            if (link == null || !link.StartsWith("balloon:msg?", StringComparison.Ordinal)) return null;
            var match = Regex.Match(link, @"(?:\?|&)text=([^&]+)");
            return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : null;
        }
    }
}
