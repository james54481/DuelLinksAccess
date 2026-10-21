using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using Il2CppYgomSystem.UI;

namespace DuelLinksAccess
{
    internal static class GateLevelAccessibility
    {
        internal static IEnumerable<YgomTabButton> Find(GameObject root)
        {
            if (root == null || root.name != "Gate") yield break;
            foreach (var tab in root.GetComponentsInChildren<YgomTabButton>(true))
                if (tab != null && GateLevelPolicy.CanSelect(tab.gameObject.activeInHierarchy,
                    tab.disableTab != null && tab.disableTab.activeInHierarchy)
                    && GateLevelPolicy.ReadLevel(ReadText(tab)).HasValue)
                    yield return tab;
        }

        private static string ReadText(YgomTabButton tab)
        {
            if (tab.isSelected && tab.selectLabel != null) return LabelExtractor.StripRichText(tab.selectLabel.text);
            if (tab.normalLabel != null) return LabelExtractor.StripRichText(tab.normalLabel.text);
            return "";
        }

        internal static string Label(YgomTabButton tab) => Loc.Get(
            tab.isSelected ? "gate_level_selected" : "gate_level", GateLevelPolicy.ReadLevel(ReadText(tab)));

        internal static bool TryActivate(GameObject go)
        {
            var tab = go.GetComponent<YgomTabButton>();
            if (tab == null || !GateLevelPolicy.ReadLevel(ReadText(tab)).HasValue) return false;
            if (!GateLevelPolicy.CanSelect(go.activeInHierarchy,
                tab.disableTab != null && tab.disableTab.activeInHierarchy)) return true;
            // Native tab callback updates Gate options and costs; do not set the level directly.
            if (!tab.isSelected) tab.OnPointerClick(new PointerEventData(EventSystem.current));
            MelonLogger.Msg($"[GateLevel] requested {ReadText(tab)} selected={tab.isSelected}");
            return true;
        }
    }
}
