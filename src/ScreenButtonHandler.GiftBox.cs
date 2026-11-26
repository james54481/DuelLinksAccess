using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Il2CppYgomSystem.UI;
namespace DuelLinksAccess
{
    internal static class GiftBoxAccessibility
    {
        public static bool IsPage(GameObject root)
        {
            try {
                if (root == null || root.name != "HtjsonPage") return false;
                var vc = root.GetComponent<ViewController>();
                Il2CppSystem.Object src;
                return vc?.Args != null && vc.Args.TryGetValue("src", out src)
                    && src?.ToString() == "cwork://Box.list";
            } catch { return false; }
        }
        public static bool IsCurrent()
        {
            try {
                ViewControllerManager manager;
                return ViewControllerManager.namedManager != null
                    && ViewControllerManager.namedManager.TryGetValue("content", out manager)
                    && IsPage(manager.GetStackTopViewController()?.gameObject);
            } catch { return false; }
        }
    }
    public partial class ScreenButtonHandler
    {
        private static string GiftRowLabel(GameObject row)
        {
            var parts = AccessibleUiText.ReadParts(row).Distinct().ToList();
            // Native Box.list repeats: title(vl), gift(item), separator(sp).
            // Read only the immediately preceding title, never search backwards.
            var parent = row.transform.parent;
            int index = row.transform.GetSiblingIndex();
            if (row.name == "item" && parent?.name == "ScrollContent" && index > 0) {
                var title = parent.GetChild(index - 1);
                if (title.name == "vl" && title.gameObject.activeInHierarchy
                    && title.GetComponentsInChildren<Button>(false).Length == 0) {
                    string name = AccessibleUiText.Read(title.gameObject);
                    if (!string.IsNullOrWhiteSpace(name) && !parts.Contains(name)) parts.Insert(0, name);
                }
            }
            var numbers = parts.Where(t => System.Text.RegularExpressions.Regex.IsMatch(t, @"^\d+$")).ToArray();
            if (numbers.Length == 1) {
                parts.Remove(numbers[0]);
                parts.Add(Loc.Get("gift_quantity", numbers[0]));
            }
            return string.Join("。", parts);
        }
        private string _giftSignature;
        private static string GiftSignature(GameObject root)
            => string.Join("|", AccessibleUiText.ReadParts(root));
        private void PollGiftBox()
        {
            if (!GiftBoxAccessibility.IsPage(_screenRoot)) return;
            string signature = GiftSignature(_screenRoot);
            if (_giftSignature != signature) {
                _giftSignature = signature;
                _tabRescanDelay = 0.35f;
            }
        }
        private void AddGiftBoxAccessibility(GameObject root)
        {
            if (!GiftBoxAccessibility.IsPage(root)) return;
            _giftSignature = GiftSignature(root);
            var areas = root.GetComponentsInChildren<Transform>(false)
                .Where(t => t.name == "TextArea").ToArray();
            foreach (var area in areas)
            {
                // Expand only inside this gift row; identical gifts remain separate.
                var scope = area;
                for (int depth = 0; depth < 12 && scope.parent != null && scope.parent != root.transform; depth++) {
                    var next = scope.parent;
                    int rows = 0;
                    foreach (var child in next.GetComponentsInChildren<Transform>(false))
                        if (child.name == "TextArea") rows++;
                    if (rows != 1 || next.GetComponent<ScrollRect>() != null || next.name == "ScrollContent") break;
                    scope = next;
                }
                var row = scope.gameObject;
                if (Main.DebugMode) {
                    for (var ancestor = area; ancestor != null && ancestor != root.transform; ancestor = ancestor.parent) {
                        DebugLogger.Log(LogCategory.Handler, "GiftRow", $"ancestor={ancestor.name} text={AccessibleUiText.Read(ancestor.gameObject)}");
                                                var node = ancestor.GetComponent<Il2CppYgomSystem.Htjson.HtjsonNode>();
                        var parameters = node?.GetReplaceParam();
                        if (parameters != null) foreach (var entry in parameters)
                            if (entry.Key.Contains("path")) DebugLogger.Log(LogCategory.Handler, "GiftRowData", $"node={ancestor.name} {entry.Key}={entry.Value}");
                        // The gift title may be a sibling of the native item.
                        if (ancestor == scope && ancestor.parent != null) {
                            var parent = ancestor.parent;
                            for (int s = 0; s < parent.childCount; s++) {
                                var sibling = parent.GetChild(s);
                                string siblingText = AccessibleUiText.Read(sibling.gameObject);
                                if (siblingText.Length > 300) siblingText = siblingText.Substring(0, 300);
                                DebugLogger.Log(LogCategory.Handler, "GiftRowSibling", $"parent={parent.name} index={s} name={sibling.name} active={sibling.gameObject.activeInHierarchy} text={siblingText}");
                            }
                            break;
                        }
                    }
                }
                var old = _items.FirstOrDefault(i => i.Go == area.gameObject);
                var actionGo = old?.Go ?? area.gameObject;
                _items.RemoveAll(i => i.Go != null && (i.Go == row || i.Go.transform.IsChildOf(scope)));
                string label = GiftRowLabel(row);
                _items.Add(new ScreenItem { Go = actionGo, Type = ItemType.Button,
                    Label = label, LiveLabel = () => GiftRowLabel(row) });
            }
            foreach (var item in _items) {
                if (item.Go?.name == "BackButton") item.Label = Loc.Get("screen_back");
                if (item.Label == "交換票券") {
                    var scope = item.Go.transform.parent;
                    var numbers = scope == null ? new string[0] : AccessibleUiText.ReadParts(scope.gameObject).Distinct().Where(t => System.Text.RegularExpressions.Regex.IsMatch(t, @"^\d+$")).ToArray();
                    var text = Loc.Get("gift_ticket_count", numbers.Length == 1 ? numbers[0] : "UNKNOWN");
                    item.Label = string.IsNullOrWhiteSpace(text) ? item.Label : text;
                }
            }
            // Native footer controls are omitted by the generic raycast scanner.
            // Identify the four pagination buttons only when the visible footer
            // contains a page indicator and exactly four controls in one row.
            var pageText = root.GetComponentsInChildren<Text>(false).FirstOrDefault(t =>
                System.Text.RegularExpressions.Regex.IsMatch(t.text ?? "", @"^\s*\d+\s*/\s*\d+\s*$"));
            if (pageText != null) {
                var footer = pageText.transform.parent;
                for (int depth = 0; depth < 3 && footer != null && footer != root.transform; depth++, footer = footer.parent) {
                    var buttons = footer.GetComponentsInChildren<Button>(false);
                    if (buttons.Length != 4) continue;
                    var sorted = buttons.OrderBy(b => b.transform.position.x).ToArray();
                    string[] keys = { "gift_first_page", "gift_previous_page", "gift_next_page", "gift_last_page" };
                    _items.Insert(0, new ScreenItem { Go = pageText.gameObject, Type = ItemType.Information,
                        Label = Loc.Get("gift_page", pageText.text) });
                    for (int n = 0; n < sorted.Length; n++) {
                        var button = sorted[n]; var key = keys[n];
                        _items.RemoveAll(i => i.Go != null && (i.Go == button.gameObject || i.Go.transform.IsChildOf(button.transform)));
                        _items.Add(new ScreenItem { Go = button.gameObject, Type = ItemType.Button,
                            Label = Loc.Get(key) + (button.interactable ? "" : Loc.Get("gift_unavailable")),
                            NativeAction = () => {
                                if (!button.interactable) { ScreenReader.Say(Loc.Get(key) + Loc.Get("gift_unavailable")); return; }
                                DialogHandler.ClickDialogTab(button.transform.TryCast<RectTransform>());
                                _tabRescanDelay = 0.4f;
                            } });
                    }
                    break;
                }
            }
            var back = _items.Where(i => i.Go?.name == "BackButton").ToArray();
            _items.RemoveAll(i => i.Go?.name == "BackButton");
            _items.AddRange(back);
            if (Main.DebugMode) {
                foreach (var button in root.GetComponentsInChildren<Button>(false))
                    DebugLogger.Log(LogCategory.Handler,"GiftBox", $"button={button.name} parent={button.transform.parent?.name} enabled={button.interactable} text={AccessibleUiText.Read(button.transform.parent?.gameObject)}");
            }
        }
    }
}