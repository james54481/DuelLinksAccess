using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Il2CppYgomGame.Deck;

namespace DuelLinksAccess
{
    public partial class ScreenButtonHandler
    {
        private VerticalLayoutGroup _deckSelectionPaddedLayout;
        private GameObject _deckSelectionPaddingRoot;
        private int _deckSelectionOriginalBottom;
        private int _deckSelectionAddedBottom;

        private void RestoreDeckSelectionPadding()
        {
            if (_deckSelectionPaddedLayout != null)
            {
                var p = _deckSelectionPaddedLayout.padding;
                if (p.bottom == _deckSelectionOriginalBottom + _deckSelectionAddedBottom)
                    _deckSelectionPaddedLayout.padding = new RectOffset(p.left, p.right, p.top, _deckSelectionOriginalBottom);
            }
            _deckSelectionPaddedLayout = null;
            _deckSelectionPaddingRoot = null;
            _deckSelectionAddedBottom = 0;
        }

        private float DeckSelectionVisibleBottom(RectTransform view)
        {
            float bottom = view.rect.yMin;
            foreach (var item in _items)
            {
                if (item.Go == null || !item.Go.activeInHierarchy
                    || (!item.Go.name.StartsWith("FooterButton", StringComparison.Ordinal)
                        && item.Go.name != "BackButton" && item.Go.name != "HomeButton")) continue;
                var rect = item.Go.GetComponent<RectTransform>();
                if (rect == null) continue;
                var footer = RectTransformUtility.CalculateRelativeRectTransformBounds(view, rect);
                bottom = Math.Max(bottom, DeckSelectionPolicy.VisibleBottom(view.rect.yMin, view.rect.yMax,
                    footer.min.y, footer.max.y));
            }
            var panel = _screenRoot.GetComponent<DeckSelectViewController>()?.IndicatorPanel;
            if (panel != null)
                foreach (var indicator in panel.GetComponentsInChildren<DeckCopyIndicator>(false))
                {
                    if (indicator.current == DeckCopyIndicator.STATUS.None) continue;
                    var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(view, indicator.transform);
                    bottom = Math.Max(bottom, DeckSelectionPolicy.VisibleBottom(view.rect.yMin, view.rect.yMax, bounds.min.y, bounds.max.y));
                }
            return bottom;
        }

        private void PadDeckSelectionFooter(ScrollRect scroll, RectTransform view, float bottom)
        {
            if (!scroll.vertical || bottom <= view.rect.yMin) return;
            var vc = _screenRoot.GetComponent<DeckSelectViewController>();
            var layout = vc?.deckListContent;
            if (layout == null || scroll.content == null || !layout.transform.IsChildOf(scroll.content)) return;
            if (_deckSelectionPaddingRoot != _screenRoot)
            {
                RestoreDeckSelectionPadding();
                _deckSelectionPaddingRoot = _screenRoot;
                _deckSelectionPaddedLayout = layout;
                _deckSelectionOriginalBottom = layout.padding.bottom;
            }
            float scale = Math.Abs(view.InverseTransformVector(scroll.content.TransformVector(Vector3.up)).y);
            if (scale <= 0.001f) return;
            int extra = Mathf.CeilToInt((bottom - view.rect.yMin) / scale) + 4;
            if (extra <= _deckSelectionAddedBottom) return;
            var p = layout.padding;
            layout.padding = new RectOffset(p.left, p.right, p.top, _deckSelectionOriginalBottom + extra);
            _deckSelectionAddedBottom = extra;
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            Canvas.ForceUpdateCanvases();
        }

