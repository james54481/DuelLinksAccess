using System;
using System.Collections.Generic;
using UnityEngine;
using Il2CppYgomGame.CardList;
using Il2CppYgomGame.Deck;

namespace DuelLinksAccess
{
    /// <summary>
    /// Keyboard-driven ticket exchange accessibility handler.
    /// Provides card browsing, selection, and exchange confirmation
    /// for CardGetterViewController (ticket exchange, card trader, etc.).
    /// </summary>
    public class TicketExchangeHandler
    {
        #region Fields

        private CardGetterViewController _vc;
        private bool _wasActive;
        private string _lastVcGoName = "";

        private int _focusIndex;
        private bool _confirmFocused;
        private readonly List<bool> _hasCardIdentity = new();

        // Card list from the exchange — stored as card IDs (mrk values)
        private readonly List<int> _cardIds = new();

        // Parallel to _cardIds: the Kirarity (rarity + kira treatment) of each
        // card, kept strictly in lockstep. Selecting a card MUST feed the game
        // the exact CardAndRarity it handed us — both Mrk AND Kirarity.
        // Reconstructing from the mrk alone leaves Kirarity=0; the exchange then
        // submits a malformed card and the game hard-resets to the title screen
        // ("An error has occurred") right after TRADE is confirmed.
        // cardExchangeList carries only mrk: those entries are read-only until
        // a complete CardAndRarity list becomes available.
        private readonly List<long> _cardKirarities = new();

        // Cooldown
        private float _operationCooldown;
        private const float OperationCooldownTime = 0.5f;

        // Delayed initial scan
        private float _initialScanDelay;
        private bool _initialScanDone;
        private int _initialScanAttempts;

