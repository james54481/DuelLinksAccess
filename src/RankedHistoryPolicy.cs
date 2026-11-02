using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DuelLinksAccess
{
    internal static class RankedHistoryPolicy
    {
        internal sealed class Entry
        {
            public string Name { get; }
            public string Time { get; }
            public string Result { get; }
            public string Rank { get; }
            public Entry(string name, string time, string result, string rank)
            { Name = name; Time = time; Result = result; Rank = rank; }
        }
        public static string Normalize(string value) => Regex.Replace(value ?? "", @"\s+", " ").Trim();
        public static string ListPath(string row)
        {
            if (row == null || !Regex.IsMatch(row,
                @"^\$\.PvP\.bannerList\.[0-9]{1,9}\.data\.page\.log\.list\.[0-9]{1,9}$")) return null;
            return row.Substring(0, row.LastIndexOf('.'));
        }
        public static Entry Match(IEnumerable<Entry> candidates, IEnumerable<string> visibleText)
        {
            var texts = new HashSet<string>(visibleText.Select(Normalize), StringComparer.Ordinal);
            var named = candidates.Where(e => !string.IsNullOrWhiteSpace(e.Name) && texts.Contains(Normalize(e.Name))).ToArray();
            var timed = named.Where(e => !string.IsNullOrWhiteSpace(e.Time) && texts.Contains(Normalize(e.Time))).ToArray();
            // If the row shows a time, it must agree; never silently use a
            // different match by the same player. Ambiguity stays unknown.
            if (timed.Length == 1) return timed[0];
            return null;
        }
        public static string ResultKey(string result) => result switch
        {
            "Win" => "ranked_history_win",
            "Lose" => "ranked_history_loss",
            "Draw" => "ranked_history_draw",
            _ => "ranked_history_unknown_result"
        };
    }
}
