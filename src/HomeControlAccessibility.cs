using UnityEngine;
using Il2CppYgomGame.Single;
using Il2CppYgomSystem.UI;

namespace DuelLinksAccess
{
    internal static class HomeControlAccessibility
    {
        internal static bool ActivateDetails(GameObject root, GameObject target)
        {
            if (root == null || root.name != "Single" || target == null || target.name != "MenuRightBase") return false;
            try
            {
                var managers = ViewControllerManager.namedManager;
                if (managers == null || !managers.TryGetValue("content", out var manager)) return false;
                var top = manager?.GetStackTopViewController();
                if (top == null || top.gameObject != root) return false;
                var nativeTarget = root.transform.Find("Layer4/SingleFooter/MenuRoot/MenuRightBase");
                if (nativeTarget == null || nativeTarget.gameObject != target || !target.activeInHierarchy) return false;
                var selectable = target.GetComponent<UnityEngine.UI.Selectable>();
                if (selectable != null && !selectable.IsInteractable()) return true;
                new SingleViewController(top.Pointer).OnCharacterButton();
                MelonLoader.MelonLogger.Msg("[HomeControl] Native OnCharacterButton invoked for MenuRightBase");
                return true;
            }
            catch (System.Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"[HomeControl] Character details failed: {ex.Message}");
                ScreenReader.Say(Loc.Get("screen_click_error"));
                return true; // Never dispatch a second click after an uncertain native action.
            }
        }
    }
}
