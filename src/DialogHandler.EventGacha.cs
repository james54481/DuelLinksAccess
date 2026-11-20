using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Il2CppYgomSystem.Utility;
using Il2CppYgomSystem.Htjson;
namespace DuelLinksAccess
{
    public partial class DialogHandler
    {
        internal static int EventGachaReceiptGeneration { get; private set; }
        private GameObject _lastGachaReceiptRoot;
        private static string GachaRow(GameObject go, bool receipt = false)
        {
            var current = go != null ? go.transform : null;
            for (int depth = 0; depth < 6 && current != null; depth++, current = current.parent)
            {
                var data = current.GetComponent<HtjsonNode>()?.replaceParam;
                if (data == null) continue;
                foreach (string key in new[] { "%datapath%", "%sublooppath%" })
                    if (data.TryGetValue(key, out var value) && value != null
                        && (receipt ? EventGachaPolicy.IsReceiptRow(value.ToString()) : EventGachaPolicy.IsRewardRow(value.ToString())))
                        return value.ToString();
            }
            return null;
        }
        private static bool IsEventGachaRewardList(GameObject root)
            => root.GetComponentsInChildren<Button>(false).Any(button => GachaRow(button.gameObject) != null);

        private bool IsEventGachaReceipt(GameObject root)
        {
            var page = GetTopVcRoot("content");
            if (page == null) return false;
            bool gacha = page.GetComponentsInChildren<HtjsonNode>(false).Any(node => node.replaceParam != null
                && node.replaceParam.TryGetValue("%basepath%", out var path) && path?.ToString() == "EventGacha.state");
            return gacha && root.GetComponentsInChildren<Button>(false).Any(button => GachaRow(button.gameObject, true) != null);
        }

        private void AddEventGachaReceipt(GameObject root)
        {
            if (!IsEventGachaReceipt(root)) return;
            if (_lastGachaReceiptRoot != root)
            { _lastGachaReceiptRoot = root; EventGachaReceiptGeneration++; }
            var controls = root.GetComponentsInChildren<Button>(false)
                .Select(button => (Button: button, Path: GachaRow(button.gameObject, true)))
                .Where(entry => entry.Path != null).ToArray();
            _items.RemoveAll(item => item.Type == ItemType.Header || (item.Go != null && GachaRow(item.Go, true) != null));
            var rows = new List<DialogItem> { new DialogItem { Go = root, Type = ItemType.Header, Label = Loc.Get("gacha_received_heading") } };
            int receiptRows = controls.Select(entry => entry.Path).Distinct().Count();
            foreach (var group in controls.GroupBy(entry => entry.Path))
            {
                string path = group.Key;
                var scope = group.First().Button.transform;
                for (var parent = scope.parent; parent != null && parent != root.transform; parent = parent.parent)
                {
                    if (controls.Any(entry => entry.Path != path && entry.Button.transform.IsChildOf(parent))) break;
                    scope = parent;
                }
                bool cardRow = ClientWork.getStringByJsonPath(path + ".type", "") == "card";
                long copies = ClientWork.getLongByJsonPath(path + ".num", -1);
                string name = "UNKNOWN", label;
                if (cardRow)
                {
                    var card = scope.GetComponentInChildren<Il2CppYgomGame.Htjson.CardWidget>(false);
                    if (card != null && card.loaded && !ReferenceEquals(card.iid, null) && card.iid.GetCategory().ToString() == "CARD")
                        name = CardFormatter.GetName(card.iid.GetMrk());
                    label = Loc.Get("gacha_received_card", name, copies > 0 ? copies.ToString() : "UNKNOWN");
                }
                else
                {
                    // The receipt's item title can live on ButtonSet/Icon,
                    // while its TextArea is an empty detail target.
                    string native = null;
                    foreach (var entry in group)
                    {
                        string candidate = LabelExtractor.GetLabel(entry.Button.gameObject);
                        if (EventGachaPolicy.TryUnitLabel(candidate, out _, out _)) { native = candidate; break; }
                    }
                    if (native == null)
                        native = AccessibleUiText.ReadParts(scope.gameObject).FirstOrDefault(text => EventGachaPolicy.TryUnitLabel(text, out _, out _));
                    bool unitsKnown = EventGachaPolicy.TryUnitLabel(native, out var parsedName, out long units);
                    if (!string.IsNullOrWhiteSpace(parsedName) && !LabelExtractor.IsPlaceholderText(parsedName)) name = parsedName;
                    long? total = unitsKnown && copies > 0 ? EventGachaPolicy.TotalUnits(units, copies) : null;
                    label = EventGachaPolicy.IsSingleQuantityReceipt(receiptRows, copies)
                        ? Loc.Get("gacha_received_single_item", name, unitsKnown ? units.ToString() : "UNKNOWN")
                        : Loc.Get("gacha_received_item", name, unitsKnown ? units.ToString() : "UNKNOWN",
                            copies > 0 ? copies.ToString() : "UNKNOWN", total?.ToString() ?? "UNKNOWN");
                }
                var target = group.FirstOrDefault(entry => entry.Button.gameObject.name == "card").Button
                    ?? group.FirstOrDefault(entry => entry.Button.gameObject.name == "InfoButton").Button;
                Action open = null;
                if (target != null)
                {
                    var rect = target.transform.TryCast<RectTransform>();
                    open = () => { if (!ClickDialogTab(rect)) ScreenReader.Say(Loc.Get("dialog_click_error")); };
                }
                rows.Add(new DialogItem { Go = root, SettingsTarget = target != null ? target.gameObject : scope.gameObject,
                    Type = open != null ? ItemType.Button : ItemType.Information, Label = label, NativeAction = open, ReadDetails = open });
                DebugLogger.Log(LogCategory.Handler, "GachaReceived", path + ": " + label);
            }
            _items.InsertRange(0, rows);
        }