        // Post-exchange stale-list guard. DecideClicked()/exchangeButton open
        // a confirm dialog that DialogHandler drives entirely on its own —
        // this handler has no callback for when it resolves. Without this,
        // a just-traded card stays in _cardIds at its old index; selecting
        // it again feeds an already-consumed card into native game calls.
        // (2026-07-22 tester log: traded Needle Sunfish, the list still
        // showed it at the same position afterward, re-selecting it
        // preceded the app resetting to the Logo/Title screens.)
        private bool _awaitingExchangeResult;
        private bool _exchangeDialogSeen;
        private float _exchangeRefreshAfter;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the handler is actively managing a ticket exchange screen.
        /// </summary>
        public bool IsActive { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Called each frame from Main.UpdateHandlers().
        /// </summary>
        public void Update()
        {
            if (_operationCooldown > 0f)
                _operationCooldown -= Time.deltaTime;

            // CardGetterViewController can appear on various screens.
            // We detect it by trying to cast the top content VC.
            var vc = TryGetCardGetterVC();
            if (vc == null)
            {
                if (_wasActive) Deactivate();
                return;
            }

            string goName = vc.gameObject?.name ?? "";
            if (!_wasActive || goName != _lastVcGoName
                || _vc?.gameObject?.GetInstanceID() != vc.gameObject?.GetInstanceID())
            {
                Activate(vc, goName);
            }

            if (!_initialScanDone)
            {
                _initialScanDelay -= Time.deltaTime;
                if (_initialScanDelay <= 0f)
                    DoInitialScan();
                return;
            }

            if (_awaitingExchangeResult)
            {
                CheckPostExchangeRefresh();
                if (_awaitingExchangeResult) return;
            }

            ProcessInput();
        }

        #endregion

        #region Lifecycle

        private void Activate(CardGetterViewController vc, string goName)
        {
            _vc = vc;
            _lastVcGoName = goName;
            _wasActive = true;
            IsActive = true;
            _focusIndex = 0;
            _confirmFocused = false;
            _initialScanDone = false;
            _initialScanDelay = 1.0f;
            _initialScanAttempts = 0;
            _awaitingExchangeResult = false;
            _exchangeDialogSeen = false;

            DebugLogger.Log(LogCategory.Handler, "TicketExchange",
                $"Activated (mode={GetModeName()}), waiting for initial scan...");
        }

        /// <summary>
        /// Watches for the confirm dialog opened by ConfirmExchange() to
        /// appear and then close, then forces a fresh read of the exchange
        /// list. A card successfully traded away must never remain
        /// selectable at its old cached index.
        /// </summary>
        private void CheckPostExchangeRefresh()
        {
            if (GameStateTracker.CurrentScreen == GameScreen.Dialog)
            {
                _exchangeDialogSeen = true;
                return;
            }

            // Dialog hasn't appeared yet (still opening) — keep waiting
            // rather than refreshing prematurely and missing the real close.
            // Main gives the dialog handler exclusive input while a dialog is
            // visible, so this handler may never observe the open transition.
            if (!_exchangeDialogSeen && Time.unscaledTime < _exchangeRefreshAfter) return;

            _awaitingExchangeResult = false;
            _exchangeDialogSeen = false;

            int previousMrk = (_cardIds.Count > 0 && _focusIndex >= 0 && _focusIndex < _cardIds.Count)
                ? _cardIds[_focusIndex] : -1;

            RefreshCardList();

            if (_cardIds.Count == 0)
            {
                _focusIndex = 0;
                ScreenReader.Say(Loc.Get("ticket_no_cards"));
                return;
            }

            if (_focusIndex >= _cardIds.Count)
                _focusIndex = _cardIds.Count - 1;

            bool sameCard = previousMrk >= 0 && _cardIds[_focusIndex] == previousMrk;
            if (!sameCard)
                AnnounceCurrentCard(queued: true);

            DebugLogger.Log(LogCategory.Handler, "TicketExchange",
                $"Post-exchange refresh: {_cardIds.Count} cards, focus now " +
                $"{CardFormatter.GetName(_cardIds[_focusIndex])}");
        }

        private void DoInitialScan()
        {
            _initialScanAttempts++;
            RefreshCardList();

            DebugLogger.Log(LogCategory.Handler, "TicketExchange",
                $"Scan attempt {_initialScanAttempts}: {_cardIds.Count} cards, mode={GetModeName()}");

            if (_cardIds.Count == 0 && _initialScanAttempts < 8)
            {
                _initialScanDelay = 0.5f;
                return;
            }

            _initialScanDone = true;

            string modeName = GetModeName();
            if (_cardIds.Count > 0)
            {
                ScreenReader.Say(Loc.Get("ticket_entered", _cardIds.Count));
                AnnounceCurrentCard(queued: true);
            }
            else
            {
                ScreenReader.Say(Loc.Get("ticket_no_cards"));
            }
        }

        private void Deactivate()
        {
            _wasActive = false;
            IsActive = false;
            _vc = null;
            _cardIds.Clear();
            _cardKirarities.Clear();
            _hasCardIdentity.Clear();
            _awaitingExchangeResult = false;
            _exchangeDialogSeen = false;

            DebugLogger.Log(LogCategory.Handler, "TicketExchange", "Deactivated");
        }

        #endregion

        #region VC Detection

        /// <summary>
        /// Attempts to find CardGetterViewController as the top content VC.
        /// </summary>
        private static CardGetterViewController TryGetCardGetterVC()
        {
            try
            {
                var namedMgr = Il2CppYgomSystem.UI.ViewControllerManager.namedManager;
                if (namedMgr == null) return null;

                foreach (string key in new[] { "sub", "content" })
                {
                    if (!namedMgr.TryGetValue(key, out var manager) || manager == null) continue;
                    var candidate = manager.GetStackTopViewController()?.TryCast<CardGetterViewController>();
                    if (candidate?.gameObject?.activeInHierarchy == true) return candidate;
                }
                return null;
            }
            catch (Exception ex)
            {
                DebugLogger.Log(LogCategory.Handler, "TicketExchange",
                    $"TryGetCardGetterVC error: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Data Access

        /// <summary>
        /// Refreshes the available card list from the VC.
        /// </summary>
        private void RefreshCardList()
        {
            _cardIds.Clear();
            _cardKirarities.Clear();
            _hasCardIdentity.Clear();

            try
            {
                if (_vc == null) return;

                // Fallback: try exchangeList (List<CardAndRarity>) — keep the
                // real Kirarity so the exchange submits the exact card.
                var carList = _vc.exchangeList;
                if (carList != null && carList.Count > 0)
                {
                    for (int i = 0; i < carList.Count; i++)
                    {
                        var car = carList[i];
                        _cardIds.Add(car.Mrk);
                        _cardKirarities.Add(car.Kirarity);
                        _hasCardIdentity.Add(true);
                    }

                    DebugLogger.Log(LogCategory.Handler, "TicketExchange",
                        $"Loaded {_cardIds.Count} cards from exchangeList");
                    return;
                }

                // Another fallback: try choiceList (List<CardAndRarity>).
                var choiceList = _vc.choiceList;
                if (choiceList != null && choiceList.Count > 0)
                {
                    for (int i = 0; i < choiceList.Count; i++)
                    {
                        var car = choiceList[i];
                        _cardIds.Add(car.Mrk);
                        _cardKirarities.Add(car.Kirarity);
                        _hasCardIdentity.Add(true);
                    }

                    DebugLogger.Log(LogCategory.Handler, "TicketExchange",
                        $"Loaded {_cardIds.Count} cards from choiceList");
                    return;
                }
                // IDs alone are readable, but not safe exchange payloads.
                var ids = _vc.cardExchangeList;
                if (ids != null)
                    for (int i = 0; i < ids.Count; i++)
                    {
                        _cardIds.Add(ids[i]);
                        _cardKirarities.Add(0);
                        _hasCardIdentity.Add(false);
                    }
            }
            catch (Exception ex)
            {
                DebugLogger.Log(LogCategory.Handler, "TicketExchange",
                    $"RefreshCardList error: {ex.Message}");
            }
        }

        #endregion

        #region Input Processing

        private void ProcessInput()
        {
            // Left / Right — navigate cards
            if (InputManager.TryConsumeKeyDown(KeyCode.LeftArrow))
            {
                NavigateBy(-1);
                return;
            }
            if (InputManager.TryConsumeKeyDown(KeyCode.RightArrow))
            {
                NavigateBy(1);
                return;
            }

            // Up / Down — page jump (10 cards)
            if (InputManager.TryConsumeKeyDown(KeyCode.UpArrow))
            {
                NavigateBy(-10);
                return;
            }
            if (InputManager.TryConsumeKeyDown(KeyCode.DownArrow))
            {
                NavigateBy(10);
                return;
            }

            // Home / End
            if (InputManager.TryConsumeKeyDown(KeyCode.Home))
            {
                if (_cardIds.Count > 0)
                {
                    _focusIndex = 0;
                    AnnounceCurrentCard();
                }
                return;
            }
            if (InputManager.TryConsumeKeyDown(KeyCode.End))
            {
                if (_cardIds.Count > 0)
                {
                    _focusIndex = _cardIds.Count - 1;
                    AnnounceCurrentCard();
                }
                return;
            }

            // Enter — select/add current card
            if (GetModeName() == "RespectOrb" && InputManager.TryConsumeKeyDown(KeyCode.Delete))
            {
                var clear = _vc?.clearButton;
                if (TicketExchangePolicy.IsUsable(clear != null,
                    clear?.gameObject?.activeInHierarchy == true, clear?.IsInteractable() == true))
                {
                    clear.onClick.Invoke();
                    _confirmFocused = false;
                    ScreenReader.Say(Loc.Get("ticket_selection_cleared"));
                }
                return;
            }
            if (InputManager.TryConsumeKeyDown(KeyCode.Return)
                || InputManager.TryConsumeKeyDown(KeyCode.KeypadEnter))
            {
                if (_confirmFocused) ConfirmExchange();
                else SelectCurrentCard();
                return;
            }

            // C or I — verbose card reading
            if (InputManager.TryConsumeKeyDown(KeyCode.C) || InputManager.TryConsumeKeyDown(KeyCode.I))
            {
                AnnounceCurrentCard(verbose: true);
                return;
            }

            // Space — confirm exchange (DecideClicked)
            if (InputManager.TryConsumeKeyDown(KeyCode.Space))
            {
                ConfirmExchange();
                return;
            }

            // G — announce ticket/exchange info
            if (InputManager.TryConsumeKeyDown(KeyCode.G))
            {
                AnnounceExchangeInfo();
                return;
            }

            // Tab exposes the exchange action as an actual keyboard focus stop.
            if (InputManager.TryConsumeKeyDown(KeyCode.Tab))
            {
                _confirmFocused = !_confirmFocused;
                if (_confirmFocused) ScreenReader.Say(Loc.Get("ticket_confirm_focus"));
                else
                {
                    RefreshCardList();
                    if (_cardIds.Count > 0) AnnounceCurrentCard();
                    else ScreenReader.Say(Loc.Get("ticket_no_cards"));
                }
                return;
            }

            // Escape / Backspace — go back
            if (InputManager.TryConsumeKeyDown(KeyCode.Escape)
                || InputManager.TryConsumeKeyDown(KeyCode.Backspace))
            {
                GoBack();
                return;
            }
        }

        #endregion

        #region Navigation

        private void NavigateBy(int delta)
        {
            _confirmFocused = false;
            if (_cardIds.Count == 0)
            {
                ScreenReader.Say(Loc.Get("ticket_no_cards"));
                return;
            }

            _focusIndex += delta;
            if (_focusIndex < 0) _focusIndex = 0;
            if (_focusIndex >= _cardIds.Count) _focusIndex = _cardIds.Count - 1;

            AnnounceCurrentCard();
        }

        #endregion

        #region Card Operations

        private void SelectCurrentCard()
        {
            if (_operationCooldown > 0f) return;
            if (_cardIds.Count == 0 || _focusIndex < 0 || _focusIndex >= _cardIds.Count) return;

            int mrk = _cardIds[_focusIndex];
            long kirarity = (_focusIndex < _cardKirarities.Count) ? _cardKirarities[_focusIndex] : 0L;
            string name = CardFormatter.GetName(mrk);

            // Re-read before native calls; a filter or previous exchange may
            // have replaced the list since this card was announced.
            RefreshCardList();
            if (_focusIndex >= _cardIds.Count || _cardIds[_focusIndex] != mrk
                || _cardKirarities[_focusIndex] != kirarity)
            {
                MelonLoader.MelonLogger.Msg($"[TicketExchange] Stale selection rejected mrk={mrk} kirarity={kirarity}");
                AnnounceCurrentCard();
                return;
            }

            if (_focusIndex >= _hasCardIdentity.Count || !_hasCardIdentity[_focusIndex])
            {
                MelonLoader.MelonLogger.Msg($"[TicketExchange] Selection refused: missing rarity mrk={mrk}");
                ScreenReader.Say(Loc.Get("ticket_identity_unavailable"));
                return;
            }

            try
            {
                // Feed the game the exact card it gave us, Kirarity included.
                // A card built from the mrk alone has Kirarity=0, which the
                // exchange later rejects and the game hard-resets to title.
                var car = new CardAndRarity(mrk, kirarity);

                // Check if addible
                if (_vc.isAddible(car))
                {
                    bool result = _vc.addToRewardView(car);
                    if (result)
                    {
                        ScreenReader.Say(Loc.Get("ticket_selected", name));
                        MelonLoader.MelonLogger.Msg($"[TicketExchange] Selected mrk={mrk} kirarity={kirarity} name={name}");
                        bool insufficient = AnnounceOrbCost(queued: true);
                        if (!insufficient) ScreenReader.SayQueued(Loc.Get("ticket_confirm_hint"));
                        DebugLogger.Log(LogCategory.Handler, "TicketExchange",
                            $"Selected card mrk={mrk} kirarity={kirarity} ({name})");
                    }
                    else
                    {
                        ScreenReader.Say(Loc.Get("ticket_activate_error"));
                    }
                }
                else
                {
                    MelonLoader.MelonLogger.Msg($"[TicketExchange] isAddible=false mrk={mrk} kirarity={kirarity} mode={GetModeName()}");
                    ScreenReader.Say(Loc.Get("ticket_activate_error"));
                    AnnounceOrbCost(queued: true);
                }

                _operationCooldown = OperationCooldownTime;
            }
            catch (Exception ex)
            {
                DebugLogger.Log(LogCategory.Handler, "TicketExchange",
                    $"SelectCurrentCard error: {ex.Message}");
                ScreenReader.Say(Loc.Get("ticket_activate_error"));
            }
        }

        private void ConfirmExchange()
        {
            if (_operationCooldown > 0f) return;

            try
            {
                // The CardGetter has two possible confirm controls. The ticket /
                // dream-ticket / choice flow confirms through decideButton (driven
                // by the game's setDecideButton()); card-trader-style modes use
                // exchangeButton. Whichever the active mode uses, its interactable
                // flag is the game's own "a valid selection can be submitted now"
                // gate. Never bypass it: DecideClicked() with an empty or already-
                // consumed selection hard-errors the game to the title screen.
                var decideBtn = _vc?.decideButton;
                var exchangeBtn = _vc?.exchangeButton;

                var action = TicketExchangePolicy.ChooseAction(
                    decideBtn?.gameObject?.activeInHierarchy == true,
                    decideBtn?.interactable == true,
                    exchangeBtn?.gameObject?.activeInHierarchy == true,
                    exchangeBtn?.interactable == true);

                MelonLoader.MelonLogger.Msg($"[TicketExchange] Confirm mode={GetModeName()} action={action} " +
                    $"decideActive={decideBtn?.gameObject?.activeInHierarchy} decideEnabled={decideBtn?.interactable} " +
                    $"exchangeActive={exchangeBtn?.gameObject?.activeInHierarchy} exchangeEnabled={exchangeBtn?.interactable}");

                switch (action)
                {
                    case TicketExchangeAction.ConfirmViaDecide:
                        decideBtn.onClick.Invoke();
                        DebugLogger.Log(LogCategory.Handler, "TicketExchange",
                            "Confirmed via decideButton.onClick");
                        _awaitingExchangeResult = true;
                        _exchangeDialogSeen = false;
                        _exchangeRefreshAfter = Time.unscaledTime + 0.5f;
                        break;

                    case TicketExchangeAction.InvokeExchangeButton:
                        exchangeBtn.onClick.Invoke();
                        DebugLogger.Log(LogCategory.Handler, "TicketExchange",
                            "Confirmed via exchangeButton");
                        _awaitingExchangeResult = true;
                        _exchangeDialogSeen = false;
                        _exchangeRefreshAfter = Time.unscaledTime + 0.5f;
                        break;

                    default: // Reject
                        DebugLogger.Log(LogCategory.Handler, "TicketExchange",
                            $"Confirm rejected: decide(present={decideBtn != null}, " +
                            $"interactable={decideBtn?.interactable == true}) " +
                            $"exchange(present={exchangeBtn != null}, " +
                            $"interactable={exchangeBtn?.interactable == true})");
                        if (!AnnounceOrbCost(queued: false))
                            ScreenReader.SayQueued(Loc.Get("ticket_nothing_to_exchange"));
                        break;
                }

                _operationCooldown = OperationCooldownTime;
            }
            catch (Exception ex)
            {
                DebugLogger.Log(LogCategory.Handler, "TicketExchange",
                    $"ConfirmExchange error: {ex.Message}");
                ScreenReader.Say(Loc.Get("ticket_activate_error"));
            }
        }

        private void AnnounceExchangeInfo()
        {
            try
            {
                if (GetModeName() == "RespectOrb")
                {
                    AnnounceOrbCost(queued: false);
                    return;
                }
                // Try to read exchange info text
                string info = _vc?.cardExchangeInfo;
                if (!string.IsNullOrEmpty(info))
                {
                    ScreenReader.Say(info);
                    return;
                }

                // Try reading the item number text
                var itemNumText = _vc?.itemNum;
                if (itemNumText != null && !string.IsNullOrEmpty(itemNumText.text))
                {
                    ScreenReader.Say(Loc.Get("ticket_count", itemNumText.text));
                    return;
                }

                // Fallback: just announce card count
                ScreenReader.Say(Loc.Get("ticket_entered", _cardIds.Count));
            }
            catch (Exception ex)
            {
                DebugLogger.Log(LogCategory.Handler, "TicketExchange",
                    $"AnnounceExchangeInfo error: {ex.Message}");
            }
        }

        // Read only: the native confirmation button remains the authority.
        // Do not enable it or submit a transaction when cost exceeds balance.
        private bool AnnounceOrbCost(bool queued)
        {
            if (_vc == null || GetModeName() != "RespectOrb") return false;
            if (_vc.selList?.Count == 0)
            {
                string prompt;
                try
                {
                    int id = _vc.needItemId > 0 ? _vc.needItemId : OrbExchangePolicy.RespectOrbItemId;
                    long owned = Il2CppYgomGame.Utility.ItemUtil.GetNum(id);
                    string name = Il2CppYgomGame.Utility.ItemUtil.GetName(id);
                    if (owned < 0 || string.IsNullOrWhiteSpace(name))
                        throw new InvalidOperationException("Orb balance unavailable");
                    prompt = Loc.Get("ticket_orb_balance", name, owned);
                    MelonLoader.MelonLogger.Msg($"[TicketExchange] Orb balance itemId={id} owned={owned} selected=0");
                }
                catch (Exception ex)
                {
                    prompt = Loc.Get("ticket_orb_balance_unknown");
                    MelonLoader.MelonLogger.Msg($"[TicketExchange] Orb balance UNKNOWN: {ex.Message}");
                }
                if (queued) ScreenReader.SayQueued(prompt); else ScreenReader.Say(prompt);
                return true;
            }
            string text;
            bool insufficient = false;
            try
            {
                string nativeText = ReadOrbRewardCostText();
                int id = _vc.needItemId > 0 ? _vc.needItemId : _vc.itemId;
                if (id <= 0) id = OrbExchangePolicy.RespectOrbItemId;
                int cost;
                long displayedOwned;
                bool parsed = OrbExchangePolicy.TryReadCostText(nativeText, out cost, out displayedOwned);
                MelonLoader.MelonLogger.Msg($"[TicketExchange] Orb raw itemId={_vc.itemId} needItemId={_vc.needItemId} currentCost={_vc.currentCost} costMax={_vc.costMax} costText={nativeText} parsed={parsed}");
                // currentCost/itemId belong to other CardGetter modes and may
                // be unset for RespectOrb. Prefer the visible native cost.
                if (!parsed)
                    throw new InvalidOperationException("Native orb cost text unavailable");
                long owned = displayedOwned >= 0 ? displayedOwned
                    : Il2CppYgomGame.Utility.ItemUtil.GetNum(id);
                long shortfall = OrbExchangePolicy.Shortfall(cost, owned);
                if (shortfall < 0) throw new InvalidOperationException("Orb balance unavailable");
                string name = Il2CppYgomGame.Utility.ItemUtil.GetName(id);
                if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Orb name unavailable");
                text = Loc.Get("ticket_orb_cost", cost, name, owned);
                insufficient = shortfall > 0;
                text += " " + (insufficient
                    ? Loc.Get("ticket_orb_shortfall", shortfall)
                    : Loc.Get("ticket_orb_clear_hint"));
                MelonLoader.MelonLogger.Msg($"[TicketExchange] Orb cost itemId={id} required={cost} owned={owned} shortfall={shortfall} nativeText={_vc.costText?.text}");
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Msg($"[TicketExchange] Orb cost UNKNOWN: {ex.Message}");
                text = Loc.Get("ticket_orb_cost_unknown");
            }
            if (queued) ScreenReader.SayQueued(text); else ScreenReader.Say(text);
            return insufficient;
        }

        private string ReadOrbRewardCostText()
        {
            // Trader modes render costs separately from the selected card.
            // CardGetterItem.costText can be an inactive placeholder (test15).
            var trade = _vc?.rewardItem;
            if (trade?.gameObject?.activeInHierarchy == true)
            {
                string pair = null;
                int count = 0;
                foreach (var icon in trade.GetComponentsInChildren<Il2CppYgomGame.Single.CardTraderInfoItemIcon>(true))
                {
                    if (icon == null || !icon.gameObject.activeInHierarchy) continue;
                    var use = icon.m_ItemUseNumText;
                    var own = icon.m_ItemPossNumText;
                    string required = LabelExtractor.StripRichText(use?.text)?.Trim().TrimEnd('/', '／');
                    string possessed = LabelExtractor.StripRichText(own?.text)?.Trim().TrimStart('/', '／');
                    MelonLoader.MelonLogger.Msg($"[TicketExchange] Orb trader-row use={required} useActive={use?.gameObject?.activeInHierarchy} own={possessed} ownActive={own?.gameObject?.activeInHierarchy}");
                    if (use?.gameObject?.activeInHierarchy != true || own?.gameObject?.activeInHierarchy != true) continue;
                    string text = required + "/" + possessed;
                    if (!OrbExchangePolicy.TryReadCostText(text, out _, out long balance) || balance < 0) continue;
                    pair = text;
                    count++;
                }
                if (count == 1) return pair;
                if (count > 1) throw new InvalidOperationException("Multiple orb cost rows; cannot identify currency safely");
                // Some builds render the ratio in a single native Text instead.
                var pairs = new HashSet<string>();
                foreach (var label in trade.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                {
                    if (label == null || !label.gameObject.activeInHierarchy) continue;
                    string text = LabelExtractor.StripRichText(label.text);
                    MelonLoader.MelonLogger.Msg($"[TicketExchange] Orb trader text {label.gameObject.name}={text}");
                    if (OrbExchangePolicy.TryReadCostText(text, out _, out long balance) && balance >= 0)
                        pairs.Add(text);
                }
                if (pairs.Count == 1) foreach (var pairText in pairs) return pairText;
            }
            // RespectOrb puts the fee on each selected CardGetterItem, not the
            // page-level costText used by other modes. Never scan the choice
            // grid: it contains cards the player has not selected.
            var view = _vc?.rewardView;
            if (view == null || !view.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Selected reward view unavailable");
            string found = null;
            int activeRows = 0;
            foreach (var row in view.GetComponentsInChildren<CardGetterItem>(true))
            {
                if (row == null || !row.gameObject.activeInHierarchy || !row.IsCardActive()) continue;
                activeRows++;
                var label = row.costText;
                string text = LabelExtractor.StripRichText(label?.text);
                MelonLoader.MelonLogger.Msg($"[TicketExchange] Orb selected-row costText={text} active={label?.gameObject?.activeInHierarchy}");
                if (label?.gameObject?.activeInHierarchy != true
                    || !OrbExchangePolicy.TryReadCostText(text, out _, out _))
                    throw new InvalidOperationException("Selected card cost text unavailable");
                found = text;
            }
            if (activeRows != 1)
                throw new InvalidOperationException($"Expected one selected orb card, got {activeRows}");
            return found;
        }

        private void GoBack()
        {
            try
            {
                var namedMgr = Il2CppYgomSystem.UI.ViewControllerManager.namedManager;
                if (namedMgr == null) return;

                Il2CppYgomSystem.UI.ViewControllerManager contentMgr;
                if (!namedMgr.TryGetValue("content", out contentMgr) || contentMgr == null)
                    return;

                var topVc = contentMgr.GetStackTopViewController();
                topVc?.SendBack();
            }
            catch (Exception ex)
            {
                DebugLogger.Log(LogCategory.Handler, "TicketExchange",
                    $"GoBack error: {ex.Message}");
            }
        }

        #endregion

        #region Announcements

        private void AnnounceCurrentCard(bool verbose = false, bool queued = false)
        {
            if (_cardIds.Count == 0)
            {
                ScreenReader.Say(Loc.Get("ticket_no_cards"));
                return;
            }

            if (_focusIndex < 0 || _focusIndex >= _cardIds.Count)
                _focusIndex = 0;

            int mrk = _cardIds[_focusIndex];
            int pos = _focusIndex + 1;
            int total = _cardIds.Count;

            string cardText = verbose ? CardFormatter.FormatVerbose(mrk) : CardFormatter.FormatCompact(mrk);
            string announcement = Loc.Get("ticket_card_position", pos, total, cardText);

            if (queued)
                ScreenReader.SayQueued(announcement);
            else
                ScreenReader.Say(announcement);
        }

        #endregion

        #region Utilities

        // Screen-button browsing can reach the native exchange control too.
        // Route it to its real click event once, before generic pointer fallbacks.
        public static bool TryActivateConfirmControl(GameObject go)
        {
            var vc = TryGetCardGetterVC();
            if (vc == null || go == null) return false;
            foreach (var button in new[] { vc.decideButton, vc.exchangeButton })
            {
                if (button == null) continue;
                if (go != button.gameObject && !go.transform.IsChildOf(button.transform)) continue;
                if (!TicketExchangePolicy.IsUsable(true,
                    button.gameObject.activeInHierarchy, button.interactable))
                {
                    ScreenReader.Say(Loc.Get("ticket_nothing_to_exchange"));
                    return true;
                }
                MelonLoader.MelonLogger.Msg($"[TicketExchange] Native confirm control={button.gameObject.name} mode={vc.Mode}");
                button.onClick.Invoke();
                return true;
            }
            return false;
        }

        private string GetModeName()
        {
            try
            {
                if (_vc == null) return "Unknown";
                return _vc.Mode.ToString();
            }
            catch { return "Unknown"; }
        }

        #endregion
    }
}
