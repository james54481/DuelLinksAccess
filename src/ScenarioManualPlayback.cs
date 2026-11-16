using System;
using Il2CppYgomSystem.UI;
using Il2CppYgomGame.Scenario;
using MelonLoader;

namespace DuelLinksAccess
{
    // james54481, 2026-11-12: start each loaded conversation in manual mode.
    // Use native settings only; do not pause scripted transitions or emit clicks.
    internal static class ScenarioManualPlayback
    {
        private static long _appliedController;
        private static long _failedController;
        internal static void Update()
        {
            long pointer = 0;
            try
            {
                var managers = ViewControllerManager.namedManager;
                if (managers == null || !managers.TryGetValue("content", out var manager)) return;
                var top = manager?.GetStackTopViewController();
                if (top?.gameObject == null || !top.gameObject.name.Contains("Scenario"))
                { _appliedController = _failedController = 0; return; }
                var vc = top.TryCast<ScenarioPlayViewController>();
                if (vc == null) vc = top.gameObject.GetComponentInChildren<ScenarioPlayViewController>(false);
                var controller = vc?.sc;
                if (controller == null) return;
                pointer = controller.Pointer.ToInt64();
                if (pointer == _failedController || !ScenarioPlaybackPolicy.Initialize(pointer,
                    _appliedController, vc.IsInit() && controller.work != null)) return;
                controller.SetAutoPlay(false);
                controller.setFastForward(false);
                _appliedController = pointer;
                MelonLogger.Msg($"[ScenarioPlayback] Requested native manual mode, fastForward={controller.work.isFastForward()}, scenario={controller.work.scenarioName}");
            }
            catch (Exception ex)
            {
                _failedController = pointer;
                MelonLogger.Warning($"[ScenarioPlayback] Manual mode initialization failed: {ex.Message}");
            }
        }
    }
}
