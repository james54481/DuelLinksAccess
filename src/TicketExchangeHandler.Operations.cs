using System;
using UnityEngine;
namespace DuelLinksAccess
{
    public partial class TicketExchangeHandler
    {
        private bool _ticketOperationsOpen;
        private int _ticketOperationIndex;
        private float _ticketOperationRefreshAt;
        private float _ticketListPollAt;
        private string _ticketDisplayedSignature;
        private string TicketDisplayedSignature()
        {
            if (_vc == null || GetModeName() != "CardTicket" || _vc.mainChoiceData == null) return null;
            var data = _vc.mainChoiceData;
            var signature = new System.Text.StringBuilder();
            signature.Append(_vc.GetInstanceID()).Append(':').Append(data.GetListNum());
            for (int n = 0; n < data.GetListNum(); n++) {
                var card = data.GetListItem(n);
                signature.Append('|').Append(card.Mrk).Append(':').Append(card.Kirarity);
            }
            return signature.ToString();
        }
        private static readonly string[] TicketOperations = {
            "deck_operations_search", "deck_operations_sort", "deck_operations_filter",
            "ticket_filter_clear", "ticket_sort_reverse"
        };
        private bool ProcessTicketOperations()
        {
            if (GetModeName() != "CardTicket") return false;
            if (!_ticketOperationsOpen) {
                if (!InputManager.TryConsumeKeyDown(KeyCode.M)) return false;
                _ticketOperationsOpen = true; _ticketOperationIndex = 0;
                ScreenReader.Say(Loc.Get("ticket_operations_help"));
                ScreenReader.SayQueued(Loc.Get(TicketOperations[0]));
                return true;
            }
            if (InputManager.TryConsumeKeyDown(KeyCode.Escape) || InputManager.TryConsumeKeyDown(KeyCode.Backspace)
                || InputManager.TryConsumeKeyDown(KeyCode.M)) {
                _ticketOperationsOpen = false; AnnounceCurrentCard(); return true;
            }
            if (InputManager.TryConsumeKeyDown(KeyCode.UpArrow)) {
                _ticketOperationIndex = (_ticketOperationIndex + TicketOperations.Length - 1) % TicketOperations.Length;
                ScreenReader.Say(Loc.Get(TicketOperations[_ticketOperationIndex]));
            } else if (InputManager.TryConsumeKeyDown(KeyCode.DownArrow)) {
                _ticketOperationIndex = (_ticketOperationIndex + 1) % TicketOperations.Length;
                ScreenReader.Say(Loc.Get(TicketOperations[_ticketOperationIndex]));
            } else if (InputManager.TryConsumeKeyDown(KeyCode.Tab)) ScreenReader.Say(Loc.Get(TicketOperations[_ticketOperationIndex]));
            else if (InputManager.TryConsumeKeyDown(KeyCode.Return) || InputManager.TryConsumeKeyDown(KeyCode.KeypadEnter)) {
                _ticketOperationsOpen = false;
                OpenTicketOperation(_ticketOperationIndex);
            }
            // Never pass keys through to select a card or confirm an exchange.
            return true;
        }
        private void OpenTicketOperation(int index)
        {
            try {
                var current = TryGetCardGetterVC();
                if (current == null || _vc == null || current.Pointer != _vc.Pointer)
                    throw new InvalidOperationException("Card list changed");
                var controls = current.filterAndSort;
                if (controls == null || !controls.gameObject.activeInHierarchy)
                    throw new InvalidOperationException("Filter controls unavailable");
                switch (index) {
                    case 0:
                        if (controls.searchButton == null || !controls.searchButton.IsInteractable()) throw new InvalidOperationException("Search disabled");
                        controls.SearchSettingClicked(true); break;
                    case 1: controls.SortClicked(); break;
                    case 2:
                        if (controls.filterButton == null || !controls.filterButton.IsInteractable()) throw new InvalidOperationException("Filter disabled");
                        controls.FilterSettingClicked(true); break;
                    case 3:
                        if (controls.clearButton == null || !controls.clearButton.IsInteractable()) throw new InvalidOperationException("No filters to clear");
                        controls.clearButton.onClick.Invoke(); break;
                    case 4:
                        if (controls.orderButton == null || !controls.orderButton.IsInteractable()) throw new InvalidOperationException("Order disabled");
                        controls.orderButton.isOn = !controls.orderButton.isOn; break;
                    default: throw new InvalidOperationException("Unknown operation");
                }
                _ticketOperationRefreshAt = Time.unscaledTime + 0.5f;
                DebugLogger.Log(LogCategory.Handler, "TicketOperations", $"Opened native operation {index}");
            } catch (Exception ex) {
                DebugLogger.Log(LogCategory.Handler, "TicketOperations", ex.Message);
                ScreenReader.Say(Loc.Get("deck_operations_unavailable"));
            }
        }
        private void RefreshTicketAfterOperation()
        {
            if (GetModeName() != "CardTicket") return;
            // Observe the actual native result, including zero matches and sort
            // changes. This also catches delayed native callbacks after closing.
            if (Time.unscaledTime >= _ticketListPollAt) {
                _ticketListPollAt = Time.unscaledTime + 0.3f;
                string signature = TicketDisplayedSignature();
                if (_ticketDisplayedSignature != null && signature != _ticketDisplayedSignature)
                    _ticketOperationRefreshAt = Time.unscaledTime;
                _ticketDisplayedSignature = signature;
            }
            if (_ticketOperationRefreshAt <= 0f || Time.unscaledTime < _ticketOperationRefreshAt) return;
            _ticketOperationRefreshAt = 0f;
            int mrk = _focusIndex >= 0 && _focusIndex < _cardIds.Count ? _cardIds[_focusIndex] : 0;
            long rarity = _focusIndex >= 0 && _focusIndex < _cardKirarities.Count ? _cardKirarities[_focusIndex] : -1;
            RefreshCardList();
            _focusIndex = 0;
            for (int n = 0; n < _cardIds.Count; n++)
                if (_cardIds[n] == mrk && _cardKirarities[n] == rarity) { _focusIndex = n; break; }
            _confirmFocused = false; _ticketSelectedFocused = false;
            ScreenReader.Say(Loc.Get("ticket_filter_result", _cardIds.Count));
            if (_cardIds.Count > 0) {
                ScreenReader.SayQueued(CardFormatter.FormatCompact(_cardIds[_focusIndex]));
            }
        }
    }
}