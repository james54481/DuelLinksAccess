using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Il2CppYgomGame.Single;
using Il2CppYgomGame;
using Il2CppYgomGame.Menu;
using Il2CppYgomSystem.UI;

namespace DuelLinksAccess
{
    // The PC information list lives in subcontent, independently of Single.
    // Only take keyboard ownership after an explicit request from its entry.
    internal static class HomeInformation
    {
        internal static bool Requested { get; private set; }
        private static float _openedAt;
        private static float _backGuardUntil;
        private static bool _registered;
        private static readonly System.Collections.Generic.Dictionary<int, bool> TabStates = new();

        internal static void Register(HarmonyLib.Harmony harmony)
        {
            if (_registered) return;
            try
            {
                harmony.Patch(HarmonyLib.AccessTools.Method(typeof(SingleViewController), "OnBack", new[] { typeof(GameObject) }),
                    prefix: new HarmonyLib.HarmonyMethod(typeof(HomeInformation), nameof(BackPrefix)));
                harmony.Patch(HarmonyLib.AccessTools.Method(typeof(Il2CppYgomSystem.Htjson.ButtonWidget), "SetToggle", new[] { typeof(bool) }),
                    postfix: new HarmonyLib.HarmonyMethod(typeof(HomeInformation), nameof(ToggleObserved)));
                _registered = true;
            }
            catch (Exception ex) { MelonLoader.MelonLogger.Warning("[HomeInformation] Registration: " + ex.Message); }
        }

        private static void ToggleObserved(Il2CppYgomSystem.Htjson.ButtonWidget __instance, bool __0)
        {
            try
            {
                var root = Root();
                if (__instance != null && root != null && __instance.transform.IsChildOf(root.transform))
                    TabStates[__instance.GetInstanceID()] = __0;
            }
            catch { }
        }

        internal static bool? ObservedSelection(Il2CppYgomSystem.Htjson.ButtonWidget widget)
            => widget != null && TabStates.TryGetValue(widget.GetInstanceID(), out bool value) ? value : null;

        private static bool BackPrefix(ref bool __result)
        {
            bool dialogOpen = true;
            try
            {
                var managers = ViewControllerManager.namedManager;
                dialogOpen = managers != null && managers.TryGetValue("dialog", out var dialog)
                    && dialog?.GetStackTopViewController() != null;
            }
            catch { }
            if (!HomeInformationPolicy.BlockNativeBack(Requested, Time.unscaledTime < _backGuardUntil,
                GameStateTracker.LastViewControllerName == "Single", dialogOpen)) return true;
            _backGuardUntil = Time.unscaledTime + 0.75f;
            if (Requested) Close();
            __result = true;
            return false;
        }

        internal static GameObject Root()
        {
            try
            {
                var managers = ViewControllerManager.namedManager;
                if (managers == null || !managers.TryGetValue("subcontent", out var manager)) return null;
                var go = manager?.GetStackTopViewController()?.gameObject;
                return go != null && go.activeInHierarchy && go.name == "BannerList" ? go : null;
            }
            catch { return null; }
        }

        internal static bool Visible(GameObject root)
        {
            if (root == null) return false;
            // The PC panel may remain on the VC stack while slid off-screen.
            // Its scroll viewport, rather than the always-visible toggle,
            // determines whether the list itself is on screen.
            foreach (var scroll in root.GetComponentsInChildren<ScrollRect>(false))
            {
                var rect = scroll.viewport ?? scroll.transform.TryCast<RectTransform>();
                if (rect == null || !rect.gameObject.activeInHierarchy) continue;
                if (rect.GetComponentsInParent<CanvasGroup>(false).Any(g => g.alpha < 0.01f)) continue;
                var canvas = rect.GetComponentInParent<Canvas>();
                var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                var corners = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(4);
                rect.GetWorldCorners(corners);
                var a = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                var b = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
                if (HomeInformationPolicy.HasVisibleArea(a.x, a.y, b.x, b.y, Screen.width, Screen.height)) return true;
            }
            return false;
        }

        internal static void Open(SingleViewController view)
        {
            if (view == null || !view.gameObject.activeInHierarchy) return;
            if (!SetWindowOpen(true)) { ScreenReader.Say(Loc.Get("screen_click_error")); return; }
            Requested = true;
            _openedAt = Time.unscaledTime;
            DebugLogger.Log(LogCategory.Handler, "HomeInformation", "Requested information list");
        }

        internal static bool OwnsInput()
        {
            if (!Requested) return false;
            if (GameStateTracker.LastViewControllerName != "Single") { Requested = false; return false; }
            var root = Root();
            // A tab replaces/hides its scroll viewport during transition.
            // That is not a request to hand keyboard focus back to Single.
            if (HomeInformationPolicy.CanOwn(Requested, GameStateTracker.LastViewControllerName, root?.name)) return true;
            if (Time.unscaledTime - _openedAt < 2f) return true;
            Requested = false;
            ScreenReader.Say(Loc.Get("screen_click_error"));
            return false;
        }

        internal static void Close()
        {
            _backGuardUntil = Time.unscaledTime + 0.75f;
            if (Time.unscaledTime < _closeRetryAt) return;
            _closeRetryAt = Time.unscaledTime + 0.4f;
            try
            {
                if (Requested && Root() != null)
                {
                    if (!SetWindowOpen(false)) { ScreenReader.Say(Loc.Get("screen_click_error")); return; }
                }
            }
            catch (Exception ex) { DebugLogger.Log(LogCategory.Handler, "HomeInformation", ex.Message); }
            Requested = false;
        }

        private static float _closeRetryAt;

        private static bool SetWindowOpen(bool open)
        {
            var manager = SubContentViewControllerManager.Instance;
            var launcher = WindowLauncher.Instance;
            var layout = WindowLayoutManager.Instance;
            if (manager == null || launcher == null || layout == null) return false;
            var window = manager.Window;
            if (layout.GetDisp(window) == open) return true;
            // Use the same PC window launcher as the visible information
            // toggle. Never toggle the obsolete mobile header button.
            launcher.ChangeToggle(window, open, true);
            bool actual = layout.GetDisp(window);
            DebugLogger.Log(LogCategory.Handler, "HomeInformation", $"Window {window}: requested={open}, visible={actual}");
            return actual == open;
        }
    }
}
