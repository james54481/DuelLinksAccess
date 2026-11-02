using System;
using UnityEngine;
using Il2CppYgomSystem.Htjson;
using Il2CppYgomSystem.Utility;
namespace DuelLinksAccess
{
    public partial class DialogHandler
    {
        private GameObject _chronicleExchangeRoot;
        private static string ChronicleItemLabel(GameObject icon)
        {
            if (icon == null || icon.name != "Icon") return null;
            string path = null;
            Transform row = null;
            var current = icon.transform;
            for (int depth = 0; current != null && depth < 8; depth++, current = current.parent)
            {
                var node = current.GetComponent<HtjsonNode>();
                if (path == null && node?.replaceParam != null
                    && node.replaceParam.TryGetValue("%sublooppath%", out var value)
                    && ChronicleItemPolicy.IsRowPath(value?.ToString())) path = value.ToString();
                if (row == null && current.name == "item") row = current;
                // The row's Htjson scope can be on its wrapping hl, above item.
                if (row != null && path != null) break;
            }
            if (path == null || row == null)
            {
                DebugLogger.Log(LogCategory.Handler, "ChronicleItems", $"No row binding: row={row?.name} path={path}");
                return null;
            }
            var area = row.Find("TextArea");
            if (area == null) return null;
            string name = LabelExtractor.GetLabel(area.gameObject);
            if (string.IsNullOrWhiteSpace(name) || name == "TextArea") return null;
            string raw = ClientWork.getStringByJsonPath(path + ".num", "");
            int remaining = ClientWork.getIntByJsonPath(path + ".num", -1);
            int total = ClientWork.getIntByJsonPath(path + ".max", -1);
            return ChronicleItemPolicy.Counts(LabelExtractor.StripRichText(raw), remaining, total, out remaining, out total)
                ? Loc.Get("chronicle_item_details", name, remaining, total)
                : Loc.Get("chronicle_item_details_unknown", name);
        }
        private void ApplyChronicleItemLabels(GameObject root)
        {
            _chronicleExchangeRoot = null;
            if (root == null || root.name != "HtjsonDialog") return;
            int count = 0;
            foreach (var item in _items)
            {
                if (item.Go == null || item.Go.name != "Icon") continue;
                try
                {
                    string label = ChronicleItemLabel(item.Go);
                    if (label == null) continue;
                    var icon = item.Go;
                    item.Label = label;
                    item.LiveLabel = () => ChronicleItemLabel(icon) ?? label;
                    count++;
                }
                catch (Exception ex) { MelonLoader.MelonLogger.Warning("[ChronicleItems] " + ex.Message); }
            }
            if (count > 0)
            {
                _chronicleExchangeRoot = root;
                foreach (var item in _items)
                    if (item.Go != null && item.Go.name == "Scrollbar")
                        item.Label = Loc.Get("chronicle_item_scrollbar");
                MelonLoader.MelonLogger.Msg($"[ChronicleItems] Labeled {count} native item preview controls");
            }
        }
    }
}
