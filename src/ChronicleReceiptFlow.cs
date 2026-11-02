using UnityEngine;
using Il2CppYgomGame.Single;
using Il2CppYgomSystem.UI;
using Il2CppYgomSystem.Htjson;
namespace DuelLinksAccess
{
    internal static class ChronicleReceiptFlow
    {
        private static GameObject _trader;
        private static int _confirmationId;
        private static bool _announced;
        private static float _nextBack;
        internal static void Begin(GameObject confirmation)
        {
            var managers = ViewControllerManager.namedManager;
            if (confirmation == null || managers == null || !managers.TryGetValue("content", out var content)) return;
            var top = content?.GetStackTopViewController();
            if (top?.gameObject?.name != "CardTrader2") return;
            _trader = top.gameObject;
            _confirmationId = confirmation.GetInstanceID();
            _announced = false;
            _nextBack = Time.unscaledTime + 0.5f;
            MelonLoader.MelonLogger.Msg("[ChronicleReceipt] Tracking confirmed exchange through presentation and receipt");
        }
        internal static void Observe()
        {
            if (_trader == null) return;
            var managers = ViewControllerManager.namedManager;
            if (managers == null || !managers.TryGetValue("content", out var content)
                || content?.GetStackTopViewController()?.gameObject != _trader)
            { _trader = null; return; }
            if (!managers.TryGetValue("dialog", out var dialog)) return;
            var root = dialog?.GetStackTopViewController()?.gameObject;
            if (root == null || !root.activeInHierarchy) return;
            bool receipt = false;
            foreach (var node in root.GetComponentsInChildren<HtjsonNode>(false))
            {
                var data = node.replaceParam;
                if (data == null) continue;
                foreach (string key in new[] { "%datapath%", "%sublooppath%" })
                    if (data.TryGetValue(key, out var value)
                        && value?.ToString()?.StartsWith("DialogGet.get_data.item.", System.StringComparison.Ordinal) == true)
                        receipt = true;
            }
            if (receipt || root.GetInstanceID() != _confirmationId)
            {
                MelonLoader.MelonLogger.Msg($"[ChronicleReceipt] New dialog {root.name}; receipt={receipt}; returning input to native dialog handler");
                _trader = null; // Preserve the receipt/error dialog for the user to read and acknowledge.
            }
        }
        internal static bool Handle(CardTraderViewController2 vc)
        {
            if (_trader == null || vc?.gameObject != _trader) return false;
            if (!_announced)
            {
                _announced = true;
                ScreenReader.Say(Loc.Get("trader_result_presentation"));
            }
            bool back = InputManager.TryConsumeKeyDown(KeyCode.Return)
                || InputManager.TryConsumeKeyDown(KeyCode.KeypadEnter)
                || InputManager.TryConsumeKeyDown(KeyCode.Space)
                || InputManager.TryConsumeKeyDown(KeyCode.Escape)
                || InputManager.TryConsumeKeyDown(KeyCode.Backspace);
            if (back && Time.unscaledTime >= _nextBack)
            {
                _nextBack = Time.unscaledTime + 0.75f;
                vc.SendBack(); // Forward user intent to the native Escape route; do not dismiss the future receipt.
                MelonLoader.MelonLogger.Msg("[ChronicleReceipt] Forwarded native back during pending presentation");
            }
            else if (InputManager.TryConsumeKeyDown(KeyCode.Tab)
                || InputManager.TryConsumeKeyDownOrRepeat(KeyCode.UpArrow)
                || InputManager.TryConsumeKeyDownOrRepeat(KeyCode.DownArrow)
                || InputManager.TryConsumeKeyDownOrRepeat(KeyCode.LeftArrow)
                || InputManager.TryConsumeKeyDownOrRepeat(KeyCode.RightArrow))
                ScreenReader.Say(Loc.Get("trader_result_presentation"));
            return true;
        }
    }
}
