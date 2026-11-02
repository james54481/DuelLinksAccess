using UnityEngine;
using Il2CppYgomGame.Menu;
using Il2CppYgomGame.Single;
using Il2CppYgomSystem.UI;

namespace DuelLinksAccess
{
    internal static class GateMissionAccessibility
    {
        private static HomeMission Find(GameObject root, GameObject go)
        {
            if (root == null || root.name != "Gate" || go == null || go.name != "switch") return null;
            var managers = ViewControllerManager.namedManager;
            if (managers == null || !managers.TryGetValue("content", out var manager)) return null;
            var top = manager?.GetStackTopViewController();
            if (top == null || top.gameObject != root) return null;
            var vc = new GateViewController(top.Pointer);
            var mission = go.GetComponentInParent<HomeMission>();
            return mission != null && vc.mission != null && mission.Pointer == vc.mission.Pointer ? mission : null;
        }

        internal static bool IsSwitch(GameObject root, GameObject go) => Find(root, go) != null;

        internal static string Label(GameObject root)
        {
            string name = CurrentName(root);
            return Loc.Get("gate_mission_current", name ?? "UNKNOWN");
        }

        private static string CurrentName(GameObject root)
        {
            try
            {
                var managers = ViewControllerManager.namedManager;
                if (root == null || managers == null || !managers.TryGetValue("content", out var manager)) return null;
                var top = manager?.GetStackTopViewController();
                if (top == null || top.gameObject != root) return null;
                return LabelExtractor.StripRichText(new GateViewController(top.Pointer).nameText?.text);
            }
            catch { return null; }
        }

        private static System.Collections.IEnumerator AnnounceResult(GameObject root, GameObject go, HomeMission mission, string before)
        {
            yield return new WaitForSeconds(0.25f);
            float deadline = Time.unscaledTime + 3f;
            while (Time.unscaledTime < deadline)
            {
                if (root == null || go == null || Find(root, go)?.Pointer != mission.Pointer) yield break;
                string after = CurrentName(root);
                if (!mission.isMoving && !mission.isTweening && !string.IsNullOrWhiteSpace(after) && after != before)
                {
                    ScreenReader.Say(Loc.Get("gate_mission_changed", after));
                    yield break;
                }
                yield return null;
            }
            ScreenReader.Say(Loc.Get("gate_mission_unconfirmed"));
        }

        internal static bool Activate(GameObject root, GameObject go)
        {
            var mission = Find(root, go);
            if (mission == null) return false;
            if (!mission.isSwitchable || mission.isMoving || mission.isTweening || mission.cids == null || mission.cids.Count == 0)
                return true;
            string before = CurrentName(root);
            mission.OnClickSwitch();
            MelonLoader.MelonLogger.Msg("[GateMission] Native OnClickSwitch invoked");
            MelonLoader.MelonCoroutines.Start(AnnounceResult(root, go, mission, before));
            return true;
        }

        internal static bool IsEmptySwitch(GameObject root, GameObject go)
        {
            if (root == null || root.name != "Gate" || go == null || go.name != "switch") return false;
            try
            {
                var managers = ViewControllerManager.namedManager;
                if (managers == null || !managers.TryGetValue("content", out var manager)) return false;
                var top = manager?.GetStackTopViewController();
                if (top == null || top.gameObject != root) return false;
                var vc = new GateViewController(top.Pointer);
                var mission = go.GetComponentInParent<HomeMission>();
                bool live = mission != null && vc.mission != null && mission.Pointer == vc.mission.Pointer;
                int? count = mission?.cids != null ? mission.cids.Count : null;
                // Unknown data must never hide a potentially functional control.
                return GateNavigationLabelPolicy.HideEmptyMissionSwitch(root.name, go.name, live, count);
            }
            catch { return false; }
        }
    }
}
