using UnityEngine;
using Il2CppYgomSystem.UI;
using Il2CppYgomGame.Single;

namespace DuelLinksAccess
{
    internal static class GateContextAccessibility
    {
        internal static string Read(GameObject root)
        {
            if (root == null || root.name != "Gate" || !root.activeInHierarchy) return null;
            try
            {
                var managers = ViewControllerManager.namedManager;
                if (managers == null || !managers.TryGetValue("content", out var manager) || manager == null) return null;
                var top = manager.GetStackTopViewController();
                if (top == null || top.gameObject != root) return null;
                var vc = new GateViewController(top.Pointer);
                string opponent = LabelExtractor.StripRichText(vc.nameText?.text);
                if (string.IsNullOrWhiteSpace(opponent)) return null; // still loading
                string context = Loc.Get(GateContextPolicy.Key(vc.isEventGate()), opponent);
                string cost = GateCostAccessibility.Read(root);
                return cost == null ? context : context + " " + cost;
            }
            catch { return Loc.Get(GateContextPolicy.Key(null), "UNKNOWN"); }
        }
    }
}
