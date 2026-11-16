using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Il2CppYgomGame.Deck;

namespace DuelLinksAccess
{
    public partial class DialogHandler
    {
        private EventSystem _cardSearchEventSystem;
        private GameObject _cardSearchInputRoot;
        private bool _cardSearchPreviousNavigation;

        private void PollCardSearchKeyboard()
        {
            if (_cardSearchInputRoot == null && _cardSearchEventSystem == null) return;
            if (_cardSearchInputRoot != null && _cardSearchInputRoot.activeInHierarchy
                && GetTopVcRoot("dialog") == _cardSearchInputRoot) return;
            if (_cardSearchEventSystem != null)
                _cardSearchEventSystem.sendNavigationEvents = _cardSearchPreviousNavigation;
            _cardSearchEventSystem = null;
            _cardSearchInputRoot = null;
        }

        private void OwnCardSearchKeyboard(GameObject root, InputField input)
        {
            var events = EventSystem.current;
            if (events == null) return;
            if (_cardSearchInputRoot != root)
            {
                if (_cardSearchEventSystem != null)
                    _cardSearchEventSystem.sendNavigationEvents = _cardSearchPreviousNavigation;
                _cardSearchEventSystem = events;
                _cardSearchInputRoot = root;
                _cardSearchPreviousNavigation = events.sendNavigationEvents;
            }
            // Module key consumption does not consume Unity's native Submit event.
            // Stop native Submit while this dialog is owned, and remove the native
            // field's default keyboard focus so Enter cannot submit/close it first.
            events.sendNavigationEvents = false;
            string selected = events.currentSelectedGameObject?.name ?? "none";
            events.SetSelectedGameObject(null);
            DebugLogger.Log(LogCategory.Handler, "CardSearch",
                $"Keyboard owned; previousFocus={selected} interactable={input.interactable} focused={input.isFocused}");
        }

        private void ApplyCardSearchInput(GameObject root)
        {
            var vc = root?.GetComponent<SearchSettingViewController>();
            if (vc == null) return;
            try
            {
                InputField input = vc.wrapInputField?.m_InputField;
                if (input == null || !input.gameObject.activeInHierarchy) return;
                bool deckSearch = GetTopVcRoot("content")?.name == "DeckSelect";
                var purpose = deckSearch ? TextEditPurpose.DeckSearch : TextEditPurpose.CardSearch;
                var wrapper = vc.wrapInputField.transform;
                var searchButton = vc.searchTextButton;
                int insert = _items.FindIndex(it => it.Go != null
                    && (it.Go.transform.IsChildOf(wrapper)
                        || (searchButton != null && it.Go.transform.IsChildOf(searchButton.transform))));
                _items.RemoveAll(it => it.Go != null
                    && (it.Go.transform.IsChildOf(wrapper)
                        || (searchButton != null && it.Go.transform.IsChildOf(searchButton.transform))));
                var item = new DialogItem
                {
                    Go = root, Type = ItemType.Button,
                    LiveLabel = () => Loc.Get(deckSearch ? "deck_search_deck_input" : "deck_search_input", string.IsNullOrEmpty(input.text)
                        ? Loc.Get("deck_search_empty") : input.text)
                };
                item.Label = item.LiveLabel();
                item.NativeAction = () =>
                {
                    DebugLogger.Log(LogCategory.Handler, "CardSearch", $"Edit requested; interactable={input.interactable} active={input.gameObject.activeInHierarchy}");
                    if (!input.interactable || !input.gameObject.activeInHierarchy)
                    {
                        ScreenReader.Say(Loc.Get("dialog_click_error"));
                        return;
                    }
                    AccessibleTextEditor.Open(input, purpose,
                        () => GetTopVcRoot(_activeManager) == root && input != null
                            && input.gameObject.activeInHierarchy,
                        () =>
                        {
                            // Use the native edit handler to refresh its keyword/Apply button.
                            // Filling text never invokes ApplyClicked or submits a search.
                            vc.OnEdit(input.text);
                            ScanDialog();
                            string applyLabel = LabelExtractor.StripRichText(vc.buttonText?.text ?? "").Trim();
                            int confirm = _items.FindIndex(it => it.NativeAction == null
                                && !string.IsNullOrWhiteSpace(applyLabel) && it.Label == applyLabel);
                            if (confirm >= 0) _focusIndex = confirm;
                            ScreenReader.Say(Loc.Get("deck_search_filled"));
                            AnnounceCurrentItem(queued: true);
                        });
                };
                _items.Insert(insert >= 0 ? Math.Min(insert, _items.Count) : 0, item);
                OwnCardSearchKeyboard(root, input);
                DebugLogger.Log(LogCategory.Handler, "CardSearch", "Accessible search input ready");
            }
            catch (Exception ex) { DebugLogger.Log(LogCategory.Handler, "CardSearch", ex.ToString()); }
        }
    }
}
