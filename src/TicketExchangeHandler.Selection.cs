using System;
using System.Collections.Generic;
using UnityEngine;
using Il2CppYgomGame.Deck;
namespace DuelLinksAccess
{
    public partial class TicketExchangeHandler
    {
        private bool _ticketSelectedFocused;
        private int _ticketSelectedIndex;
        private string TicketSelectionProgress()
            => Loc.Get("ticket_selection_progress", _vc?.selList?.Count ?? 0,
                _vc != null && _vc.selNum > 0 ? _vc.selNum.ToString() : "UNKNOWN");
        private List<CardAndRarity> SelectedTicketCards()
        {
            var cards = new List<CardAndRarity>();
            if (_vc?.selList == null) return cards;
            foreach (var car in _vc.selList) {
                bool exists = false;
                foreach (var other in cards) if (other.Mrk == car.Mrk && other.Kirarity == car.Kirarity) { exists = true; break; }
                if (!exists) cards.Add(car);
            }
            return cards;
        }
        private void AnnounceSelectedTicket(bool verbose = false)
        {
            var cards = SelectedTicketCards();
            if (cards.Count == 0) { ScreenReader.Say(Loc.Get("ticket_selection_empty")); return; }
            _ticketSelectedIndex = Math.Max(0, Math.Min(_ticketSelectedIndex, cards.Count - 1));
            var car = cards[_ticketSelectedIndex];
            int copies = 0;
            foreach (var selected in _vc.selList)
                if (selected.Mrk == car.Mrk && selected.Kirarity == car.Kirarity) copies++;
            ScreenReader.Say(Loc.Get("ticket_selection_card",
                verbose ? CardFormatter.FormatVerbose(car.Mrk) : CardFormatter.FormatCompact(car.Mrk),
                copies, _ticketSelectedIndex + 1, cards.Count));
        }
        private bool ProcessTicketSelectionInput()
        {
            if (_vc == null || GetModeName() != "CardTicket") return false;
            if (InputManager.TryConsumeKeyDown(KeyCode.Tab)) {
                if (_confirmFocused) { _confirmFocused = false; _ticketSelectedFocused = false; AnnounceCurrentCard(); }
                else if (_ticketSelectedFocused) { _ticketSelectedFocused = false; _confirmFocused = true; ScreenReader.Say(Loc.Get("ticket_confirm_focus") + "。" + TicketSelectionProgress()); }
                else { _ticketSelectedFocused = true; _ticketSelectedIndex = 0; ScreenReader.Say(Loc.Get("ticket_selection_zone") + "。" + TicketSelectionProgress()); AnnounceSelectedTicket(); }
                return true;
            }
            if (InputManager.TryConsumeKeyDown(KeyCode.G)) { ScreenReader.Say(TicketSelectionProgress()); return true; }
            if (InputManager.TryConsumeKeyDown(KeyCode.Delete)) {
                if (_operationCooldown > 0f) return true;
                bool clearAll = InputManager.IsKeyHeld(KeyCode.LeftControl) || InputManager.IsKeyHeld(KeyCode.RightControl);
                if (clearAll) {
                    var button = _vc.clearButton;
                    if (button != null && button.gameObject.activeInHierarchy && button.IsInteractable()) {
                        button.onClick.Invoke();
                        ScreenReader.Say((_vc.selList?.Count ?? 0) == 0 ? Loc.Get("ticket_selection_cleared") : Loc.Get("ticket_activate_error"));
                    } else ScreenReader.Say(Loc.Get("ticket_selection_empty"));
                } else {
                    var cards = SelectedTicketCards();
                    if (!_ticketSelectedFocused || cards.Count == 0) { ScreenReader.Say(Loc.Get("ticket_selection_remove_hint")); return true; }
                    _ticketSelectedIndex = Math.Max(0, Math.Min(_ticketSelectedIndex, cards.Count - 1));
                    var car = cards[_ticketSelectedIndex];
                    if (_vc.isDeletable(car) && _vc.delFromRewardView(car)) {
                        ScreenReader.Say(Loc.Get("ticket_selection_removed", CardFormatter.GetName(car.Mrk)));
                        ScreenReader.SayQueued(TicketSelectionProgress());
                    } else ScreenReader.Say(Loc.Get("ticket_activate_error"));
                }
                _operationCooldown = OperationCooldownTime;
                return true;
            }
            if (!_ticketSelectedFocused) return false;
            if (InputManager.TryConsumeKeyDown(KeyCode.LeftArrow) || InputManager.TryConsumeKeyDown(KeyCode.UpArrow)) {
                _ticketSelectedIndex--; AnnounceSelectedTicket(); return true;
            }
            if (InputManager.TryConsumeKeyDown(KeyCode.RightArrow) || InputManager.TryConsumeKeyDown(KeyCode.DownArrow)) {
                _ticketSelectedIndex++; AnnounceSelectedTicket(); return true;
            }
            if (InputManager.TryConsumeKeyDown(KeyCode.V) || InputManager.TryConsumeKeyDown(KeyCode.I)) { AnnounceSelectedTicket(true); return true; }
            if (InputManager.TryConsumeKeyDown(KeyCode.Return) || InputManager.TryConsumeKeyDown(KeyCode.KeypadEnter) || InputManager.TryConsumeKeyDown(KeyCode.Space)) {
                AnnounceSelectedTicket(); return true;
            }
            if (InputManager.TryConsumeKeyDown(KeyCode.Escape) || InputManager.TryConsumeKeyDown(KeyCode.Backspace)) {
                _ticketSelectedFocused = false; AnnounceCurrentCard(); return true;
            }
            // Other keys must not navigate the available-card list underneath.
            if (InputManager.TryConsumeKeyDown(KeyCode.Home)) { _ticketSelectedIndex = 0; AnnounceSelectedTicket(); return true; }
            if (InputManager.TryConsumeKeyDown(KeyCode.End)) { _ticketSelectedIndex = SelectedTicketCards().Count - 1; AnnounceSelectedTicket(); return true; }
            return false;
        }
    }
}