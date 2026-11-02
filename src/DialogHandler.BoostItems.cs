using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Il2CppYgomSystem.Htjson;

namespace DuelLinksAccess
{
    public partial class DialogHandler
    {
        private DialogItem _pendingBoostItem;
        private GameObject _pendingBoostRoot;
        private float _pendingBoostStart;

        private void QueueBoostActivationLabel(DialogItem item)
        {
            _pendingBoostItem = item;
            _pendingBoostRoot = _dialogRoot;
            _pendingBoostStart = Time.unscaledTime;
        }

        private void PollBoostActivationLabel()
        {
            var item = _pendingBoostItem;
            if (item == null) return;
            // Never interrupt navigation, speak a stale dialog, or replay a click.
            if (_dialogRoot != _pendingBoostRoot || _pendingBoostRoot == null
                || !_pendingBoostRoot.activeInHierarchy || !LabelExtractor.IsAlive(item.Go)
                || GetTopVcRoot(_activeManager) != _pendingBoostRoot
                || _focusIndex < 0 || _focusIndex >= _items.Count
                || !ReferenceEquals(_items[_focusIndex], item))
            { _pendingBoostItem = null; return; }
            float elapsed = Time.unscaledTime - _pendingBoostStart;
            if (elapsed < 0.1f) return;
            try
            {
                string current = item.LiveLabel();
                if (current == item.Label && elapsed < 1f) return;
                _pendingBoostItem = null;
                item.Label = current;
                ScreenReader.Say(current);
            }
            catch { _pendingBoostItem = null; }
        }

        private sealed class BoostRow
        {
            public Transform Root;
            public CheckBoxWidget Check;
            public string Path;
            public int Id;
            public string Name;
        }

        private string[] BoostText(BoostRow row) => row.Root.GetComponentsInChildren<Text>(true)
            .Where(t => t != null && t.gameObject.activeInHierarchy)
            .Select(t => LabelExtractor.StripRichText(t.text)).Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();

        private string BoostUnavailable(BoostRow row)
        {
            if (row.Check == null || !row.Check.gameObject.activeInHierarchy) return Loc.Get("boost_unavailable");
            if (row.Check.interactable) return null;
            string note = Il2CppYgomSystem.Utility.ClientWork.getStringByJsonPath(row.Path + ".note", "");
            return string.IsNullOrWhiteSpace(note) ? Loc.Get("boost_unavailable") : LabelExtractor.StripRichText(note);
        }

        private string BoostLabel(BoostRow row)
        {
            var quantities = BoostText(row).Select(BoostItemPolicy.Quantity).Where(q => q != null).Distinct().ToArray();
            string quantity = quantities.Length == 1 ? quantities[0] : Loc.Get("boost_unknown_quantity");
            string state = BoostUnavailable(row) ?? Loc.Get(row.Check.isChecked ? "boost_selected" : "boost_not_selected");
            return Loc.Get("boost_item_choice", row.Name, quantity, state);
        }