        private void RevealDeckSelection(GameObject target)
        {
            if (target == null || _screenRoot == null || !target.transform.IsChildOf(_screenRoot.transform)) return;
            try
            {
                Canvas.ForceUpdateCanvases();
                // Decks live inside a horizontal character row, itself inside
                // the vertical list. Reveal both axes without clicking a deck.
                var scrolls = target.GetComponentsInParent<ScrollRect>(true).ToList();
                var outer = _screenRoot.GetComponent<DeckSelectViewController>()?.deckListView;
                if (outer != null && !scrolls.Any(s => s.Pointer == outer.Pointer)) scrolls.Add(outer);
                foreach (var scroll in scrolls)
                {
                    if (scroll == null || !scroll.gameObject.activeInHierarchy || scroll.content == null
                        || !target.transform.IsChildOf(scroll.content)) continue;
                    var view = scroll.viewport ?? scroll.GetComponent<RectTransform>();
                    if (view == null) continue;
                    float bottom = scroll.vertical ? DeckSelectionVisibleBottom(view) : view.rect.yMin;
                    PadDeckSelectionFooter(scroll, view, bottom);
                    var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(view, target.transform);
                    float dx = scroll.horizontal ? DeckSelectionPolicy.RevealOffset(bounds.min.x, bounds.max.x,
                        view.rect.xMin, view.rect.xMax, scroll.content.rect.width - view.rect.width) : 0f;
                    float dy = scroll.vertical ? DeckSelectionPolicy.RevealOffset(bounds.min.y, bounds.max.y,
                        bottom, view.rect.yMax, scroll.content.rect.height - view.rect.height) : 0f;
                    DebugLogger.Log(LogCategory.Handler, "DeckSelection", $"Visibility {target.name}: y=[{bounds.min.y},{bounds.max.y}] visible=[{bottom},{view.rect.yMax}] padding={_deckSelectionAddedBottom}");
                    if (dx == 0f && dy == 0f) continue;
                    scroll.StopMovement();
                    if (dx != 0f) scroll.horizontalNormalizedPosition = Mathf.Clamp01(scroll.horizontalNormalizedPosition + dx);
                    if (dy != 0f) scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + dy);
                    DebugLogger.Log(LogCategory.Handler, "DeckSelection", $"Reveal {target.name}: horizontal={dx}, vertical={dy}");
                }
            }
            catch (Exception ex) { DebugLogger.Log(LogCategory.Handler, "DeckSelection", "Reveal: " + ex.Message); }
        }

        private static string ReadDeckCharacterGroup(DeckListCharaNode node, string name)
        {
            try
            {
                var levelText = node?.charaLvlText;
                if (levelText == null || !levelText.gameObject.activeInHierarchy)
                    return Loc.Get("deck_chara_group", name);
                int? level = DeckSelectionPolicy.CharacterLevel(LabelExtractor.StripRichText(levelText.text));
                return level.HasValue ? Loc.Get("deck_select_character_level", name, level.Value)
                    : Loc.Get("deck_select_character_level_unknown", name);
            }
            catch { return Loc.Get("deck_chara_group", name); }
        }

        private static bool DeckSlotPurchaseVisible(DeckSelectItem item) =>
            item.purchaseIcon != null && item.purchaseIcon.activeInHierarchy;

        private static string ReadDeckSlot(DeckSelectItem item, string owner, int ordinal)
        {
            if (item == null || !item.gameObject.activeInHierarchy) return Loc.Get("deck_select_unknown");
            try
            {
                bool purchase = DeckSlotPurchaseVisible(item);
                var info = item.Info;
                if (purchase)
                    return string.IsNullOrWhiteSpace(owner) ? Loc.Get("deck_select_add_slot")
                        : Loc.Get("deck_slot_owner", owner, Loc.Get("deck_select_add_slot"));
                string name = DeckSelectionPolicy.FullName(info?.Name,
                    LabelExtractor.StripRichText(item.deckNameText?.text ?? ""));
                if (string.IsNullOrWhiteSpace(name)) name = Loc.Get("deck_slot_empty");
                string label = info?.IsStructure == true ? Loc.Get("deck_select_structure", name)
                    : ordinal > 0 ? Loc.Get("deck_select_slot", ordinal, name) : name;
                if (!string.IsNullOrWhiteSpace(owner)) label = Loc.Get("deck_slot_owner", owner, label);
                bool warning = (item.invalidIcon != null && item.invalidIcon.gameObject.activeInHierarchy && item.invalidIcon.enabled)
                    || (item.invalidIconNew != null && item.invalidIconNew.gameObject.activeInHierarchy && item.invalidIconNew.enabled);
                foreach (string key in DeckSelectionPolicy.StatusKeys(info?.IsSet == true,
                    info?.IsFav == true, info?.IsLocked == true, warning))
                    label += "，" + Loc.Get(key);
                DebugLogger.Log(LogCategory.Handler, "DeckSelection",
                    $"owner={item.CharaID} slot={ordinal} deckId={info?.DeckId} label={label}");
                return label;
            }
            catch (Exception ex)
            {
                DebugLogger.Log(LogCategory.Handler, "DeckSelection", ex.Message);
                return Loc.Get("deck_select_unknown");
            }
        }
    }
}
