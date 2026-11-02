using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Il2CppYgomSystem.UI;
using Il2CppYgomGame.Single;
namespace DuelLinksAccess
{
    internal static class GateCostAccessibility
    {
        internal static string Read(GameObject root)
        {
            if (root == null || root.name != "Gate" || !root.activeInHierarchy) return null;
            try
            {
                var managers = ViewControllerManager.namedManager;
                if (managers == null || !managers.TryGetValue("content", out var mgr) || mgr == null) return null;
                var top = mgr.GetStackTopViewController();
                if (top == null || top.gameObject != root) return null;
                var vc = new GateViewController(top.Pointer);
                if (vc.items == null) return Loc.Get("gate_cost_unknown");
                var rows = new List<string>();
                foreach (var row in vc.items)
                {
                    if (row == null || !row.activeInHierarchy) continue;
                    var ids = row.GetComponentsInChildren<Image>(true)
                        .Where(i => i != null && i.gameObject.activeInHierarchy && i.sprite != null)
                        .Select(i => GateCostPolicy.ItemId(i.sprite.name)).Where(i => i.HasValue).Distinct().ToArray();
                    string left = ReadText(row, "textA"), right = ReadText(row, "textB");
                    if (ids.Length != 1 || !GateCostPolicy.Counts(left, right, out int needed, out int owned))
                    { rows.Add(Loc.Get("gate_cost_unknown")); continue; }
                    string name = LabelExtractor.StripRichText(Il2CppYgomGame.Utility.ItemUtil.GetName(ids[0].Value));
                    if (string.IsNullOrWhiteSpace(name)) { rows.Add(Loc.Get("gate_cost_unknown")); continue; }
                    rows.Add(Loc.Get("gate_cost_item", name, needed, owned));
                }
                return rows.Count > 0 ? string.Join(" ", rows) : Loc.Get("gate_cost_unknown");
            }
            catch (Exception ex)
            {
                DebugLogger.Log(LogCategory.Handler, "GateCost", ex.Message);
                return Loc.Get("gate_cost_unknown");
            }
        }
        private static string ReadText(GameObject root, string name)
        {
            var texts = root.GetComponentsInChildren<Text>(true)
                .Where(t => t != null && t.gameObject.activeInHierarchy && t.gameObject.name == name).ToArray();
            return texts.Length == 1 ? LabelExtractor.StripRichText(texts[0].text) : null;
        }
    }
}
