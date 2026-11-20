using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Il2CppYgomSystem.Utility;
using System.Collections.Generic;
using UnityEngine.EventSystems;
namespace DuelLinksAccess
{
    public partial class ScreenButtonHandler
    {
        private GameObject _gachaDrawRoot;
        private HashSet<int> _gachaPreviousEffects = new HashSet<int>();
        private int _gachaReceiptGeneration;
        private bool _gachaPrompted;
        private float _gachaClickAt;
        private void RememberEventGachaDraw(GameObject go)
        {
            if (_screenRoot == null || !EventGachaPolicy.IsDrawPath(RouletteParam(go, "%datapath%"))) return;
            _gachaDrawRoot = _screenRoot;
            _gachaPreviousEffects.Clear();
            foreach (var effect in _screenRoot.GetComponentsInChildren<Il2CppYgomGame.Htjson.LwfWidget>(false))
                _gachaPreviousEffects.Add(effect.GetInstanceID());
            _gachaPrompted = false;
            _gachaReceiptGeneration = DialogHandler.EventGachaReceiptGeneration;
            _gachaClickAt = Time.unscaledTime + 0.8f;
        }
        private bool HandleEventGachaAnimation()
        {
            if (_gachaDrawRoot == null) return false;
            if (_screenRoot != _gachaDrawRoot) { _gachaDrawRoot = null; return false; }
            if (DialogHandler.EventGachaReceiptGeneration != _gachaReceiptGeneration)
            { _gachaDrawRoot = null; return false; }
            var effects = _screenRoot.GetComponentsInChildren<Il2CppYgomGame.Htjson.LwfWidget>(false)
                .Where(effect => !_gachaPreviousEffects.Contains(effect.GetInstanceID())).ToArray();
            // A disappearing effect can be a transition before the receipt.
            // Keep underlying draw controls blocked until that receipt was seen.
            if (!_gachaPrompted)
            { _gachaPrompted = true; ScreenReader.Say(Loc.Get("gacha_result_continue")); }
            bool continueKey = InputManager.TryConsumeKeyDown(KeyCode.Return)
                || InputManager.TryConsumeKeyDown(KeyCode.KeypadEnter) || InputManager.TryConsumeKeyDown(KeyCode.Space);
            if (InputManager.TryConsumeKeyDown(KeyCode.Tab)) ScreenReader.Say(Loc.Get("gacha_result_continue"));
            if (!continueKey || Time.unscaledTime < _gachaClickAt) return true;
            // Only acknowledge a newly-created native draw animation. Never
            // dispatch the cached draw button while the effect covers the page.
            foreach (var effect in effects)
            {
                var rect = effect.transform.TryCast<RectTransform>();
                DebugLogger.Log(LogCategory.Handler, "GachaAnimation", $"effect={effect.gameObject.name} rect={rect != null}");
                if (rect == null || rect.rect.width <= 0 || rect.rect.height <= 0) continue;
                var canvas = effect.GetComponentInParent<Canvas>();
                var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                var point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
                if (point.x < 0 || point.y < 0 || point.x >= Screen.width || point.y >= Screen.height) continue;
                var system = EventSystem.current;
                if (system == null) continue;
                var data = new PointerEventData(system) { position = point };
                var hits = new Il2CppSystem.Collections.Generic.List<RaycastResult>();
                system.RaycastAll(data, hits);
                if (hits.Count > 0 && EventGachaPolicy.IsDrawPath(RouletteParam(hits[0].gameObject, "%datapath%"))) continue;
                _gachaClickAt = Time.unscaledTime + 0.8f;
                Main.ClickViaHardwareMouse(point, "event gacha result continuation");
                return true;
            }
            // This event can reuse an existing animation rather than create a
            // new LwfWidget. Its result advances on a real click in the central
            // artwork area (verified in the native UI). Only expose this route
            // after our own draw, on the same page, and away from page actions.
            var centre = new Vector2(Screen.width / 2f, Screen.height / 2f);
            var eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                var pointer = new PointerEventData(eventSystem) { position = centre };
                var hits = new Il2CppSystem.Collections.Generic.List<RaycastResult>();
                eventSystem.RaycastAll(pointer, hits);
                bool action = false, card = false;
                if (hits.Count > 0 && hits[0].gameObject != null)
                {
                    var hit = hits[0].gameObject;
                    action = EventGachaPolicy.IsDrawPath(RouletteParam(hit, "%datapath%"));
                    card = hit.GetComponentInParent<Il2CppYgomGame.Htjson.CardWidget>() != null;
                    for (var current = hit.transform; current != null; current = current.parent)
                        action |= current.name.StartsWith("FooterButton", StringComparison.Ordinal)
                            || current.name == "BackButton" || current.name == "HomeButton";
                }
                if (EventGachaPolicy.CanAcknowledge(_gachaDrawRoot != null, _screenRoot == _gachaDrawRoot, action, card))
                {
                    _gachaClickAt = Time.unscaledTime + 0.8f;
                    bool sent = Main.ClickViaHardwareMouse(centre, "event gacha result artwork");
                    DebugLogger.Log(LogCategory.Handler, "GachaAnimation", $"Same-page result acknowledgement sent={sent}; newEffects={effects.Length}");
                    if (sent) return true;
                }
            }
            ScreenReader.Say(Loc.Get("gacha_animation_unknown"));
            return true;
        }
        private static string EventGachaDrawLabel(string path)
        {
            long number = ClientWork.getLongByJsonPath(path + ".num", -1);
            long cost = ClientWork.getLongByJsonPath(path + ".cost", -1);
            return EventGachaPolicy.ValidDraw(number, cost)
                ? (cost == 0 ? Loc.Get("gacha_draw_free", number) : Loc.Get("gacha_draw_cost", number, cost))
                : Loc.Get("gacha_draw_unknown");
        }
        private static string EventGachaCardLabel(GameObject go)
        {
            var card = go.GetComponentInChildren<Il2CppYgomGame.Htjson.CardWidget>(false);
            string name = null;
            if (card != null && card.loaded && !ReferenceEquals(card.iid, null)
                && card.iid.GetCategory().ToString() == "CARD" && card.iid.GetMrk() > 0)
                name = CardFormatter.GetName(card.iid.GetMrk());
            return Loc.Get("gacha_preview_card", string.IsNullOrWhiteSpace(name) ? "UNKNOWN" : name);
        }
        private static string EventGachaBalance(GameObject root)
        {
            string amount = "UNKNOWN";
            foreach (var image in root.GetComponentsInChildren<Image>(false))
            {
                if (image.sprite?.name != "ItemA0135") continue;
                var current = image.transform.parent;
                for (int depth = 0; depth < 3 && current != null && current != root.transform; depth++, current = current.parent)
                {
                    var numbers = AccessibleUiText.ReadParts(current.gameObject).Distinct()
                        .Where(text => EventGachaPolicy.IsQuantity(text)).ToArray();
                    if (numbers.Length == 1) { amount = numbers[0]; break; }
                    if (numbers.Length > 1) break;
                }
                if (amount != "UNKNOWN") break;
            }
            return Loc.Get("gacha_balance", amount);
        }
        private void AddEventGachaAccessibility(GameObject root)
        {
            if (root == null || !_items.Any(item => item.Go != null
                && RouletteParam(item.Go, "%basepath%") == "EventGacha.state")) return;
            _items.RemoveAll(item => item.Go != null && item.Go.GetComponentInParent<Scrollbar>() != null);
            foreach (var item in _items)
            {
                if (item.Go == null) continue;
                if (item.Go.name == "BackButton") item.Label = Loc.Get("screen_back");
                else if (item.Go.name == "HomeButton") item.Label = Loc.Get("deck_view_home");
                else if (item.Go.name == "card" && RouletteParam(item.Go, "%cardpath%") == "EventGacha.state.reward.cards")
                {
                    var go = item.Go;
                    item.Label = EventGachaCardLabel(go);
                    item.LiveLabel = () => EventGachaCardLabel(go);
                }
                else
                {
                    string path = RouletteParam(item.Go, "%datapath%");
                    if (!EventGachaPolicy.IsDrawPath(path)) continue;
                    item.Label = EventGachaDrawLabel(path);
                    item.LiveLabel = () => EventGachaDrawLabel(path);
                }
            }
            _items.Insert(0, new ScreenItem { Go = root, Type = ItemType.Information,
                Label = EventGachaBalance(root), LiveLabel = () => EventGachaBalance(root) });
            if (Main.DebugMode)
                foreach (var image in root.GetComponentsInChildren<Image>(false))
                    if (image.sprite != null)
                        DebugLogger.Log(LogCategory.Handler, "EventGacha", $"image={image.gameObject.name} sprite={image.sprite.name} parentText={AccessibleUiText.Read(image.transform.parent?.gameObject)}");
        }
    }
}
