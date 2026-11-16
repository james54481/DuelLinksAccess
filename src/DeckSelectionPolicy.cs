using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace DuelLinksAccess
{
    public static class DeckSelectionPolicy
    {
        public static int? CharacterLevel(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var match = Regex.Match(text.Trim(), @"^(?:(?:等級|等级|Lv\.?|Level)\s*[:：]?\s*)?([0-9]+)$", RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out int level) && level > 0
                ? level : null;
        }

        public static bool UsesCharacterOrdinal(bool hasInfo, bool purchase, bool structure, int owner)
            => hasInfo && !purchase && !structure && owner > 0;

        public static float VisibleBottom(float viewMin, float viewMax, float footerMin, float footerMax)
        {
            // A known footer only clips the viewport when they overlap.
            if (footerMax <= viewMin || footerMin >= viewMax || footerMax >= viewMax) return viewMin;
            return footerMax;
        }

        // Normalized movement toward the nearest hidden edge. Callers apply
        // the appropriate sign for vertical versus horizontal ScrollRects.
        public static float RevealOffset(float min, float max, float viewMin, float viewMax, float travel)
        {
            if (travel <= 0f || float.IsNaN(travel) || float.IsInfinity(travel)) return 0f;
            if (min < viewMin) return (min - viewMin) / travel;
            if (max > viewMax) return (max - viewMax) / travel;
            return 0f;
        }

        // Card/deck names are game data; never translate or reconstruct truncated text.
        public static string FullName(string nativeName, string visibleName) =>
            !string.IsNullOrWhiteSpace(nativeName) ? nativeName.Trim() : visibleName?.Trim();
        public static IEnumerable<string> StatusKeys(bool used, bool favorite, bool locked, bool warning)
        {
            if (used) yield return "deck_select_in_use";
            if (favorite) yield return "deck_select_favorite";
            if (locked) yield return "deck_select_locked";
            // A warning icon does not prove why a deck is unusable.
            if (warning) yield return "deck_select_warning";
        }
    }
}
