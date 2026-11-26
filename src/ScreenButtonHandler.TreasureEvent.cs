using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Il2CppYgomSystem.Utility;
namespace DuelLinksAccess
{
    public partial class ScreenButtonHandler
    {
        private GameObject _treasureSceneFocus;
        private static bool IsTreasureScene(GameObject go) => Regex.IsMatch(RouletteParam(go, "%no%") ?? "",
            @"^Event\.0198\.data\.top\.specialepisode\.tab\.\d+\.number$");
        private static string TreasureSceneState(GameObject go)
        {
            string label = TreasureSceneLabel(go);
            var button = go.GetComponent<Button>();
            return button != null && !button.interactable ? Loc.Get("friend_tab_selected", label) : label;
        }
        private static bool IsTreasureEvent(GameObject root)
        {
            if (root == null || root.name != "HtjsonPage") return false;
            try {
                var vc = root.GetComponent<Il2CppYgomSystem.UI.ViewController>();
                return vc?.Args != null && vc.Args.TryGetValue("src", out var src)
                    && src?.ToString() == "cwork://Event.0198.page.top";
            } catch { return false; }
        }
        private static string TreasureRewardPath(GameObject go)
        {
            foreach (string key in new[] { "%datapath%", "%sublooppath%" }) {
                string path = RouletteParam(go, key);
                if (Regex.IsMatch(path ?? "", @"^Event\.0198\.data\.top\.reward\.list\.\d+$")) return path;
            }
            return null;
        }
        private static string TreasureRewardLabel(string path)
        {
            string name = NativeRewardName.FromPath(path + ".id") ?? "UNKNOWN";
            long count = ClientWork.getLongByJsonPath(path + ".count", -1);
            long point = ClientWork.getLongByJsonPath(path + ".point", -1);
            string hint = LabelExtractor.StripRichText(ClientWork.getStringByJsonPath(path + ".text", "")).Trim();
            return Loc.Get("treasure_reward", point >= 0 ? point.ToString() : "UNKNOWN", name,
                count >= 0 ? count.ToString() : "UNKNOWN") + (hint.Length == 0 ? "" : "，" + hint);
        }
        private static string TreasureSceneLabel(GameObject go)
        {
            string path = RouletteParam(go, "%no%");
            int number = ClientWork.getIntByJsonPath(path, -1);
            return Loc.Get("treasure_scene", number > 0 ? number.ToString() : "UNKNOWN");
        }
        private static string TreasureCurrencyLabel(GameObject root, GameObject icon)
        {
            var numbers = AccessibleUiText.ReadParts(icon).Distinct().Where(t => Regex.IsMatch(t, @"^\d+$")).ToArray();
            if (numbers.Length == 1) return Loc.Get("treasure_currency", numbers[0]);
            // The quantity is a sibling of the icon, not part of its button.
            // Stay within the smallest native group with this one ButtonSet;
            // stop before it includes the activity score or another reward icon.
            var scope = icon.transform.parent;
            for (int depth = 0; scope != null && scope != root.transform && depth < 4; depth++, scope = scope.parent) {
                int icons = 0;
                foreach (var child in scope.GetComponentsInChildren<Transform>(false))
                    if (child.name == "ButtonSet") icons++;
                if (icons > 1) break;
                var parts = AccessibleUiText.ReadParts(scope.gameObject);
                if (parts.Any(t => t.Contains("活動分數") || Regex.IsMatch(t, @"^\d+\s*分$"))) break;
                numbers = parts.Distinct().Where(t => Regex.IsMatch(t, @"^\d+$")).ToArray();
                if (numbers.Length == 1) return Loc.Get("treasure_currency", numbers[0]);
                if (numbers.Length > 1) break;
            }
            // The number can be a separate sibling outside ButtonSet. Match
            // only visible numeric text in the same horizontal row to its right.
            var rect = icon.GetComponent<RectTransform>();
            if (rect != null) {
                var values = new System.Collections.Generic.HashSet<string>();
                foreach (var text in root.GetComponentsInChildren<Text>(false)) {
                    string value = (text.text ?? "").Trim();
                    if (!Regex.IsMatch(value, @"^\d+$")) continue;
                    var textRect = text.GetComponent<RectTransform>();
                    if (textRect == null) continue;
                    var p = rect.InverseTransformPoint(textRect.TransformPoint(textRect.rect.center));
                    if (p.x > 0 && Mathf.Abs(p.y) < Mathf.Max(8f, rect.rect.height * 0.5f)) values.Add(value);
                }
                if (values.Count == 1) return Loc.Get("treasure_currency", values.First());
            }
            return Loc.Get("treasure_currency", "UNKNOWN");
        }
        private void AddTreasureEventAccessibility(GameObject root)
        {
            if (!IsTreasureEvent(root)) return;
            _items.RemoveAll(i => i.Go != null && i.Go.GetComponentInParent<Scrollbar>() != null);
            // Include the selected native scene button too; the generic scanner
            // omits non-interactable controls used to indicate selection.
            foreach (var button in root.GetComponentsInChildren<Button>(false)) {
                var go = button.gameObject;
                if (!IsTreasureScene(go)) continue;
                _items.RemoveAll(i => i.Go != null && (i.Go == go || i.Go.transform.IsChildOf(go.transform)));
                _items.Insert(0, new ScreenItem { Go = go, Type = ItemType.Button,
                    Label = TreasureSceneState(go), LiveLabel = () => TreasureSceneState(go),
                    NativeAction = () => {
                        if (!button.interactable) { ScreenReader.Say(TreasureSceneState(go)); return; }
                        if (DialogHandler.ClickDialogTab(button.transform.TryCast<RectTransform>())) {
                            _treasureSceneFocus = go; _tabRescanDelay = 0.35f;
                        } else ScreenReader.Say(Loc.Get("screen_click_error"));
                    } });
            }
            foreach (var item in _items) {
                if (item.Go == null) continue;
                if (item.Go.name == "BackButton") { item.Label = Loc.Get("screen_back"); continue; }
                if (item.Go.name == "HomeButton") { item.Label = Loc.Get("deck_view_home"); continue; }
                string reward = TreasureRewardPath(item.Go);
                if (reward != null) {
                    item.Label = TreasureRewardLabel(reward);
                    item.LiveLabel = () => TreasureRewardLabel(reward);
                    continue;
                }
                string scene = RouletteParam(item.Go, "%no%");
                if (Regex.IsMatch(scene ?? "", @"^Event\.0198\.data\.top\.specialepisode\.tab\.\d+\.number$")) {
                    var target = item.Go;
                    item.Label = TreasureSceneState(target); item.LiveLabel = () => TreasureSceneState(target);
                    continue;
                }
                if (item.Go.name == "ButtonSet") {
                    string iid = RouletteParam(item.Go, "%iid%");
                    if (int.TryParse(iid, out int id) && id > 0) {
                        string name = LabelExtractor.StripRichText(Il2CppYgomGame.Utility.ItemUtil.GetName(id));
                        item.Label = Loc.Get("treasure_scene_reward", string.IsNullOrWhiteSpace(name) ? "UNKNOWN" : name);
                    } else if (item.Label == "Button" || item.Label == "button") {
                        var icon = item.Go;
                        item.Label = TreasureCurrencyLabel(root, icon);
                        item.LiveLabel = () => TreasureCurrencyLabel(root, icon);
                    }
                }
            }
            // Keep conditions as native visible text, without treating them as actions.
            var texts = AccessibleUiText.ReadParts(root);
            string score = texts.FirstOrDefault(t => Regex.IsMatch(t, @"^\d+\s*分$"));
            if (score != null) _items.Insert(0, new ScreenItem { Go = root, Type = ItemType.Information,
                Label = Loc.Get("treasure_score", score) });
            foreach (string text in texts.Distinct())
                if (text.Contains("收集活動分數") && text.Contains("完成場景"))
                    _items.Insert(score != null ? 1 : 0, new ScreenItem { Go = root, Type = ItemType.Information, Label = text });
            foreach (var item in _items)
                DebugLogger.Log(LogCategory.Handler, "TreasureEvent", $"control={item.Go?.name} label={item.Label}");
        }
    }
}
