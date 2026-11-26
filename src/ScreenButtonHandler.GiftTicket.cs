using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Il2CppYgomSystem.Utility;
namespace DuelLinksAccess
{
    public partial class ScreenButtonHandler
    {
        private static string TicketRowPath(GameObject go)
        {
            foreach (string key in new[] { "%datapath%", "%sublooppath%" }) {
                string path = RouletteParam(go, key);
                var match = Regex.Match(path ?? "", @"^Gift\.ticket\.data\.list\.\d+(?=\.|$)");
                if (match.Success) return match.Value;
            }
            return null;
        }
        private static string GiftTicketLabel(string path)
        {
            string name = LabelExtractor.StripRichText(ClientWork.getStringByJsonPath(path + ".title", "UNKNOWN"));
            string description = LabelExtractor.StripRichText(ClientWork.getStringByJsonPath(path + ".text", ""));
            // Boxed IL2CPP numbers must use the typed reader, never Object.ToString().
            long count = ClientWork.getLongByJsonPath(path + ".count", -1);
            return name + "。" + Loc.Get("gift_quantity", count >= 0 ? count.ToString() : "UNKNOWN")
                + (string.IsNullOrWhiteSpace(description) ? "" : "。" + description.Trim());
        }
        private void AddGiftTicketAccessibility(GameObject root)
        {
            if (root == null || root.name != "GiftTicket") return;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var rebuilt = new List<ScreenItem>();
            var navigation = new List<ScreenItem>();
            foreach (var item in _items) {
                if (item.Go == null || item.Go.GetComponentInParent<Scrollbar>() != null) continue;
                if (item.Go.name == "BackButton" || item.Go.name == "HomeButton") {
                    item.Label = Loc.Get(item.Go.name == "BackButton" ? "screen_back" : "deck_view_home");
                    navigation.Add(item);
                    continue;
                }
                string path = TicketRowPath(item.Go);
                if (path == null) { rebuilt.Add(item); continue; }
                if (!seen.Add(path)) continue;
                // Keep the original native activation target; deduplicate by data
                // row, not by title, so same-name tickets remain independent.
                item.Label = GiftTicketLabel(path);
                item.LiveLabel = () => GiftTicketLabel(path);
                rebuilt.Add(item);
                DebugLogger.Log(LogCategory.Handler, "GiftTicket", $"row={path} label={item.Label}");
            }
            rebuilt.AddRange(navigation);
            _items.Clear();
            _items.AddRange(rebuilt);
        }
    }
}