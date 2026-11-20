using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Il2CppYgomGame.Single;
using Il2CppYgomSystem.Htjson;
using Il2CppYgomSystem.UI;

namespace DuelLinksAccess
{
    public partial class ScreenButtonHandler
    {
        private float _informationPollAt;
        private string _informationSignature;
        private string _informationPendingSignature;
        private float _informationStableAt;
        private string _informationFocus;

        private void AddHomeInformationAccessibility(GameObject root)
        {
            if (root == null || root.name != "Single") return;
            foreach (var item in _items)
            {
                if (item.Go == null || !(item.Go.name == "GeneralInfomationButton"
                    || item.Go.transform.parent?.name == "GeneralInfomationButton")) continue;
                item.Label = Loc.Get("home_information");
                item.NativeAction = () => HomeInformation.Open(root.GetComponent<SingleViewController>());
            }
        }

        private static bool InformationActive(GameObject go)
        {
            if (go == null || !go.activeInHierarchy) return false;
            foreach (var group in go.GetComponentsInParent<CanvasGroup>(false))
                if (group.alpha < 0.01f) return false;
            return true;
        }

        private static string InformationSignature(GameObject root)
        {
            if (root == null) return "";
            return root.GetInstanceID() + ":" + string.Join("|", root.GetComponentsInChildren<Text>(false)
                .Where(t => InformationActive(t.gameObject)).Select(t => t.GetInstanceID() + ":" + t.text));
        }

        private void PollHomeInformation()
        {
            if (!HomeInformation.Requested) { _informationSignature = _informationPendingSignature = null; return; }
            if (Time.unscaledTime < _informationPollAt) return;
            _informationPollAt = Time.unscaledTime + 0.2f;
            string signature = InformationSignature(HomeInformation.Root());
            if (_informationSignature == null) { _informationSignature = signature; return; }
            if (signature == _informationSignature) { _informationPendingSignature = null; return; }
            if (signature != _informationPendingSignature)
            {
                _informationPendingSignature = signature;
                _informationStableAt = Time.unscaledTime + 0.3f;
                return;
            }
            if (Time.unscaledTime < _informationStableAt) return;
            if (_informationFocus == null && _focusIndex >= 0 && _focusIndex < _items.Count)
                _informationFocus = InformationIdentity(_items[_focusIndex].Go, _items[_focusIndex].Label);
            _informationSignature = signature;
            _scanned = false; _scanDelay = 0.1f; _scanAttempts = 0;
        }

        private static string InformationTitle(GameObject go, string fallback)
        {
            if (go == null) return fallback;
            var widget = go.GetComponentInParent<ButtonWidget>();
            var parts = AccessibleUiText.ReadParts(go);
            if (parts.Count == 0)
            {
                // Image links can have a title beside the image. Only use
                // a small ancestor containing this one click target.
                for (var parent = go.transform.parent; parent != null && parent != HomeInformation.Root()?.transform;
                    parent = parent.parent)
                {
                    int clicks = parent.GetComponentsInChildren<HtjsonNode>(false)
                        .Count(n => ExecuteEvents.GetEventHandler<IPointerClickHandler>(n.gameObject) == n.gameObject);
                    if (clicks != 1) break;
                    parts = AccessibleUiText.ReadParts(parent.gameObject);
                    if (parts.Count > 0) break;
                }
            }
            string title = widget?.labelText?.text;
            if (string.IsNullOrWhiteSpace(title)) title = parts.FirstOrDefault(p => !p.All(char.IsDigit));
            if (string.IsNullOrWhiteSpace(title)) title = fallback;
            return LabelExtractor.StripRichText(title ?? "").Trim();
        }

