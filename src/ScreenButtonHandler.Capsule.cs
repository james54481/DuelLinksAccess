using UnityEngine;
using Il2CppYgomGame.DuelCapsule;
namespace DuelLinksAccess
{
    public partial class ScreenButtonHandler
    {
        private string _capsuleStep;
        private GameObject _capsuleRoot;
        internal static float LastCapsuleSeenAt { get; private set; } = -1000f;
        private static string CapsuleStepLabel(string step)
        {
            if (step == "WaitInput") return Loc.Get("capsule_input");
            if (step == "WaitInputOpenDialog") return Loc.Get("capsule_reward_open");
            if (step == "WaitInputFinish") return Loc.Get("capsule_finish");
            return Loc.Get("capsule_animation");
        }
        private void ScanCapsule(GameObject root)
        {
            var vc = root?.GetComponent<DuelCapsuleViewController>();
            if (vc == null) return;
            _items.Clear(); _textMode = false;
            _capsuleRoot = root; _capsuleStep = vc.step.ToString();
            LastCapsuleSeenAt = Time.unscaledTime;
            _items.Add(new ScreenItem { Go = root, Type = ItemType.Information,
                Label = CapsuleStepLabel(_capsuleStep), LiveLabel = () => CapsuleStepLabel(vc.step.ToString()) });
            _focusIndex = 0; _scanned = true;
            ScreenReader.Say(CapsuleStepLabel(_capsuleStep));
            DebugLogger.Log(LogCategory.Handler, "Capsule", $"step={vc.step} coins={vc.currentCoinNum} rolls={vc.rollCount}/{vc.maxRollCount}");
        }
        private bool HandleCapsuleInput()
        {
            if (_screenRoot == null || _screenRoot.name != "DuelCapsule") return false;
            var vc = _screenRoot.GetComponent<DuelCapsuleViewController>();
            if (vc == null) return false;
            LastCapsuleSeenAt = Time.unscaledTime;
            string step = vc.step.ToString();
            if (_capsuleRoot != _screenRoot || step != _capsuleStep) {
                _capsuleRoot = _screenRoot; _capsuleStep = step;
                ScreenReader.Say(CapsuleStepLabel(step));
                DebugLogger.Log(LogCategory.Handler, "Capsule", $"step={step} coins={vc.currentCoinNum} rolls={vc.rollCount}/{vc.maxRollCount}");
            }
            if (InputManager.TryConsumeKeyDown(KeyCode.Tab) || InputManager.TryConsumeKeyDown(KeyCode.G)) {
                ScreenReader.Say(CapsuleStepLabel(step)); return true;
            }
            if (InputManager.TryConsumeKeyDown(KeyCode.Return) || InputManager.TryConsumeKeyDown(KeyCode.KeypadEnter)
                || InputManager.TryConsumeKeyDown(KeyCode.Space)) {
                // Dispatch once, only in native waiting states. Never finish or loop draws.
                if (step == "WaitInput" || step == "WaitInputOpenDialog" || step == "WaitInputFinish") {
                    bool accepted = vc.OnPointerClick();
                    DebugLogger.Log(LogCategory.Handler, "Capsule", $"click step={step} accepted={accepted}");
                    if (!accepted) ScreenReader.Say(Loc.Get("capsule_action_unknown"));
                } else ScreenReader.Say(CapsuleStepLabel(step));
            }
            return true;
        }
    }
}