        private void AddEventGachaRewardList(GameObject root)
        {
            var controls = root.GetComponentsInChildren<Button>(false)
                .Select(button => (Button: button, Path: GachaRow(button.gameObject)))
                .Where(entry => entry.Path != null).ToArray();
            if (controls.Length == 0) return;
            _items.RemoveAll(item => item.Type == ItemType.Header || (item.Go != null && GachaRow(item.Go) != null));
            var rows = new List<DialogItem>();
            rows.Add(new DialogItem { Go = root, Type = ItemType.Header, Label = Loc.Get("gacha_reward_heading") });
            foreach (var group in controls.GroupBy(entry => entry.Path))
            {
                string path = group.Key;
                var scope = group.First().Button.transform;
                // Expand within this row to include its right-hand owned count,
                // stopping before any ancestor containing a different reward.
                for (var parent = scope.parent; parent != null && parent != root.transform; parent = parent.parent)
                {
                    if (controls.Any(entry => entry.Path != path && entry.Button.transform.IsChildOf(parent))) break;
                    scope = parent;
                }
                string type = ClientWork.getStringByJsonPath(path + ".type", "");
                string name = null;
                if (type == "card")
                {
                    var card = scope.GetComponentInChildren<Il2CppYgomGame.Htjson.CardWidget>(false);
                    if (card != null && card.loaded && !ReferenceEquals(card.iid, null) && card.iid.GetCategory().ToString() == "CARD")
                        name = CardFormatter.GetName(card.iid.GetMrk());
                }
                else if (type == "item") name = NativeRewardName.FromPath(path + ".id");
                if (string.IsNullOrWhiteSpace(name)) name = "UNKNOWN";
                string label;
                if (type == "card")
                    label = Loc.Get("gacha_reward_owned", name, EventGachaPolicy.UniqueQuantity(AccessibleUiText.ReadParts(scope.gameObject)));
                else
                {
                    long count = ClientWork.getLongByJsonPath(path + ".count", -1);
                    label = Loc.Get("gacha_reward_quantity", name, count >= 0 ? count.ToString() : "UNKNOWN");
                }
                var target = group.FirstOrDefault(entry => entry.Button.gameObject.name == "card").Button;
                if (target == null) target = group.FirstOrDefault(entry => entry.Button.gameObject.name == "InfoButton").Button;
                if (target == null) target = group.FirstOrDefault(entry => entry.Button.gameObject.name == "ButtonSet").Button;
                Action open = null;
                if (target != null)
                {
                    var rect = target.transform.TryCast<RectTransform>();
                    open = () => { if (!ClickDialogTab(rect)) ScreenReader.Say(Loc.Get("dialog_click_error")); };
                }
                rows.Add(new DialogItem { Go = root, SettingsTarget = target != null ? target.gameObject : scope.gameObject,
                    Type = open != null ? ItemType.Button : ItemType.Header, Label = label, NativeAction = open, ReadDetails = open });
                DebugLogger.Log(LogCategory.Handler, "GachaReward", $"{path}: {label}; target={target?.gameObject.name ?? "none"}");
            }
            _items.InsertRange(0, rows);
        }
    }
}