        private static bool? InformationSelected(GameObject go)
        {
            var tab = go.GetComponentInParent<YgomTabButton>();
            if (tab != null) return tab.isSelected;
            var widget = go.GetComponentInParent<ButtonWidget>();
            var root = HomeInformation.Root();
            if (root != null)
            {
                var rows = root.GetComponentsInChildren<HtjsonNode>(false)
                    .Where(n => InformationActive(n.gameObject))
                    .Select(n => InformationRowPath(n.gameObject)).Where(p => p != null).Distinct();
                var selected = HomeInformationPolicy.SelectedTab(InformationTitle(go, ""), rows);
                if (selected.HasValue) return selected;
            }
            var observed = HomeInformation.ObservedSelection(widget);
            if (observed.HasValue) return observed;
            if (widget?.buttonImage == null || widget.toggleSprite == null || widget.normalSprite == null
                || widget.toggleSprite == widget.normalSprite) return null;
            if (widget.buttonImage.sprite == widget.toggleSprite) return true;
            if (widget.buttonImage.sprite == widget.normalSprite) return false;
            return null;
        }

        private static string InformationBannerTitle(GameObject go, string fallback)
        {
            string row = InformationRowPath(go);
            if (row == null) return fallback;
            string art = Il2CppYgomSystem.Utility.ClientWork.getStringByJsonPath(row + ".banner", "");
            string term = LabelExtractor.StripRichText(Il2CppYgomSystem.Utility.ClientWork.getStringByJsonPath(row + ".term", "")).Trim();
            string key = HomeInformationPolicy.BannerKey(art);
            string title = HomeInformationPolicy.GenericTitle(fallback, term)
                ? key == null ? Loc.Get("home_information_unknown") : Loc.Get(key)
                : key == null || fallback.Contains(Loc.Get(key), StringComparison.Ordinal)
                    ? fallback : Loc.Get(key) + "，" + fallback;
            return term.Length == 0 || title.Contains(term, StringComparison.Ordinal) ? title : title + "，" + term;
        }

        private static string InformationRowPath(GameObject go)
        {
            var root = HomeInformation.Root();
            for (var current = go?.transform; current != null && current != root?.transform; current = current.parent)
            {
                var node = current.GetComponent<HtjsonNode>();
                var parameters = node?.GetReplaceParam();
                if (parameters == null) continue;
                foreach (string key in new[] { "%datapath%", "%sublooppath%" })
                    if (parameters.TryGetValue(key, out var value))
                    {
                        string path = HomeInformationPolicy.RowPath(value?.ToString());
                        if (path != null) return path;
                    }
            }
            return null;
        }

        private static string InformationIdentity(GameObject go, string fallback)
        {
            string title = InformationTitle(go, fallback);
            if (HomeInformationPolicy.IsTabTitle(title)) return "tab:" + title;
            return go == null ? fallback : HomeInformationPolicy.ItemKey(InformationRowPath(go), go.GetInstanceID(), InformationAction(go));
        }

        private static string InformationAction(GameObject go)
        {
            var card = go.GetComponentInParent<Il2CppYgomGame.Htjson.CardWidget>();
            // A real card preview is a different action from opening its
            // activity banner, even when both live in the same native row.
            return card == null ? "open" : "card:" + card.GetInstanceID();
        }

