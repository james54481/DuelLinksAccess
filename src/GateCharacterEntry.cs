using UnityEngine;
using UnityEngine.EventSystems;
using Il2CppYgomGame.Single;
using Il2CppYgomSystem.UI;

namespace DuelLinksAccess
{
    internal static class GateCharacterEntry
    {
        // Identity comes from the active native VC, never a guessed object name.
        internal static GameObject Find(GameObject root)
        {
            if (root == null || root.name != "Gate" || !root.activeInHierarchy) return null;
            var managers = ViewControllerManager.namedManager;
            if (managers == null || !managers.TryGetValue("content", out var manager) || manager == null) return null;
            var top = manager.GetStackTopViewController();
            if (top == null || top.gameObject != root) return null;
            // As in ScreenButtonHandler.TryCallVcMethod, this game's manager
            // exposes a base VC wrapper. GetComponent<GateViewController>
            // misses it; wrap the verified active Gate pointer explicitly.
            var vc = new GateViewController(top.Pointer);
            var button = vc?.CharaFilterButton;
            DebugLogger.Log(LogCategory.Handler, "GateCharacterEntry",
                $"active Gate filter={(button == null ? "(none)" : button.name)} active={button != null && button.activeInHierarchy}");
            return button != null && button.activeInHierarchy ? button : null;
        }

        internal static bool Activate(GameObject root, GameObject target)
        {
            var button = Find(root);
            if (button == null || target != button) return false;
            var native = button.GetComponentInChildren<YgomButton>(true);
            var clickTarget = native != null && native.gameObject.activeInHierarchy ? native.gameObject : button;
            var selectable = clickTarget.GetComponent<UnityEngine.UI.Selectable>();
            if (selectable != null && !selectable.IsInteractable()) return true;
            var data = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(clickTarget, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(clickTarget, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(clickTarget, data, ExecuteEvents.pointerClickHandler);
            MelonLoader.MelonLogger.Msg("[GateCharacterEntry] native CharaFilterButton clicked");
            return true;
        }
    }
}
