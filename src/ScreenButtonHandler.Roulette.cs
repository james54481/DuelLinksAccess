using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Il2CppYgomSystem.Utility;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace DuelLinksAccess
{
    public partial class ScreenButtonHandler
    {
        private GameObject _rouletteTrackedRoot;
        private GameObject _rouletteFocus;
        private string _roulettePhase;
        private string _roulettePendingPhase;
        private float _roulettePollAt;
        private float _roulettePendingAt;
        private bool _rouletteSpinning;
        private bool _rouletteRefreshing;
        private bool _rouletteFollowControl;

        private static bool IsRouletteControl(GameObject go)
            => go != null && go.name == "btn"
                && RouletteParam(go, "%mappath%") == "Event.Roulette.data.map"
                && RouletteParam(go, "%lotpath%") == "Event.Roulette.data.lot"
                && (RouletteAccessibilityPolicy.TryCost(AccessibleUiText.Read(go), out _, out _)
                    || AccessibleUiText.Read(go) == "決鬥");

        private static bool RouletteHit(GameObject go, out PointerEventData data, out GameObject hit)
        {
            data = null; hit = null;
            if (go == null || !go.activeInHierarchy) return false;
            var selectable = go.GetComponent<Selectable>();
            if (selectable == null || !selectable.IsInteractable()) return false;
            var rect = go.transform.TryCast<RectTransform>();
            var system = EventSystem.current;
            if (rect == null || system == null) return false;
            var canvas = go.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            data = new PointerEventData(system) {
                position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)),
                button = PointerEventData.InputButton.Left };
            var hits = new Il2CppSystem.Collections.Generic.List<RaycastResult>();
            system.RaycastAll(data, hits);
            if (hits.Count == 0 || hits[0].gameObject == null) return false;
            hit = hits[0].gameObject;
            data.pointerCurrentRaycast = hits[0];
            // Use the actual native click handler, not a hidden alias or its parent.
            return ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit) == go;
        }

        private (string Phase, GameObject Control) ReadRouletteState(GameObject root)
        {
            GameObject cost = null, duel = null;
            foreach (var button in root.GetComponentsInChildren<Selectable>(false))
            {
                var go = button.gameObject;
                if (!IsRouletteControl(go) || !RouletteHit(go, out _, out _)) continue;
                if (AccessibleUiText.Read(go) == "決鬥") duel = go;
                else cost = go;
            }
            return (RouletteAccessibilityPolicy.Phase(duel != null, cost != null, _rouletteSpinning), duel ?? cost);
        }

        private void PollRouletteState()
        {
            if (!_scanned || _screenRoot == null || _screenRoot.name != "RouletteTop"
                || Time.unscaledTime < _roulettePollAt) return;
            _roulettePollAt = Time.unscaledTime + 0.25f;
            var state = ReadRouletteState(_screenRoot);
            if (state.Phase == _roulettePhase) { _roulettePendingPhase = null; return; }
            if (_roulettePendingPhase != state.Phase)
            { _roulettePendingPhase = state.Phase; _roulettePendingAt = Time.unscaledTime; return; }
            if (Time.unscaledTime - _roulettePendingAt < 0.4f) return;
            _rouletteFocus = _focusIndex >= 0 && _focusIndex < _items.Count ? _items[_focusIndex].Go : null;
            if (_rouletteFollowControl || _rouletteFocus == null || IsRouletteControl(_rouletteFocus)) _rouletteFocus = state.Control;
            _rouletteRefreshing = true;
            _scanned = false; _scanDelay = 0.1f; _scanAttempts = 0;
        }

        private bool RestoreRouletteFocus()
        {
            if (!_rouletteRefreshing) return false;
            _rouletteRefreshing = false;
            if (_screenRoot == null || _screenRoot.name != "RouletteTop") return false;
            int index = _items.FindIndex(item => item.Go == _rouletteFocus);
            _rouletteFocus = null;
            if (index >= 0) _focusIndex = index;
            // An animation can temporarily remove every wheel control. Keep
            // following the wheel instead of announcing the first menu item.
            if (_rouletteFollowControl && index < 0 && _roulettePhase == "unknown") return true;
            if (_roulettePhase == "duel") ScreenReader.Say(ReadRouletteResult());
            if (_roulettePhase == "duel") _rouletteFollowControl = false;
            AnnounceCurrentItem(queued: true);
            return true;
        }

        private bool TryActivateRouletteControl(GameObject go)
        {
            if (_screenRoot == null || _screenRoot.name != "RouletteTop" || !IsRouletteControl(go)) return false;
            if (!RouletteHit(go, out var data, out var hit))
            { ScreenReader.Say(Loc.Get("screen_rescan")); _scanned = false; _scanDelay = 0.1f; return true; }
            bool isDuel = AccessibleUiText.Read(go) == "決鬥";
            bool wasSpinning = _rouletteSpinning;
            bool handled = ExecuteEvents.ExecuteHierarchy(hit, data, ExecuteEvents.pointerClickHandler) != null;
            if (!handled) { ScreenReader.Say(Loc.Get("screen_click_error")); return true; }
            if (!isDuel)
            {
                _rouletteSpinning = true;
                _rouletteFollowControl = true;
                ScreenReader.Say(Loc.Get(wasSpinning ? "roulette_stop" : "roulette_spinning"));
            }
            return true;
        }

        private static string RouletteParam(GameObject go, string key)
        {
            var current = go != null ? go.transform : null;
            for (int depth = 0; depth < 3 && current != null; depth++, current = current.parent)
            {
                var data = current.GetComponent<Il2CppYgomSystem.Htjson.HtjsonNode>()?.replaceParam;
                if (data != null && data.TryGetValue(key, out var value) && value != null
                    && !string.IsNullOrWhiteSpace(value.ToString())) return value.ToString();
            }
            return "";
        }

        private string RouletteStartLabel(GameObject go)
        {
            if (AccessibleUiText.Read(go) == "決鬥") return "決鬥";
            if (_rouletteSpinning) return Loc.Get("roulette_stop");
            string cost = RouletteAccessibilityPolicy.TryCost(AccessibleUiText.Read(go), out int required, out int owned)
                ? Loc.Get("roulette_cost", required, owned) : Loc.Get("roulette_cost_unknown");
            return Loc.Get("roulette_start") + "。" + cost;
        }

        private static string RouletteProgress(GameObject root, GameObject wheel)
        {
            string area = "UNKNOWN";
            if (int.TryParse(RouletteParam(wheel, "%current%"), out int index) && index >= 0)
            {
                int number = ClientWork.getIntByJsonPath("Event.Roulette.data.area." + index + ".no", -1);
                if (number > 0) area = number.ToString();
            }
            string remaining = AccessibleUiText.ReadParts(root).FirstOrDefault(part =>
                System.Text.RegularExpressions.Regex.IsMatch(part, @"^距離終點還有\s*\d+\s*格$"));
            return Loc.Get("roulette_current_area", area) + (remaining != null ? "。" + remaining : "");
        }

        private string ReadRouletteResult()
        {
            string opponent = "UNKNOWN", steps = "UNKNOWN";
            try
            {
                const string lot = "Event.Roulette.data.lot";
                var rows = ClientWork.getListByJsonPath(lot + ".list", null);
                int? result = RouletteAccessibilityPolicy.ResultIndex(_roulettePhase == "duel",
                    ClientWork.getLongByJsonPath(lot + ".result", -1), rows?.Count ?? 0);
                if (result.HasValue)
                {
                    string row = lot + ".list." + result.Value;
                    var characters = ClientWork.getListByJsonPath(row + ".chara", null);
                    var names = new List<string>();
                    for (int i = 0; characters != null && i < characters.Count; i++)
                    {
                        // The same array also contains item IDs. Only entries
                        // marked as characters may be passed to CharaUtil.
                        if (ClientWork.getStringByJsonPath(row + ".charadata." + i + ".type", "") != "chara") continue;
                        long nativeCid = ClientWork.getLongByJsonPath(row + ".chara." + i, -1);
                        int cid = nativeCid > 0 && nativeCid <= int.MaxValue ? (int)nativeCid : -1;
                        string name = null;
                        if (cid > 0)
                            foreach (var resolve in new Func<int, string>[] {
                                Il2CppYgomGame.Utility.CharaUtil.GetNameWithSeries,
                                Il2CppYgomGame.Utility.CharaUtil.GetNameAndSeries,
                                Il2CppYgomGame.Utility.CharaUtil.GetName })
                            {
                                try { name = LabelExtractor.StripRichText(resolve(cid)); } catch { }
                                if (!string.IsNullOrWhiteSpace(name)) break;
                            }
                        names.Add(string.IsNullOrWhiteSpace(name) ? "UNKNOWN" : name.Trim());
                    }
                    if (names.Count > 0) opponent = string.Join("、", names.Distinct());
                    long baseSteps = ClientWork.getLongByJsonPath(row + ".reward.0.pt", -1);
                    int? multiplier = RouletteAccessibilityPolicy.ResultMultiplier(ClientWork.getLongByJsonPath(row + ".bonus", -1));
                    long? total = RouletteAccessibilityPolicy.ResultSteps(baseSteps, multiplier);
                    if (total.HasValue)
                        steps = multiplier.Value == 1 ? Loc.Get("roulette_result_normal_steps", total.Value)
                            : Loc.Get("roulette_result_bonus_steps", baseSteps, multiplier.Value, total.Value);
                    DebugLogger.Log(LogCategory.Handler, "RouletteResultSpeech",
                        $"index={result.Value} opponent={opponent} baseSteps={baseSteps} multiplier={multiplier?.ToString() ?? "UNKNOWN"} total={total?.ToString() ?? "UNKNOWN"}");
                }
            }
            catch (Exception ex) { DebugLogger.Log(LogCategory.Handler, "RouletteResultSpeech", "Read failed: " + ex.Message); }
            return Loc.Get("roulette_result_details", opponent, steps);
        }

        private void AddRouletteAccessibility(GameObject root)
        {
            if (root == null || root.name != "RouletteTop") return;
            if (_rouletteTrackedRoot != root)
            { _rouletteTrackedRoot = root; _rouletteSpinning = false; _rouletteRefreshing = false; _rouletteFollowControl = false; }
            var state = ReadRouletteState(root);
            _roulettePhase = state.Phase;
            _roulettePendingPhase = null;
            if (state.Phase == "duel") _rouletteSpinning = false;
            _items.RemoveAll(item => IsRouletteControl(item.Go) && item.Go != state.Control);
            _items.RemoveAll(item => item.Go != null
                && (item.Go.GetComponentInParent<Scrollbar>() != null
                    || string.Equals(item.Go.name, "scrollbar", StringComparison.OrdinalIgnoreCase)));
            foreach (var item in _items)
            {
                if (item.Go == null) continue;
                if (item.Go.name == "BackButton") { item.Label = Loc.Get("screen_back"); continue; }
                // The Selectable is often a child of the Htjson node that owns
                // the row parameters (the generic extractor walks these parents too).
                string Param(string key) => RouletteParam(item.Go, key);
                string path = Param("%mappath%");
                int position = RouletteAccessibilityPolicy.MapPosition(path);
                string label = null;
                if (position > 0)
                {
                    string unit = path + ".unit";
                    string type = ClientWork.getStringByJsonPath(unit + ".type", "");
                    if (type == "item")
                    {
                        int id = ClientWork.getIntByJsonPath(unit + ".id", -1);
                        int count = ClientWork.getIntByJsonPath(unit + ".count", -1);
                        string name = id > 0 ? LabelExtractor.StripRichText(Il2CppYgomGame.Utility.ItemUtil.GetName(id)) : "";
                        if (string.IsNullOrWhiteSpace(name)) name = Loc.Get("shop_unknown_item");
                        label = count >= 0 ? Loc.Get("roulette_route_quantity", position, name, count)
                            : Loc.Get("roulette_route_item", position, name);
                    }
                    else if (type == "ep")
                    {
                        string title = RouletteAccessibilityPolicy.BalloonText(ClientWork.getStringByJsonPath(unit + ".link", ""));
                        if (!string.IsNullOrWhiteSpace(title)) label = Loc.Get("roulette_route_item", position, title);
                    }
                }
                else if (item.Go.name == "ButtonSet" && Param("%maprewardpath%") == "Event.Roulette.data.map.reward")
                {
                    string rewardPath = Param("%maprewardpath%");
                    string name = NativeRewardName.FromPath(rewardPath + ".id");
                    if (string.IsNullOrWhiteSpace(name) && int.TryParse(Param("%id%"), out int id) && id > 0)
                        name = LabelExtractor.StripRichText(Il2CppYgomGame.Utility.ItemUtil.GetName(id));
                    if (string.IsNullOrWhiteSpace(name)) name = Loc.Get("shop_unknown_item");
                    string reward = int.TryParse(Param("%count%"), out int count)
                        ? Loc.Get("roulette_item_quantity", name, count) : name;
                    label = Loc.Get("roulette_goal_reward", reward);
                }
                else if (RouletteAccessibilityPolicy.IsStartControl(item.Go.name, path,
                    Param("%lotpath%"), AccessibleUiText.Read(item.Go)))
                {
                    var wheel = item.Go;
                    label = RouletteStartLabel(wheel);
                    item.LiveLabel = () => RouletteStartLabel(wheel);
                }
                else if (item.Go.name == "ButtonSet" && Param("%datapath%").StartsWith("Event.Roulette.data.lot.list.", StringComparison.Ordinal))
                {
                    if (int.TryParse(Param("%id%"), out int id) && id > 0)
                    {
                        string name = LabelExtractor.StripRichText(Il2CppYgomGame.Utility.ItemUtil.GetName(id));
                        if (!string.IsNullOrWhiteSpace(name))
                            label = int.TryParse(Param("%count%"), out int count)
                                ? Loc.Get("roulette_item_quantity", name, count) : name;
                    }
                }
                if (label != null) item.Label = label;
                if (Main.DebugMode)
                    DebugLogger.Log(LogCategory.Handler, "Roulette", $"{item.Go.name}: {item.Label}; path={path}; text={AccessibleUiText.Read(item.Go)}");
            }
            var start = _items.FirstOrDefault(item => item.Go == state.Control);
            if (start != null)
            {
                var wheel = start.Go;
                _items.Insert(0, new ScreenItem { Go = root, Type = ItemType.Information,
                    Label = RouletteProgress(root, wheel), LiveLabel = () => RouletteProgress(root, wheel) });
            }
            if (_roulettePhase == "duel")
                _items.Insert(Math.Min(1, _items.Count), new ScreenItem { Go = root, Type = ItemType.Information,
                    Label = ReadRouletteResult(), LiveLabel = ReadRouletteResult });
            if (Main.DebugMode)
            {
                DebugLogger.Log(LogCategory.Handler, "Roulette", "Visible text: " + AccessibleUiText.Read(root));
                DumpRouletteResultData(root);
            }
        }

        private static void DumpRouletteResultData(GameObject root)
        {
            void Dump(string path, int depth)
            {
                if (depth > 5) return;
                var obj = ClientWork.getObjectByJsonPath(path, null);
                var dict = obj?.TryCast<Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Object>>();
                if (dict != null)
                {
                    var entries = dict.GetEnumerator();
                    int count = 0;
                    while (entries.MoveNext() && count++ < 30)
                    {
                        string key = entries.Current.Key;
                        if (key == "link" || key == "action" || key == "se") continue;
                        Dump(path + "." + key, depth + 1);
                    }
                    return;
                }
                var list = obj?.TryCast<Il2CppSystem.Collections.Generic.List<Il2CppSystem.Object>>();
                if (list != null)
                {
                    for (int i = 0; i < Math.Min(list.Count, 12); i++) Dump(path + "." + i, depth + 1);
                    return;
                }
                // Native boxed integer ToString() returned a pointer in this
                // game. Use the typed ClientWork accessor for actual numbers.
                long number = ClientWork.getLongByJsonPath(path, long.MinValue);
                string value = number != long.MinValue ? number.ToString()
                    : ClientWork.getStringByJsonPath(path, "UNKNOWN");
                if (value.Length > 200) value = value.Substring(0, 200);
                DebugLogger.Log(LogCategory.Handler, "RouletteResult", path + "=" + value);
            }
            Dump("Event.Roulette.data.lot", 0);
            foreach (var image in root.GetComponentsInChildren<Image>(false))
                if (image.sprite != null && image.GetComponentInParent<Selectable>() != null)
                    DebugLogger.Log(LogCategory.Handler, "RouletteResult", image.gameObject.name + " sprite=" + image.sprite.name);
        }
    }
}