        private void ScanHomeInformation(GameObject root)
        {
            // This panel owns its complete list. Never add Header or map objects.
            var candidates = new List<GameObject>();
            foreach (var control in root.GetComponentsInChildren<Selectable>(false))
                if (control.TryCast<Scrollbar>() == null && control.TryCast<Slider>() == null
                    && control.IsInteractable() && InformationActive(control.gameObject)) candidates.Add(control.gameObject);
            foreach (var widget in root.GetComponentsInChildren<ButtonWidget>(false))
                if (widget.button != null && widget.button.IsInteractable() && InformationActive(widget.gameObject))
                    candidates.Add(widget.button.gameObject);
            foreach (var node in root.GetComponentsInChildren<HtjsonNode>(false))
            {
                var go = node.gameObject;
                if (!InformationActive(go) || go.GetComponent<Scrollbar>() != null) continue;
                var button = go.GetComponent<Selectable>() ?? go.GetComponentInChildren<Selectable>(false);
                if (button != null && !button.IsInteractable()) continue;
                var handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(go);
                if (handler == go) candidates.Add(go);
            }
            var unique = new Dictionary<string, GameObject>();
            foreach (var go in candidates.Distinct())
            {
                string row = InformationRowPath(go);
                int containedRows = row != null ? 0 : go.GetComponentsInChildren<HtjsonNode>(false)
                    .Select(n => InformationRowPath(n.gameObject)).Where(p => p != null).Distinct().Count();
                if (HomeInformationPolicy.SkipContainer(row, containedRows)) continue;
                string key = HomeInformationPolicy.ItemKey(row, go.GetInstanceID(), InformationAction(go));
                if (!unique.TryGetValue(key, out var previous)) unique.Add(key, go);
                else if (previous.transform.IsChildOf(go.transform)) unique[key] = go;
            }
            foreach (var go in unique.Values)
            {
                string original = LabelExtractor.GetLabel(go);
                string title = InformationBannerTitle(go, InformationTitle(go, original));
                var card = go.GetComponentInParent<Il2CppYgomGame.Htjson.CardWidget>();
                if (card != null)
                {
                    string name = "UNKNOWN";
                    if (card.loaded && !ReferenceEquals(card.iid, null) && card.iid.GetCategory().ToString() == "CARD")
                        name = CardFormatter.GetName(card.iid.GetMrk());
                    title = Loc.Get("screen_card_detail") + "，" + name;
                }
                if (string.IsNullOrWhiteSpace(title)) continue;
                var tab = go.GetComponentInParent<YgomTabButton>();
                bool isTab = tab != null || HomeInformationPolicy.IsTabTitle(title);
                if (tab != null)
                    title = LabelExtractor.StripRichText((tab.normalLabel ?? tab.selectLabel)?.text ?? title).Trim();
                var capturedTitle = title;
                Func<string> live = () => isTab && InformationSelected(go) == true
                    ? Loc.Get("friend_tab_selected", capturedTitle) : capturedTitle;
                _items.Add(new ScreenItem { Go = go, Type = ItemType.Button, Label = live(), LiveLabel = live,
                    NativeAction = () =>
                    {
                        if (root != _screenRoot || !HomeInformation.Requested || !InformationActive(go)) return;
                        _informationFocus = isTab ? "tab:" + capturedTitle : null;
                        RevealArenaItem(go);
                        Canvas.ForceUpdateCanvases();
                        if (card != null) { card.OnButton(); return; }
                        if (!DispatchInformationClick(go))
                        {
                            _informationFocus = null;
                            ScreenReader.Say(Loc.Get("screen_click_error"));
                            return;
                        }
                        if (isTab) _tabRescanDelay = 0.5f;
                    }
                });
                DebugLogger.Log(LogCategory.Handler, "HomeInformation", $"Item {go.name} row={InformationRowPath(go) ?? "none"}: {live()}");
            }
            _items.Add(new ScreenItem { Go = root, Type = ItemType.Button, Label = Loc.Get("screen_back"),
                NativeAction = HomeInformation.Close });
            _informationSignature = InformationSignature(root);
            if (_informationFocus != null)
            {
                int index = _items.FindIndex(i => InformationIdentity(i.Go, i.Label) == _informationFocus);
                _informationFocus = null;
                if (index >= 0) { _focusIndex = index; AnnounceCurrentItem(); return; }
            }
            _focusIndex = 0;
            ScreenReader.Say(Loc.Get("screen_buttons", _items.Count));
            AnnounceCurrentItem();
        }

        private static bool DispatchInformationClick(GameObject go)
        {
            var widget = go.GetComponentInParent<ButtonWidget>();
            var target = widget?.button?.gameObject ?? go;
            var system = EventSystem.current;
            var rect = target?.transform.TryCast<RectTransform>();
            if (system == null || rect == null) return false;
            var canvas = target.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var data = new PointerEventData(system)
            {
                position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)),
                button = PointerEventData.InputButton.Left
            };
            var hits = new Il2CppSystem.Collections.Generic.List<RaycastResult>();
            system.RaycastAll(data, hits);
            if (hits.Count == 0 || hits[0].gameObject == null) return false;
            var hit = hits[0].gameObject;
            if (hit != target && !hit.transform.IsChildOf(target.transform)) return false;
            data.pointerCurrentRaycast = hits[0];
            var click = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);
            if (click == null) return false;
            ExecuteEvents.Execute(click, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(click, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(click, data, ExecuteEvents.pointerClickHandler);
            return true;
        }
    }
}
