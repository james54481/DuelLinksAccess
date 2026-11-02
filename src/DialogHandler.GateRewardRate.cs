using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Il2CppYgomGame.Menu;
using Il2CppYgomSystem.UI;

namespace DuelLinksAccess
{
    public partial class DialogHandler
    {
        private void TidyGateRewardRateItems(GameObject root)
        {
            try
            {
                var managers = ViewControllerManager.namedManager;
                if (root == null || managers == null || !managers.TryGetValue("content", out var manager)
                    || manager?.GetStackTopViewController()?.gameObject?.name != "Gate") return;
                var vc = root.GetComponent<PullDownViewController>();
                if (vc?.entrys == null || vc.entrys.Length == 0) return;
                var entries = vc.entrys.ToArray();
                if (entries.Any(e => e == null || e.name != "PullDownEntryRewardRate(Clone)")) return;
                var ids = new HashSet<int>(entries.Select(e => e.GetInstanceID()));
                _items.RemoveAll(i => i.Go != null && (i.Go == root
                    || (i.Go.name == "PullDownEntry(Clone)" && !ids.Contains(i.Go.GetInstanceID()))));
                foreach (var item in _items.Where(i => i.Go != null && ids.Contains(i.Go.GetInstanceID())))
                {
                    var entry = item.Go;
                    item.Label = GateRewardRateLabel(entry);
                    item.LiveLabel = () => GateRewardRateLabel(entry);
                }
            }
            catch (Exception ex) { DebugLogger.Log(LogCategory.Handler, "GateRewardRate", ex.Message); }
        }

        private static string GateRewardRateLabel(GameObject entry)
        {
            try
            {
                var texts = entry.GetComponentsInChildren<Text>(true)
                    .Where(t => t != null && t.gameObject.activeInHierarchy)
                    .Select(t => LabelExtractor.StripRichText(t.text)?.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToArray();
                // Read values from this native row only. Never infer cost from
                // option index or the currently selected Gate's total cost.
                var rates = texts.Select(t => Regex.Match(t, @"^[xX×]\s*(\d+)$"))
                    .Where(m => m.Success).Select(m => m.Groups[1].Value).Distinct().ToArray();
                var costs = texts.Select(t => Regex.Match(t, @"^(?:□\s*)?(\d+)$"))
                    .Where(m => m.Success).Select(m => m.Groups[1].Value).Distinct().ToArray();
                var images = entry.GetComponentsInChildren<Image>(true)
                    .Where(i => i != null && i.gameObject.activeInHierarchy && i.sprite != null).ToArray();
                var itemIds = images.Select(i => GateCostPolicy.ItemId(i.sprite.name))
                    .Where(id => id.HasValue).Select(id => id.Value).Distinct().ToArray();
                string name = itemIds.Length == 1
                    ? LabelExtractor.StripRichText(Il2CppYgomGame.Utility.ItemUtil.GetName(itemIds[0])) : null;
                string rate = rates.Length == 1 ? rates[0] : "UNKNOWN";
                string cost = costs.Length == 1 ? costs[0] : "UNKNOWN";
                string label = Loc.Get("gate_reward_rate_option", rate,
                    string.IsNullOrWhiteSpace(name) ? "UNKNOWN" : name, cost);
                bool selected = images.Any(i => i.gameObject.name == "Check" && i.sprite.name == "GUI_T_ComCheckmark2");
                return selected ? Loc.Get("deck_auto_selected", label) : label;
            }
            catch { return Loc.Get("gate_reward_rate_option", "UNKNOWN", "UNKNOWN", "UNKNOWN"); }
        }
    }
}
