using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Il2CppYgomSystem.UI;
using Il2CppYgomGame.Single;

namespace DuelLinksAccess
{
    internal static class GateDataDiagnostics
    {
        internal static void Capture(GameObject root)
        {
            if (!Main.DebugMode || root == null || root.name != "Gate") return;
            try
            {
                var managers = ViewControllerManager.namedManager;
                if (managers == null || !managers.TryGetValue("content", out var manager)) return;
                var top = manager?.GetStackTopViewController();
                if (top == null || top.gameObject != root) return;
                var vc = new GateViewController(top.Pointer);
                foreach (var selectable in root.GetComponentsInChildren<Selectable>(true))
                {
                    if (selectable == null || selectable.gameObject.name != "switch") continue;
                    var go = selectable.gameObject;
                    string path = go.name;
                    for (var parent = go.transform.parent; parent != null; parent = parent.parent)
                        path = parent.name + "/" + path;
                    var rect = go.GetComponent<RectTransform>();
                    var canvas = go.GetComponentInParent<Canvas>();
                    var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                        ? canvas.worldCamera : null;
                    string bounds = "UNKNOWN";
                    if (rect != null)
                    {
                        var corners = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(4);
                        rect.GetWorldCorners(corners);
                        var a = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                        var b = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
                        bounds = $"({a.x},{a.y})-({b.x},{b.y}) bottom-left origin, screen={Screen.width}x{Screen.height}";
                    }
                    Log($"switch identity path={path} active={go.activeInHierarchy} interactable={selectable.interactable} bounds={bounds}");
                    var mission = go.GetComponentInParent<Il2CppYgomGame.Menu.HomeMission>();
                    if (mission != null)
                    {
                        bool isLive = vc.mission != null && vc.mission.Pointer == mission.Pointer;
                        bool isPrefab = vc.missionPrefab != null && vc.missionPrefab.Pointer == mission.Pointer;
                        Log($"switch mission live={isLive} prefab={isPrefab} switchable={mission.isSwitchable} characters={mission.cids?.Count ?? -1} missionButton={mission.missionButton?.gameObject?.name ?? "null"}");
                        foreach (var group in go.GetComponentsInParent<CanvasGroup>(true))
                            Log($"switch visibility group={group.gameObject.name} alpha={group.alpha} interactable={group.interactable} raycasts={group.blocksRaycasts}");
                        Dump(mission.gameObject, "mission-detail", 0);
                    }
                    Dump(go, "switch-detail", 0);
                }
                Log($"level context name={vc.nameText?.text} rewardRate={vc.GetGateRewardRateSelect()}");
                if (vc.ItemRoot != null) Dump(vc.ItemRoot, "cost", 0);
                if (vc.items != null)
                    for (int i = 0; i < vc.items.Length; i++)
                        if (vc.items[i] != null) Dump(vc.items[i], "nativeItem" + i, 0);
                foreach (string key in new[] { "subcontent", "sub" })
                    if (managers.TryGetValue(key, out var sub) && sub != null)
                    {
                        var view = sub.GetStackTopViewController();
                        if (view?.gameObject != null) Dump(view.gameObject, "reward-" + key, 0);
                    }
            }
            catch (Exception ex) { Log("capture failed: " + ex.Message); }
        }

        private static void Dump(GameObject go, string path, int depth)
        {
            if (go == null || !go.activeInHierarchy || depth > 9) return;
            string text = string.Join(" | ", go.GetComponents<Text>().Select(t => LabelExtractor.StripRichText(t.text)));
            string images = string.Join(" | ", go.GetComponents<Image>()
                .Where(i => i.sprite != null).Select(i => i.sprite.name));
            string types = string.Join(",", go.GetComponents<Component>().Where(c => c != null)
                .Select(c => c.GetIl2CppType().Name));
            Log($"{path}/{go.name} text=[{text}] images=[{images}] components=[{types}]");
            var node = go.GetComponent<Il2CppYgomSystem.Htjson.HtjsonNode>();
            if (node?.replaceParam != null)
                foreach (var pair in node.replaceParam)
                    if (pair.Key.Contains("id") || pair.Key.Contains("path") || pair.Key.Contains("num"))
                        Log($"{path}/{go.name} binding {pair.Key}={pair.Value}");
            for (int i = 0; i < go.transform.childCount; i++)
                Dump(go.transform.GetChild(i).gameObject, path + "/" + go.name, depth + 1);
        }
        private static void Log(string text) => DebugLogger.Log(LogCategory.Handler, "GateData", text);
    }
}
