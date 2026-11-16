using System;
using UnityEngine;

namespace DuelLinksAccess
{
    public partial class DeckEditHandler
    {
        private bool _operationMenuOpen;
        private int _operationMenuIndex;
        private float _refreshOperationAt;
        private static readonly string[] OperationKeys = {
            "deck_operations_search", "deck_operations_sort",
            "deck_operations_filter", "deck_operations_native"
        };

        // Runs before card input: Enter in this menu must never add/remove a card.
        private bool ProcessOperationMenu()
        {
            if (!_operationMenuOpen)
            {
                if (!InputManager.TryConsumeKeyDown(KeyCode.M)) return false;
                _operationMenuOpen = true;
                _operationMenuIndex = 0;
                ScreenReader.Say(Loc.Get("deck_operations_help"));
                ScreenReader.SayQueued(Loc.Get(OperationKeys[_operationMenuIndex]));
                return true;
            }
            if (InputManager.TryConsumeKeyDown(KeyCode.Escape)
                || InputManager.TryConsumeKeyDown(KeyCode.Backspace)
                || InputManager.TryConsumeKeyDown(KeyCode.M))
            {
                _operationMenuOpen = false;
                AnnounceCurrentCard();
                return true;
            }
            if (InputManager.TryConsumeKeyDown(KeyCode.UpArrow))
            {
                _operationMenuIndex = (_operationMenuIndex + OperationKeys.Length - 1) % OperationKeys.Length;
                ScreenReader.Say(Loc.Get(OperationKeys[_operationMenuIndex]));
            }
            else if (InputManager.TryConsumeKeyDown(KeyCode.DownArrow))
            {
                _operationMenuIndex = (_operationMenuIndex + 1) % OperationKeys.Length;
                ScreenReader.Say(Loc.Get(OperationKeys[_operationMenuIndex]));
            }
            else if (InputManager.TryConsumeKeyDown(KeyCode.Return)
                || InputManager.TryConsumeKeyDown(KeyCode.KeypadEnter))
            {
                _operationMenuOpen = false;
                OpenOperation(_operationMenuIndex);
            }
            else if (InputManager.TryConsumeKeyDown(KeyCode.Tab))
                ScreenReader.Say(Loc.Get(OperationKeys[_operationMenuIndex]));
            // No normal editor shortcuts run while the operations menu is open.
            return true;
        }

        private void OpenOperation(int index)
        {
            try
            {
                var current = TryGetDeckEditVC();
                if (current == null || _vc == null || current.Pointer != _vc.Pointer)
                    throw new InvalidOperationException("Deck editor changed");
                if (index == 3)
                    current.deckMenuClicked();
                else
                {
                    var controls = current.filterAndSort;
                    if (controls == null) throw new InvalidOperationException("Filter controls missing");
                    if (index == 0)
                    {
                        if (controls.searchButton == null || !controls.searchButton.interactable)
                            throw new InvalidOperationException("Search unavailable");
                        controls.SearchSettingClicked(true);
                    }
                    else if (index == 1)
                        controls.SortClicked();
                    else if (index == 2)
                    {
                        if (controls.filterButton == null || !controls.filterButton.interactable)
                            throw new InvalidOperationException("Filters unavailable");
                        controls.FilterSettingClicked(true);
                    }
                    else throw new InvalidOperationException("Unknown operation");
                }
                _refreshOperationAt = Time.unscaledTime + 0.5f;
                DebugLogger.Log(LogCategory.Handler, "DeckEdit", $"Opened operation {index}");
            }
            catch (Exception ex)
            {
                DebugLogger.Log(LogCategory.Handler, "DeckEdit", $"Operation {index} failed: {ex.Message}");
                ScreenReader.Say(Loc.Get("deck_operations_unavailable"));
            }
        }

        private void RefreshAfterOperation()
        {
            if (_refreshOperationAt <= 0f || Time.unscaledTime < _refreshOperationAt) return;
            _refreshOperationAt = 0f;
            var list = GetCurrentList();
            int mrk = _focusIndex >= 0 && _focusIndex < list.Count ? list[_focusIndex] : 0;
            RefreshAllLists();
            _focusIndex = DeckEditPolicy.RestoreFocus(GetCurrentList(), mrk, _focusIndex);
        }
    }
}
