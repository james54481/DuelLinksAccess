using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Il2CppYgomGame.Deck;

namespace DuelLinksAccess
{
    public partial class ScreenButtonHandler
    {
        private GameObject _deckSelectionFilterFocus;
        private GameObject _deckSelectionFilterFocusRoot;

        private bool RestoreDeckSelectionFilterFocus()
        {
            if (_deckSelectionFilterFocusRoot == null) return false;
            int index = _screenRoot == _deckSelectionFilterFocusRoot
                ? _items.FindIndex(it => it.Go == _deckSelectionFilterFocus) : -1;
            _deckSelectionFilterFocusRoot = null;
            _deckSelectionFilterFocus = null;
            if (index < 0) return false;
            _focusIndex = index;
            var item = _items[index];
            if (item.LiveLabel != null) item.Label = item.LiveLabel();
            ScreenReader.Say(item.Label);
            return true;
        }

        private void DeckSelectionFilterChanged(GameObject root, GameObject control)
        {
            _deckSelectionFilterFocusRoot = root;
            _deckSelectionFilterFocus = control;
            _tabRescanDelay = 0.5f;
        }

        private void BindDeckSelectionFilter(GameObject root, GameObject scope, GameObject control,
            bool include, Func<string> label, Action action, Func<bool> available)
        {
            if (scope == null || control == null) return;
            int index = _items.FindIndex(it => it.Go != null && (it.Go == scope || it.Go.transform.IsChildOf(scope.transform)));
            _items.RemoveAll(it => it.Go != null && (it.Go == scope || it.Go.transform.IsChildOf(scope.transform)));
            if (!include || !control.activeInHierarchy) return;
            var item = new ScreenItem { Go = control, Type = ItemType.Button, LiveLabel = label, Label = label() };
            item.NativeAction = () => {
                if (_screenRoot != root || GetActiveScreenRoot() != root || !control.activeInHierarchy) return;
                if (!available()) { ScreenReader.Say(Loc.Get("deck_operations_unavailable")); return; }
                // Set the native control once. Its registered callbacks apply filters.
                DeckSelectionFilterChanged(root, control);
                action();
            };
            _items.Insert(index >= 0 ? Math.Min(index, _items.Count) : _items.Count, item);
        }

        private void ApplyDeckSelectionFilters(GameObject root)
        {
            var vc = root?.GetComponent<DeckSelectViewController>();
            if (vc == null || vc.menuButton == null) return;
            try
            {
                bool expanded = vc.menuButton.isOn;
                _items.RemoveAll(it => it.Go != null && it.Go.GetComponentInParent<Scrollbar>() != null);
                BindDeckSelectionFilter(root, vc.menuButton.gameObject, vc.menuButton.gameObject, true,
                    () => Loc.Get(vc.menuButton.isOn ? "deck_select_filter_close" : "deck_select_filter_open",
                        LabelExtractor.StripRichText(vc.menuInfo?.text ?? "")),
                    () => vc.menuButton.isOn = !vc.menuButton.isOn,
                    () => vc.menuButton.interactable && !vc.isMenuTweening);

                if (vc.seriesToggleTbl != null)
                    foreach (var pair in vc.seriesToggleTbl) BindDeckSelectionChoice(root, pair.Value, expanded);
                BindDeckSelectionChoice(root, vc.structureButton, expanded);
                if (vc.favoriteButton != null)
                    BindDeckSelectionFilter(root, vc.favoriteButton.gameObject, vc.favoriteButton.gameObject, expanded,
                        () => Loc.Get(vc.favoriteButton.isOn ? "deck_auto_selected" : "deck_auto_not_selected", Loc.Get("deck_select_filter_favorite")),
                        () => vc.favoriteButton.isOn = !vc.favoriteButton.isOn,
                        () => vc.favoriteButton.interactable);
                if (vc.searchButton != null)
                    BindDeckSelectionFilter(root, vc.searchButton.gameObject, vc.searchButton.gameObject, expanded,
                        () => Loc.Get("deck_select_filter_search", string.IsNullOrWhiteSpace(vc.searchKeyword)
                            ? Loc.Get("deck_search_empty") : vc.searchKeyword),
                        () => vc.SearchButtonClicked(true), () => vc.searchButton.interactable);
                if (vc.clearButton != null)
                    BindDeckSelectionFilter(root, vc.clearButton.gameObject, vc.clearButton.gameObject, expanded && vc.clearButton.interactable,
                        () => Loc.Get("deck_select_filter_clear"), () => vc.clearButton.onClick.Invoke(), () => vc.clearButton.interactable);
            }
            catch (Exception ex) { DebugLogger.Log(LogCategory.Handler, "DeckSelectFilters", ex.ToString()); }
        }

        private void BindDeckSelectionChoice(GameObject root, FilterSettingItem choice, bool expanded)
        {
            if (choice == null || choice.myself == null) return;
            string text = LabelExtractor.StripRichText(choice.label?.text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text)) return;
            BindDeckSelectionFilter(root, choice.gameObject, choice.myself.gameObject, expanded,
                () => Loc.Get(choice.IsSelected ? "deck_auto_selected" : "deck_auto_not_selected", text),
                () => choice.myself.isOn = !choice.myself.isOn, () => choice.myself.interactable);
        }
    }
}
