using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Il2CppYgomSystem.UI;

namespace DuelLinksAccess
{
    internal static class DailyRewardOverlay
    {
        private static float _pollAt;
        private static float _clickUntil;
        private static GameObject _caption;
        private static int _announced;
        private static string _diagnosed;
        private static string _lastError;

        internal static bool Update()
        {
            try { return UpdateCore(); }
            catch (Exception ex)
            {
                _caption = null;
                if (_lastError != ex.Message)
                {
                    _lastError = ex.Message;
                    DebugLogger.Log(LogCategory.Handler, "HomeOverlay", "Inspection failed: " + ex.Message);
                }
                return false;
            }
        }

        private static bool UpdateCore()
        {
            if (GameStateTracker.CurrentScreen != GameScreen.Home
                || GameStateTracker.LastViewControllerName != "Single")
            { _caption = null; _announced = 0; return false; }
            var managers = ViewControllerManager.namedManager;
            if (managers == null || !managers.TryGetValue("content", out var content)) return false;
            if (managers.TryGetValue("dialog", out var dialog) && dialog?.GetStackTopViewController() != null) return false;
            var single = content?.GetStackTopViewController()?.gameObject;
            if (single == null) return false;
            if (Time.unscaledTime >= _pollAt)
            {
                _pollAt = Time.unscaledTime + 0.25f;
                _caption = null;
                var hit = CenterHit();
                Diagnose(hit);
                foreach (var text in Resources.FindObjectsOfTypeAll<Text>())
                {
                    if (text == null || !text.gameObject.activeInHierarchy) continue;
                    string value = LabelExtractor.StripRichText(text.text ?? "").Trim();
                    if (!DailyRewardPolicy.IsCaption(value)) continue;
                    if (DailyRewardPolicy.CanHandle(true, false, true, SameOverlay(text.gameObject, hit, single)))
                    { _caption = text.gameObject; break; }
                }
                if (_caption == null)
                    foreach (var text in Resources.FindObjectsOfTypeAll<YgomTextAccessor>())
                    {
                        if (text == null || !text.gameObject.activeInHierarchy) continue;
                        string value = LabelExtractor.StripRichText(text.text ?? "").Trim();
                        if (DailyRewardPolicy.IsCaption(value) && SameOverlay(text.gameObject, hit, single))
                        { _caption = text.gameObject; break; }
                    }
            }
            if (_caption == null || !_caption.activeInHierarchy) { _announced = 0; return false; }
            if (_announced != _caption.GetInstanceID())
            {
                _announced = _caption.GetInstanceID();
                ScreenReader.Say(Loc.Get("daily_reward_continue"));
            }
            bool activate = InputManager.TryConsumeKeyDown(KeyCode.Return)
                || InputManager.TryConsumeKeyDown(KeyCode.KeypadEnter)
                || InputManager.TryConsumeKeyDown(KeyCode.Space);
            if (activate && Time.unscaledTime >= _clickUntil)
            {
                _clickUntil = Time.unscaledTime + 0.5f;
                // Confirm the same native overlay still owns the center before
                // sending a single, user-requested click to its visible title.
                if (SameOverlay(_caption, CenterHit(), single)
                    && Main.TryGetUiScreenPos(_caption, out var point))
                    Main.ClickViaHardwareMouse(point, "DailyReward-continue", requireForeground: true);
            }
            else if (InputManager.TryConsumeKeyDown(KeyCode.Tab))
                ScreenReader.Say(Loc.Get("daily_reward_continue"));
            // Do not let arrows/Enter reach an obscured home or information list.
            return true;
        }

        private static GameObject CenterHit()
        {
            var system = EventSystem.current;
            if (system == null) return null;
            var data = new PointerEventData(system) { position = new Vector2(Screen.width / 2f, Screen.height / 2f) };
            var hits = new Il2CppSystem.Collections.Generic.List<RaycastResult>();
            system.RaycastAll(data, hits);
            return hits.Count > 0 ? hits[0].gameObject : null;
        }

        private static bool SameOverlay(GameObject caption, GameObject hit, GameObject single)
        {
            if (caption == null || hit == null || single == null) return false;
            int depth = 0;
            for (var p = caption.transform.parent; p != null && depth++ < 6; p = p.parent)
            {
                // A normal world-map label/banner is never a modal prompt.
                if (p == single.transform || single.transform.IsChildOf(p)) return false;
                if (hit.transform == p || hit.transform.IsChildOf(p)) return true;
            }
            return false;
        }

        private static string Path(GameObject go)
        {
            if (go == null) return "none";
            string path = go.name;
            int depth = 0;
            for (var p = go.transform.parent; p != null && depth++ < 8; p = p.parent) path = p.name + "/" + path;
            return path;
        }

        private static void Diagnose(GameObject hit)
        {
            if (!Main.DebugMode) { _diagnosed = null; return; }
            string key = hit == null ? "none" : hit.GetInstanceID() + ":" + Path(hit);
            if (key == _diagnosed) return;
            _diagnosed = key;
            DebugLogger.Log(LogCategory.Handler, "HomeOverlay", "center=" + key);
            if (hit != null)
            {
                int depth = 0;
                for (var p = hit.transform; p != null && depth++ < 4; p = p.parent)
                    DebugLogger.Log(LogCategory.Handler, "HomeOverlay", "ancestor=" + p.name + " components="
                        + string.Join(",", p.GetComponents<Component>().Where(c => c != null).Select(c => c.GetIl2CppType().Name)));
            }
            int count = 0;
            foreach (var image in Resources.FindObjectsOfTypeAll<Image>())
            {
                if (image == null || !image.gameObject.activeInHierarchy || image.sprite == null) continue;
                string name = image.sprite.name ?? "";
                string path = Path(image.gameObject);
                string combined = (name + " " + path).ToLowerInvariant();
                if (!combined.Contains("daily") && !combined.Contains("bonus") && !combined.Contains("reward")
                    && !combined.Contains("login")) continue;
                DebugLogger.Log(LogCategory.Handler, "HomeOverlay", $"image={name} path={path}");
                if (++count >= 40) break;
            }
        }
    }
}
