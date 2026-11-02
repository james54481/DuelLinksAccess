using System.Collections.Generic;
using UnityEngine;
using MelonLoader;

namespace DuelLinksAccess
{
    public partial class DialogHandler
    {
        private void TidyGateSwitchItems(GameObject root)
        {
            try
            {
                var managers = Il2CppYgomSystem.UI.ViewControllerManager.namedManager;
                if (managers == null || !managers.TryGetValue("content", out var manager)) return;
                var top = manager?.GetStackTopViewController();
                if (top == null || top.gameObject.name != "Gate" || root == null) return;
                var vc = root.GetComponent<Il2CppYgomGame.Menu.PullDownViewController>();
                if (vc == null || vc.entrys == null || vc.entrys.Length == 0
                    || vc.Args == null || !vc.Args.TryGetValue("icons", out var icons) || icons == null) return;

                // Scope this cleanup to a confirmed native gate selector. Other
                // PullDown dialogs may expose additional functional controls.
                var entries = new HashSet<int>();
                for (int i = 0; i < vc.entrys.Length; i++)
                {
                    if (!GateSwitchPolicy.IsGateIcon(LabelExtractor.ExtractStringFromArray(icons, i))) return;
                    var entry = vc.entrys[i];
                    if (entry == null) return;
                    entries.Add(entry.GetInstanceID());
                }

                // The root click surface and an unbound entry clone were being
                // announced as Button. Neither is a native selectable entry.
                int removed = _items.RemoveAll(item => item.Go != null
                    && (item.Go == root || (item.Go.name == "PullDownEntry(Clone)"
                        && !entries.Contains(item.Go.GetInstanceID()))));
                if (removed > 0) MelonLogger.Msg($"[GateSwitch] Removed {removed} non-entry scan items");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning($"[GateSwitch] Keeping original scan: {ex.Message}");
            }
        }
    }
}