        private void ApplyBoostItemLabels(GameObject root)
        {
            if (root == null || root.name != "HtjsonDialog") return;
            try
            {
                var rows = new List<BoostRow>();
                var candidates = root.GetComponentsInChildren<CheckBoxWidget>(true);
                DebugLogger.Log(LogCategory.Handler, "BoostItems", $"scan root={root.name} checkboxCount={candidates.Length}");
                foreach (var check in candidates)
                {
                    if (check == null || !check.gameObject.activeInHierarchy) continue;
                    string path = null;
                    int id = 0;
                    // Htjson binds the row template on an ancestor, not on
                    // the checkbox. Stop before a container shared by rows.
                    for (var binding = check.transform; binding != null && binding != root.transform;
                        binding = binding.parent)
                    {
                        if (binding.GetComponentsInChildren<CheckBoxWidget>(true)
                            .Count(c => c != null && c.gameObject.activeInHierarchy) > 1) break;
                        var data = binding.GetComponent<HtjsonNode>()?.replaceParam;
                        if (data == null || !data.TryGetValue("%itempath%", out var boundPath)
                            || !data.TryGetValue("%itemid%", out var itemId)
                            || !int.TryParse(itemId?.ToString(), out int boundId)
                            || !BoostItemPolicy.IsRow(boundPath?.ToString(), boundId)) continue;
                        path = boundPath.ToString();
                        id = boundId;
                        break;
                    }
                    DebugLogger.Log(LogCategory.Handler, "BoostItems", $"candidate={GetGameObjectPath(check.gameObject)} boundPath={path ?? "(none)"}");
                    if (path == null) continue;
                    // Require native identity agreement; Response is a shared store.
                    int nativeId = Il2CppYgomSystem.Utility.ClientWork.getIntByJsonPath(path + ".item_id", -1);
                    DebugLogger.Log(LogCategory.Handler, "BoostItems", $"identity path={path} templateId={id} nativeId={nativeId}");
                    if (nativeId != id) continue;
                    string name = LabelExtractor.StripRichText(Il2CppYgomGame.Utility.ItemUtil.GetName(id));
                    DebugLogger.Log(LogCategory.Handler, "BoostItems", $"nativeName id={id} name=[{name}]");
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    Transform rowRoot = null;
                    for (var t = check.transform.parent; t != null && t != root.transform; t = t.parent)
                    {
                        var checks = t.GetComponentsInChildren<CheckBoxWidget>(true)
                            .Count(c => c != null && c.gameObject.activeInHierarchy);
                        string texts = string.Join(" | ", t.GetComponentsInChildren<Text>(true)
                            .Where(text => text != null && text.gameObject.activeInHierarchy)
                            .Select(text => LabelExtractor.StripRichText(text.text)));
                        DebugLogger.Log(LogCategory.Handler, "BoostItems", $"ancestor={GetGameObjectPath(t.gameObject)} checks={checks} text=[{texts}]");
                        if (checks > 1) break;
                        if (checks == 1 && t.GetComponentsInChildren<Text>(true)
                            .Any(text => text != null && text.gameObject.activeInHierarchy
                                && LabelExtractor.StripRichText(text.text) == name)) { rowRoot = t; break; }
                    }
                    if (rowRoot != null) rows.Add(new BoostRow { Root = rowRoot, Check = check,
                        Path = path.ToString(), Id = id, Name = name });
                }
                if (rows.Count == 0 || rows.Select(r => r.Id).Distinct().Count() != rows.Count) return;
                if (Main.DebugMode)
                    foreach (var button in root.GetComponentsInChildren<Button>(true))
                        if (button != null && button.gameObject.activeInHierarchy
                            && !rows.Any(r => button.transform.IsChildOf(r.Root)))
                        {
                            string icons = string.Join(" | ", button.GetComponentsInChildren<Image>(true)
                                .Where(i => i != null && i.sprite != null).Select(i => i.sprite.name).Distinct());
                            DebugLogger.Log(LogCategory.Handler, "BoostItems", $"footer {GetGameObjectPath(button.gameObject)} " +
                                $"interactable={button.interactable} icons=[{icons}]");
                        }
                int insert = _items.FindIndex(i => i.Go != null && rows.Any(r => i.Go.transform.IsChildOf(r.Root)));
                if (insert < 0) insert = 0;
                var replacements = new List<DialogItem>();
                foreach (var row in rows)
                {
                    replacements.Add(new DialogItem { Go = row.Check.gameObject, Type = ItemType.Button,
                        Label = BoostLabel(row), LiveLabel = () => BoostLabel(row),
                        DeferredActivationLabel = true,
                        UnavailableReason = () => BoostUnavailable(row) });
                    var image = row.Root.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b != null
                        && b.gameObject.name == "img" && b.gameObject.activeInHierarchy && b.interactable);
                    if (image != null)
                        replacements.Add(new DialogItem { Go = image.gameObject, Type = ItemType.Button,
                            Label = Loc.Get("boost_item_details", row.Name, string.Join("，", BoostText(row))),
                            LiveLabel = () => Loc.Get("boost_item_details", row.Name, string.Join("，", BoostText(row))) });
                    DebugLogger.Log(LogCategory.Handler, "BoostItems", $"row={row.Path} id={row.Id} label={BoostLabel(row)}");
                }
                _items.RemoveAll(i => i.Go != null && rows.Any(r => i.Go.transform.IsChildOf(r.Root)));
                _items.InsertRange(Math.Min(insert, _items.Count), replacements);
            }
            catch (Exception ex) { DebugLogger.Log(LogCategory.Handler, "BoostItems", ex.ToString()); }
        }
    }
}
