using UnityEngine;

namespace DuelLinksAccess
{
    /// <summary>
    /// Main handler for duel accessibility. Coordinates event announcements,
    /// event log browsing, field navigation, and card actions.
    ///
    /// Key bindings during duel:
    ///   Tab / Shift+Tab — Cycle field zones
    ///   Left / Right — Navigate cards within zone
    ///   Enter — Open actions for selected card
    ///   V — Re-read current card (verbose)
    ///   F — Field summary
    ///   P — Advance phase
    ///   I — Status (LP, phase, turn)
    ///   J — Toggle event log browsing
    ///   Space — Retry automatic draw during Draw Phase
    ///   During log browsing:
    ///     Up/Down — Navigate entries (older/newer)
    ///     Tab — Re-read current entry
    ///     Escape or J — Close log
    ///   During action menu:
    ///     Up/Down — Navigate commands
    ///     Enter — Execute command
    ///     Escape — Cancel
    ///
    /// Field navigation is suppressed when a dialog overlay is active
    /// so DialogHandler can process duel dialogs (Yes/No, effect selection).
    /// </summary>
    public class DuelHandler
    {
        #region Fields

        private readonly DuelEventLog _eventLog = new();
        private readonly DuelFieldNavigator _fieldNav = new();
        private readonly AutomaticDrawController _automaticDraw = new();
        private readonly DuelPullDownHandler _duelPullDown = new();
        private readonly DuelSkillHandler _skill = new();
        private bool _wasActive;
        private bool _tutorialArrowAnnounced;
        private bool _tutorialArrowDismissAttempted;

        // Post-duel result screen: DuelEndMessage.OnNextButton() is the OK click
        private bool _duelResultScanned;
        private readonly ResultScanState _resultScan = new();
        private Il2Cpp.DuelEndMessage _duelEndMessage;

        // Duel yes/no dialog (DuelCommonDialog) — tribute summon confirmation, etc.
        private readonly DuelConfirmationState _yesNoState = new();

        // Battle position dialog — ATK or DEF position choice
        private bool _bpDialogAnnounced;

        // Select-effect dialog (SelectEffectDialog) — the tabbed picker shown when
        // a card has multiple effects to choose from. Each effect is a tab; the OK
        // button confirms the selected one. Not a VC/Htjson-stack dialog — it's a
        // MonoBehaviour on the duel worker (worker2d.selEffDialog), same as yes/no.
        private bool _selEffAnnounced;
        private int _selEffIndex;
        private float _selEffCooldown; // Grace period after confirm/cancel
        private int _selEffDialogId = -1;
        private int _selEffPendingFrames;
        private bool _selEffViewingAnnounced;
        private string _selEffTabSignature = "";
        private int _selEffVerifiedIndex = -1;
        private string _selEffVerifiedText = "";

        #endregion

        #region Properties

        /// <summary>
        /// Whether the duel handler is currently active.
        /// True when a duel is in progress OR the screen is classified as Duel.
        /// </summary>
        public bool IsActive => DuelEventAnnouncer.InDuel
            || GameStateTracker.CurrentScreen == GameScreen.Duel;

        public bool IsConfirmationActive { get; private set; }

        #endregion

        #region Constructor

        public DuelHandler()
        {
            DuelEventAnnouncer.OnAnnouncement += OnDuelAnnouncement;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Called every frame by Main.Update().
        /// Processes duel-specific key bindings and manages event log.
        /// </summary>
        public void Update()
        {
            IsConfirmationActive = false;
            if (!IsActive)
            {
                if (_wasActive)
                {
                    _wasActive = false;
                    if (_eventLog.IsBrowsing)
                        _eventLog.StopBrowsing();
                    _eventLog.Clear();
                    _fieldNav.Reset();
                    _automaticDraw.Reset();
                    _skill.Reset();
                    _tutorialArrowAnnounced = false;
                    _tutorialArrowDismissAttempted = false;
                    DuelEventAnnouncer.Reset();
                }
                return;
            }

            if (!_wasActive)
            {
                _wasActive = true;
                _tutorialArrowAnnounced = false;
                _tutorialArrowDismissAttempted = false;
                _duelResultScanned = false;
                _resultScan.Reset();
                _duelEndMessage = null;
                _yesNoState.Reset();
                _selEffAnnounced = false;
                _selEffIndex = 0;
                _selEffCooldown = 0f;
                _eventLog.Clear();
                _fieldNav.Reset();
                _automaticDraw.Reset();
                _skill.Reset();
            }

            bool eligibleSkillDraw = IsAutomaticDrawEligible();
            _skill.Observe(eligibleSkillDraw);

            // Visible yes/no prompts own input even while browsing the event log.
            if (HandleDuelYesNoDialog()) return;

            // Native quantity/option prompts must own keys before log and field navigation.
            if (_duelPullDown.Update()) { IsConfirmationActive = true; return; }

            // Effect selection owns navigation, including its field-view mode.
            if (HandleSelectEffectDialog()) return;

            bool skillInputAllowed = GameStateTracker.CurrentScreen != GameScreen.Dialog
                && !_fieldNav.InEmotionalList && !_fieldNav.InCardSelect
                && !_fieldNav.InActionMenu && !_fieldNav.InTargetMode
                && DuelEventAnnouncer.InDuel && !DuelEventAnnouncer.DuelEnded
                && DuelState.CurrentTurnPlayer == DuelState.MyPlayerNum();
            if (skillInputAllowed && _skill.ProcessInput(eligibleSkillDraw, true))
            { IsConfirmationActive = true; return; }
            if (!_skill.BlockAutomaticDraw)
            {
                UpdateAutomaticDraw();
                CompleteAutomaticDrawPresentation();
            }
            else _automaticDraw.Reset();

            // Log browsing mode takes priority for navigation keys
            if (_eventLog.IsBrowsing)
            {
                ProcessLogBrowsing();
                return;
            }

            // Post-duel result screen takes priority — find DuelEndMessage and
            // call OnNextButton() directly (bypasses TutorialArrow entirely).
            if (DuelEventAnnouncer.DuelEnded)
            {
                HandleDuelResult();
                return;
            }

            // TutorialArrow overlay: two types:
            //   "click-to-continue" — dismissed by clicking anywhere (OnPointerClick)
            //   "pointing" — stays on screen, points at a game element to interact with
            // We detect "pointing" arrows by checking if a dismiss attempt failed.
            if (IsTutorialArrowActive())
            {
                if (!_tutorialArrowAnnounced)
                {
                    _tutorialArrowAnnounced = true;
                    if (_tutorialArrowDismissAttempted)
                    {
                        // Already tried to dismiss once — this is a "pointing" arrow.
                        // Don't re-announce. Let the user interact with the field.
                        ScreenReader.Say(Loc.Get("duel_tutorial_arrow_pointing"));
                    }
                    // "Click-to-continue" arrows are auto-dismissed below —
                    // no need to announce, it just adds noise
                }

                if (!_tutorialArrowDismissAttempted)
                {
                    // Auto-dismiss click-to-continue arrows silently
                    _tutorialArrowDismissAttempted = true;
                    _tutorialArrowAnnounced = false;
                    DismissTutorialOverlay();
                    return;
                }

                // Pointing arrow — Space re-attempts dismiss
                if (InputManager.TryConsumeKeyDown(KeyCode.Space))
                {
                    DismissTutorialOverlay();
                    return;
                }
            }
            else
            {
                // Arrow gone — reset tracking
                if (_tutorialArrowDismissAttempted || _tutorialArrowAnnounced)
                {
                    _tutorialArrowDismissAttempted = false;
                    _tutorialArrowAnnounced = false;
                }
            }

            // Battle position dialog — ATK or DEF position choice during normal summon.
            if (HandleBattlePositionDialog()) return;

            // Multi-effect picker (SelectEffectDialog) — choose which of a card's
            // effects to activate. Tabbed dialog with its own OK/confirm button.

            // Field navigation — most keys suppressed when a dialog overlay is active
            // so DialogHandler can handle duel dialogs (Yes/No, card selection).
            // Tab for zone cycling always works — it doesn't conflict with dialog keys.
            bool dialogActive = GameStateTracker.CurrentScreen
                == GameScreen.Dialog;

            // EmotionalList works regardless of dialog state — both when already
            // active and when first detected. RunList often fires alongside RunDialog,
            // so detection must happen before the dialogActive gate.
            if (_fieldNav.InEmotionalList || _fieldNav.CheckForEmotionalList())
            {
                _fieldNav.ProcessInput();
                return;
            }

            if (!dialogActive)
            {
                // Card selection (tribute/material) takes full priority
                if (_fieldNav.InCardSelect)
                {
                    _fieldNav.ProcessInput();
                    return;
                }

                // Action menu and target selection take full priority when open
                if (_fieldNav.InActionMenu || _fieldNav.InTargetMode)
                {
                    _fieldNav.ProcessInput();
                    return;
                }

                // Field nav handles Tab, Left/Right, Enter, C, F, P, Escape
                if (_fieldNav.ProcessInput()) return;
            }
            else
            {
                if (_fieldNav.InActionMenu)
                    _fieldNav.CancelActionMenu();

                // Allow non-conflicting keys (Tab, P, F, C) even during duel dialogs.
                // Dialog keys (Up/Down/Enter/Escape) stay with DialogHandler.
                if (DuelEventAnnouncer.InDuel)
                    _fieldNav.ProcessNonConflictingInput();
            }

            ProcessDuelKeys();
        }

        #endregion

        #region Private Methods

        private void ProcessLogBrowsing()
        {
            if (InputManager.TryConsumeKeyDownOrRepeat(KeyCode.UpArrow))
            {
                _eventLog.BrowseOlder();
                return;
            }

            if (InputManager.TryConsumeKeyDownOrRepeat(KeyCode.DownArrow))
            {
                _eventLog.BrowseNewer();
                return;
            }

            if (InputManager.TryConsumeKeyDown(KeyCode.Escape)
                || InputManager.TryConsumeKeyDown(KeyCode.J))
            {
                _eventLog.StopBrowsing();
                return;
            }

            if (InputManager.TryConsumeKeyDown(KeyCode.Tab))
            {
                _eventLog.ReadCurrent();
                return;
            }
        }

        private void ProcessDuelKeys()
        {
            if (!_skill.BlockAutomaticDraw && IsAutomaticDrawEligible()
                && InputManager.TryConsumeKeyDown(KeyCode.Space))
            {
                try
                {
                    _automaticDraw.Retry(true, true, true,
                        () => DispatchDrawCommand("Space retry"));
                }
                catch (System.Exception ex)
                {
                    DebugLogger.Log(LogCategory.Game, "DuelDraw",
                        $"Space retry failed: {ex.Message}");
                }
                return;
            }

            // I = Status report (LP, phase, turn) — was S, now S is spell zone hotkey
            if (InputManager.TryConsumeKeyDown(KeyCode.I))
            {
                string status = DuelEventAnnouncer.GetStatusText();
                ScreenReader.Say(status);
                return;
            }

            // J = Open event log — was L, now L is LP hotkey
            if (InputManager.TryConsumeKeyDown(KeyCode.J))
            {
                _eventLog.StartBrowsing();
                return;
            }

            // Space (no prompt claimed it above, engine input window CLOSED)
            // = animation/resolution playback is running. Sighted players
            // tap the screen to fast-forward these while the PvP clock
            // drains; keyboard users had no equivalent (2026-07-17 tester
            // report). Send one real OS-level click at screen center — the
            // skip checks are native touch polls, so only a hardware click
            // registers. With the input window closed a tap cannot take
            // game actions (TapCard no-ops there, 2026-07-16 spell log),
            // so a mistimed press is a no-op. Repeat presses = repeat taps.
            if (InputManager.TryConsumeKeyDown(KeyCode.Space)
                && DuelEventAnnouncer.InDuel
                && !DuelEventAnnouncer.DuelEnded
                && DuelState.InputType
                    == Il2CppYgomGame.Duel.Engine.MenuActType.Null)
            {
                var center = new Vector2(
                    Screen.width * 0.5f, Screen.height * 0.5f);
                bool sent = Main.ClickViaHardwareMouse(center, "anim skip");
                DebugLogger.Log(LogCategory.Game, "DuelHandler",
                    $"Space animation-skip click sent={sent}");
                return;
            }
        }

        private void UpdateAutomaticDraw()
        {
            bool duelActive = DuelEventAnnouncer.InDuel
                && !DuelEventAnnouncer.DuelEnded;
            bool localTurn = duelActive
                && DuelState.CurrentTurnPlayer == DuelState.MyPlayerNum();
            bool drawPhase = DuelState.InputType
                == Il2CppYgomGame.Duel.Engine.MenuActType.DrawPhase;

            try
            {
                _automaticDraw.Update(duelActive, localTurn, drawPhase,
                    Time.unscaledTime,
                    () => DispatchDrawCommand("fallback"));
            }
            catch (System.Exception ex)
            {
                DebugLogger.Log(LogCategory.Game, "DuelDraw",
                    $"Automatic draw failed: {ex.Message}");
            }
        }

        private static bool IsAutomaticDrawEligible()
        {
            return DuelEventAnnouncer.InDuel
                && !DuelEventAnnouncer.DuelEnded
                && DuelState.CurrentTurnPlayer == DuelState.MyPlayerNum()
                && DuelState.InputType
                    == Il2CppYgomGame.Duel.Engine.MenuActType.DrawPhase;
        }

        private static void DispatchDrawCommand(string source)
        {
            int player = DuelState.MyPlayerNum();
            DebugLogger.Log(LogCategory.Game, "DuelDraw",
                $"Dispatching {source} draw command for player {player}");
            Il2CppYgomGame.Duel.Engine.DLL_DuelComDoCommand(
                player,
                15,
                0,
                (int)Il2CppYgomGame.Duel.Engine.CommandType.Draw);
        }

        private void CompleteAutomaticDrawPresentation()
        {
            Il2CppYgomGame.Duel.DrawOperationMultiDraw operation = null;
            bool ready = false;

            try
            {
                operation = Il2CppYgomGame.Duel.DuelClient.instance?
                    .worker3d?.drawOperation?
                    .TryCast<Il2CppYgomGame.Duel.DrawOperationMultiDraw>();
                ready = operation != null
                    && operation.step
                        == Il2CppYgomGame.Duel.DrawOperationMultiDraw.Step.Touchable
                    && (operation.phase
                            == Il2CppYgomGame.Duel.DrawOperationMultiDraw.TouchPhase.Neutral
                        || operation.phase
                            == Il2CppYgomGame.Duel.DrawOperationMultiDraw.TouchPhase.Touching)
                    && operation.deckPlace != null;
            }
            catch (System.Exception ex)
            {
                DebugLogger.Log(LogCategory.Game, "DuelDraw",
                    $"Draw operation probe failed: {ex.Message}");
            }

            try
            {
                _automaticDraw.CompleteGesture(ready, Time.unscaledTime,
                    () => SwipeDrawCard(operation));
            }
            catch (System.Exception ex)
            {
                DebugLogger.Log(LogCategory.Game, "DuelDraw",
                    $"Automatic draw gesture failed: {ex.Message}");
            }

            bool detailReady = operation != null
                && operation.step
                    == Il2CppYgomGame.Duel.DrawOperationMultiDraw.Step.Touchable
                && operation.phase
                    == Il2CppYgomGame.Duel.DrawOperationMultiDraw.TouchPhase.WaitDetail;
            try
            {
                _automaticDraw.CompleteDetail(detailReady, () =>
                {
                    operation.time = Il2CppYgomGame.Duel.DrawOperationMultiDraw
                        .waitDetailPhaseTimeLimit;
                    DebugLogger.Log(LogCategory.Game, "DuelDraw",
                        "Advanced automatic draw card detail");
                });
            }
            catch (System.Exception ex)
            {
                DebugLogger.Log(LogCategory.Game, "DuelDraw",
                    $"Automatic draw detail advance failed: {ex.Message}");
            }
        }

        private static void SwipeDrawCard(
            Il2CppYgomGame.Duel.DrawOperationMultiDraw operation)
        {
            var start = new Vector2(
                Screen.width * 0.85f, Screen.height * 0.25f);

            var end = new Vector2(Screen.width / 2f, Screen.height / 2f);

            var eventData = new UnityEngine.EventSystems.PointerEventData(
                UnityEngine.EventSystems.EventSystem.current)
            {
                button = UnityEngine.EventSystems.PointerEventData.InputButton.Left,
                pointerId = -1,
                position = start,
                pressPosition = start
            };

            operation.OnTapDownDrawCard(eventData);
            eventData.delta = end - eventData.position;
            eventData.position = end;
            operation.OnTapUpDrawCard(eventData);

            DebugLogger.Log(LogCategory.Game, "DuelDraw",
                $"Swiped draw card from ({start.x:F0},{start.y:F0}) " +
                $"to ({end.x:F0},{end.y:F0})");
        }

        private void OnDuelAnnouncement(string message, bool queued)
        {
            _eventLog.Add(message);

            // Don't interrupt log browsing with live announcements
            if (_eventLog.IsBrowsing) return;

            if (queued)
                ScreenReader.SayQueued(message);
            else
                ScreenReader.Say(message);
        }

        /// <summary>
        /// Checks if TutorialArrow is the top VC on the dialog stack.
        /// </summary>
        private static bool IsTutorialArrowActive()
        {
            try
            {
                var namedManager = Il2CppYgomSystem.UI.ViewControllerManager.namedManager;
                if (namedManager == null) return false;

                Il2CppYgomSystem.UI.ViewControllerManager mgr;
                if (!namedManager.TryGetValue("dialog", out mgr)) return false;

                var topVc = mgr?.GetStackTopViewController();
                if (topVc?.gameObject == null) return false;

                return topVc.gameObject.name == "TutorialArrow";
            }
            catch { return false; }
        }

        /// <summary>
        /// Dismisses the TutorialArrow by calling OnPointerClick directly on
        /// the TutorialArrowViewController. Uses screen center as click position.
        /// </summary>
        private static void DismissTutorialOverlay()
        {
            try
            {
                var namedManager = Il2CppYgomSystem.UI.ViewControllerManager.namedManager;
                if (namedManager == null) return;

                Il2CppYgomSystem.UI.ViewControllerManager mgr;
                if (!namedManager.TryGetValue("dialog", out mgr)) return;

                var topVc = mgr?.GetStackTopViewController();
                if (topVc?.gameObject == null) return;

                var arrowVc = topVc.TryCast<Il2CppYgomGame.Menu.TutorialArrowViewController>();
                if (arrowVc == null)
                {
                    DebugLogger.Log(LogCategory.Game, "DuelHandler",
                        "TutorialArrow cast failed, falling back to ExecuteEvents");
                    var fallbackData = new UnityEngine.EventSystems.PointerEventData(
                        UnityEngine.EventSystems.EventSystem.current);
                    UnityEngine.EventSystems.ExecuteEvents.Execute(
                        topVc.gameObject, fallbackData,
                        UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                    return;
                }

                // Use physicTarget position for pointing arrows, screen center otherwise
                var eventData = new UnityEngine.EventSystems.PointerEventData(
                    UnityEngine.EventSystems.EventSystem.current);

                var physicTarget = arrowVc.physicTarget;
                if (physicTarget != null)
                {
                    var cam = arrowVc.targetCamera;
                    if (cam == null) cam = Camera.main;

                    if (cam != null)
                    {
                        Vector3 sp = cam.WorldToScreenPoint(
                            physicTarget.transform.position);
                        eventData.position = new Vector2(sp.x, sp.y);
                        DebugLogger.Log(LogCategory.Game, "DuelHandler",
                            $"Clicking arrow at physicTarget ({sp.x:F0}, {sp.y:F0}) via {cam.name}");
                    }
                    else
                    {
                        eventData.position = new Vector2(
                            Screen.width / 2f, Screen.height / 2f);
                        DebugLogger.Log(LogCategory.Game, "DuelHandler",
                            "No camera for physicTarget, using screen center");
                    }
                }
                else
                {
                    eventData.position = new Vector2(
                        Screen.width / 2f, Screen.height / 2f);
                    DebugLogger.Log(LogCategory.Game, "DuelHandler",
                        "Clicking arrow at screen center (no physicTarget)");
                }

                arrowVc.OnPointerClick(eventData);
            }
            catch (System.Exception ex)
            {
                DebugLogger.Log(LogCategory.Game, "DuelHandler",
                    $"DismissTutorialOverlay error: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks for and handles the duel yes/no dialog (DuelCommonDialog).
        /// This dialog appears for tribute summon confirmation, effect activation prompts, etc.
        /// It's a MonoBehaviour on the duel HUD, not a standard VC-based dialog.
        /// Returns true if a yes/no dialog is active and input was consumed.
        /// </summary>
        private bool HandleDuelYesNoDialog()
        {
            // Use IsActive (InDuel OR screen==Duel) instead of just InDuel,
            // because resumed duels never fire DuelStart so InDuel stays false.
            if (!IsActive) return false;

            try
            {
                var client = Il2CppYgomGame.Duel.DuelClient.instance;
                var worker = client?.worker2d;
                var dlg = worker?.yesnoDialog;

                string text = "";
                try { text = SpeechTextFormatter.StripRichText(dlg?.dlgText?.text ?? ""); }
                catch { }

                bool visible = dlg != null && dlg.content != null
                    && dlg.content.activeInHierarchy;
                if (!_yesNoState.Observe(visible, text, Time.unscaledTime, out bool announce))
                    return false;

                IsConfirmationActive = true;

                if (announce)
                {
                    ScreenReader.Say(Loc.Get("duel_yesno_prompt", text));
                    MelonLoader.MelonLogger.Msg($"[DuelConfirm] Visible prompt: {text}");
                }

                // Keep modal ownership during callback grace time. Do not let
                // Enter/Escape fall through to phase or card actions underneath.
                if (!_yesNoState.CanRespond(Time.unscaledTime))
                {
                    InputManager.TryConsumeKeyDown(KeyCode.Return);
                    InputManager.TryConsumeKeyDown(KeyCode.KeypadEnter);
                    InputManager.TryConsumeKeyDown(KeyCode.Space);
                    InputManager.TryConsumeKeyDown(KeyCode.Escape);
                    return true;
                }

                if (InputManager.TryConsumeKeyDown(KeyCode.Tab))
                    ScreenReader.Say(Loc.Get("duel_yesno_prompt", text));
                // Enter/Space = Yes
                if (InputManager.TryConsumeKeyDown(KeyCode.Return)
                    || InputManager.TryConsumeKeyDown(KeyCode.KeypadEnter)
                    || InputManager.TryConsumeKeyDown(KeyCode.Space))
                {
                    DebugLogger.Log(LogCategory.Game, "DuelHandler",
                        "DuelCommonDialog: calling OnButton(0) (Yes)");
                    MelonLoader.MelonLogger.Msg("[DuelConfirm] User chose Yes (0)");
                    dlg.OnButton(0);
                    ScreenReader.Say(Loc.Get("duel_yes"));
                    _yesNoState.Responded(Time.unscaledTime);
                    return true;
                }

                // Escape = No
                if (InputManager.TryConsumeKeyDown(KeyCode.Escape))
                {
                    DebugLogger.Log(LogCategory.Game, "DuelHandler",
                        "DuelCommonDialog: calling OnButton(1) (No)");
                    MelonLoader.MelonLogger.Msg("[DuelConfirm] User chose No (1)");
                    dlg.OnButton(1);
                    ScreenReader.Say(Loc.Get("duel_no"));
                    _yesNoState.Responded(Time.unscaledTime);
                    return true;
                }

                // Consume other keys while dialog is active
                return true;
            }
            catch (System.Exception ex)
            {
                DebugLogger.Log(LogCategory.Game, "DuelHandler",
                    $"DuelYesNo error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handles the battle position dialog — choose ATK or DEF position during summon.
        /// The game shows BattlePositionDialog (via worker2d.bpDialog) when summoning a
        /// monster that can be placed in either position.
        /// Enter/1 = ATK position, 2 = DEF position.
        /// </summary>
        private bool HandleBattlePositionDialog()
        {
            if (!IsActive) return false;

            try
            {
                var client = Il2CppYgomGame.Duel.DuelClient.instance;
                var worker = client?.worker2d;
                var bpDlg = worker?.bpDialog;

                if (bpDlg == null) { _bpDialogAnnounced = false; return false; }

                // Check if dialog's content is active AND has a valid card
                // (cardId > 0 distinguishes an active prompt from a dormant component)
                var content = bpDlg.content;
                if (content == null || !content.activeInHierarchy)
                {
                    _bpDialogAnnounced = false;
                    return false;
                }

                int cardId = 0;
                try { cardId = bpDlg.cardId; } catch { }
                if (cardId <= 0)
                {
                    _bpDialogAnnounced = false;
                    return false;
                }

                // Announce the dialog
                if (!_bpDialogAnnounced)
                {
                    _bpDialogAnnounced = true;
                    ScreenReader.Say(Loc.Get("duel_battle_position"));
                }

                // Enter or 1 = ATK position
                if (InputManager.TryConsumeKeyDown(KeyCode.Return)
                    || InputManager.TryConsumeKeyDown(KeyCode.Alpha1))
                {
                    bpDlg.OnClickCard(1); // left=1 = left card = ATK
                    bpDlg.OnConfirm();
                    ScreenReader.Say(Loc.Get("duel_atk_position"));
                    _bpDialogAnnounced = false;
                    return true;
                }

                // 2 = DEF position
                if (InputManager.TryConsumeKeyDown(KeyCode.Alpha2))
                {
                    bpDlg.OnClickCard(0); // left=0 = right card = DEF
                    bpDlg.OnConfirm();
                    ScreenReader.Say(Loc.Get("duel_def_position"));
                    _bpDialogAnnounced = false;
                    return true;
                }

                // Escape = back/cancel
                if (InputManager.TryConsumeKeyDown(KeyCode.Escape))
                {
                    bpDlg.OnBack();
                    _bpDialogAnnounced = false;
                    return true;
                }

                // Consume keys while dialog is active
                return true;
            }
            catch (System.Exception ex)
            {
                DebugLogger.Log(LogCategory.Game, "DuelHandler",
                    $"BattlePosition error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handles the multi-effect picker (SelectEffectDialog) — shown when a card
        /// has more than one effect and the player must choose which to activate
        /// (e.g. a monster with two optional effects). It's a tabbed MonoBehaviour
        /// on worker2d.selEffDialog, not a VC/Htjson-stack dialog: each effect is a
        /// tab under tabParent, effectText shows the selected effect's description,
        /// and okButton confirms. Up/Down move between effects (firing each tab's
        /// own toggle so the game updates selectedIdx + effectText), Enter/Space
        /// confirm via OnClickOk, Escape cancels via OnClickCancel. Returns true
        /// when the dialog is active and input was consumed.
        /// </summary>
        private bool HandleSelectEffectDialog()
        {
            if (!IsActive) return false;

            try
            {
                var client = Il2CppYgomGame.Duel.DuelClient.instance;
                var worker = client?.worker2d;
                var dlg = worker?.selEffDialog;

                var content = dlg?.content;
                if (dlg == null || !dlg.gameObject.activeInHierarchy)
                {
                    ResetEffectChoice();
                    return false;
                }

                if (dlg.viewing)
                {
                    IsConfirmationActive = true;
                    if (!_selEffViewingAnnounced)
                    {
                        _selEffViewingAnnounced = true;
                        ScreenReader.Say(Loc.Get("duel_effect_field_return"));
                        MelonLoader.MelonLogger.Msg("[EffectChoice] field view active");
                    }
                    if (InputManager.TryConsumeKeyDown(KeyCode.Return)
                        || InputManager.TryConsumeKeyDown(KeyCode.KeypadEnter)
                        || InputManager.TryConsumeKeyDown(KeyCode.Space)
                        || InputManager.TryConsumeKeyDown(KeyCode.Escape))
                    {
                        dlg.OnBack();
                        _selEffAnnounced = false;
                        _selEffPendingFrames = 0;
                        MelonLoader.MelonLogger.Msg("[EffectChoice] requested OnBack from field view");
                    }
                    return true;
                }
                _selEffViewingAnnounced = false;
                if (content == null || !content.activeInHierarchy)
                {
                    ResetEffectChoice();
                    return false;
                }
                IsConfirmationActive = true;
                if (_selEffCooldown > 0f)
                {
                    _selEffCooldown -= Time.deltaTime;
                    return true;
                }

                var tabs = GetEffectTabs(dlg);
                int count = tabs.Count;
                string signature = string.Join(",", tabs.ConvertAll(t => t.GetInstanceID().ToString()));
                if (_selEffDialogId != dlg.GetInstanceID() || signature != _selEffTabSignature)
                {
                    _selEffAnnounced = false;
                    _selEffPendingFrames = 0;
                    _selEffVerifiedIndex = -1;
                    _selEffDialogId = dlg.GetInstanceID();
                    _selEffTabSignature = signature;
                }

                // First detection — announce and dump the structure so the first
                // live encounter either works or reveals the exact tab layout.
                if (!_selEffAnnounced)
                {
                    _selEffAnnounced = true;
                    int sel = SafeSelectedIdx(dlg);
                    _selEffIndex = sel >= 0 && sel < count ? sel : -1;
                    _selEffVerifiedIndex = _selEffIndex;

                    DumpSelectEffectState(dlg, tabs);

                    string info = StripText(dlg.infoText);
                    string eff = StripText(dlg.effectText);
                    _selEffVerifiedText = eff;
                    if (count > 1)
                        ScreenReader.Say(Loc.Get("duel_effect_intro",
                            info, _selEffIndex + 1, count, eff));
                    else if (count == 1)
                        ScreenReader.Say(Loc.Get("duel_effect_single", info, eff));
                    else
                        ScreenReader.Say(Loc.Get("duel_effect_unavailable"));
                    MelonLoader.MelonLogger.Msg($"[EffectChoice] open tabs={count} selected={sel} text={eff}");
                }

                // Native tabs may apply their change on the next Update. Until the
                // readback matches, Enter must never confirm the old/default effect.
                if (_selEffPendingFrames > 0)
                {
                    _selEffPendingFrames--;
                    int actual = SafeSelectedIdx(dlg);
                    if (actual == _selEffIndex && !string.IsNullOrWhiteSpace(StripText(dlg.effectText)))
                    {
                        _selEffPendingFrames = 0;
                        _selEffVerifiedIndex = actual;
                        _selEffVerifiedText = StripText(dlg.effectText);
                        AnnounceEffect(dlg, tabs);
                        MelonLoader.MelonLogger.Msg($"[EffectChoice] selected requested={_selEffIndex} actual={actual} text={StripText(dlg.effectText)}");
                    }
                    else if (_selEffPendingFrames == 0)
                    {
                        ScreenReader.Say(Loc.Get("duel_effect_mismatch"));
                        MelonLoader.MelonLogger.Msg($"[EffectChoice] selection failed requested={_selEffIndex} actual={actual}");
                    }
                }

                // Up = previous effect
                if (count > 1
                    && InputManager.TryConsumeKeyDownOrRepeat(KeyCode.UpArrow))
                {
                    RequestEffectTab(dlg, tabs, -1);
                    return true;
                }

                // Down = next effect
                if (count > 1
                    && InputManager.TryConsumeKeyDownOrRepeat(KeyCode.DownArrow))
                {
                    RequestEffectTab(dlg, tabs, 1);
                    return true;
                }

                // Enter/Space = confirm the selected effect
                if (InputManager.TryConsumeKeyDown(KeyCode.Return)
                    || InputManager.TryConsumeKeyDown(KeyCode.KeypadEnter)
                    || InputManager.TryConsumeKeyDown(KeyCode.Space))
                {
                    var ok = dlg.okButton;
                    var ok2 = dlg.okButton2;
                    bool enabled = (ok != null && ok.gameObject.activeInHierarchy && ok.interactable)
                        || (ok2 != null && ok2.gameObject.activeInHierarchy && ok2.interactable);
                    int actual = SafeSelectedIdx(dlg);
                    if (!EffectChoicePolicy.CanConfirm(count, _selEffIndex, actual,
                        EffectChoicePolicy.DescriptionMatches(_selEffVerifiedText, StripText(dlg.effectText)), enabled,
                        _selEffPendingFrames > 0, dlg.viewing, _selEffVerifiedIndex == _selEffIndex))
                    {
                        ScreenReader.Say(Loc.Get("duel_effect_mismatch"));
                        MelonLoader.MelonLogger.Msg($"[EffectChoice] confirm blocked tabs={count} requested={_selEffIndex} actual={actual} enabled={enabled}");
                        return true;
                    }
                    MelonLoader.MelonLogger.Msg($"[EffectChoice] confirm selected={actual} text={StripText(dlg.effectText)}");
                    DebugLogger.Log(LogCategory.Game, "DuelHandler",
                        $"SelectEffectDialog: OnClickOk() selectedIdx={SafeSelectedIdx(dlg)}");
                    dlg.OnClickOk();
                    ScreenReader.Say(Loc.Get("duel_effect_confirmed"));
                    _selEffAnnounced = false;
                    _selEffIndex = 0;
                    _selEffCooldown = 0.5f;
                    return true;
                }

                // Escape = cancel (game ignores it when the choice is mandatory)
                if (InputManager.TryConsumeKeyDown(KeyCode.Escape))
                {
                    DebugLogger.Log(LogCategory.Game, "DuelHandler",
                        "SelectEffectDialog: OnClickCancel()");
                    dlg.OnClickCancel();
                    ScreenReader.Say(Loc.Get("duel_effect_cancelled"));
                    _selEffAnnounced = false;
                    _selEffIndex = 0;
                    _selEffCooldown = 0.5f;
                    return true;
                }

                // V = re-read the current effect
                if (InputManager.TryConsumeKeyDown(KeyCode.V))
                {
                    AnnounceEffect(dlg, tabs);
                    return true;
                }

                // Consume other keys while the dialog is up.
                return true;
            }
            catch (System.Exception ex)
            {
                MelonLoader.MelonLogger.Msg($"[EffectChoice] error: {ex.Message}");
                // Do not send the same key into field navigation after a picker error.
                return IsConfirmationActive;
            }
        }

        private void ResetEffectChoice()
        {
            _selEffAnnounced = false;
            _selEffIndex = -1;
            _selEffDialogId = -1;
            _selEffPendingFrames = 0;
            _selEffVerifiedIndex = -1;
            _selEffVerifiedText = "";
            _selEffViewingAnnounced = false;
            _selEffTabSignature = "";
            _selEffCooldown = 0f;
        }

        private void RequestEffectTab(Il2CppYgomGame.Duel.SelectEffectDialog dlg,
            System.Collections.Generic.List<GameObject> tabs, int direction)
        {
            int target = EffectChoicePolicy.Move(tabs.Count, SafeSelectedIdx(dlg), direction);
            _selEffIndex = target;
            _selEffVerifiedIndex = -1;
            _selEffVerifiedText = "";
            _selEffPendingFrames = 0;
            if (target < 0 || !SelectEffectTab(tabs, target))
            {
                ScreenReader.Say(Loc.Get("duel_effect_mismatch"));
                return;
            }
            _selEffPendingFrames = 30;
            MelonLoader.MelonLogger.Msg($"[EffectChoice] request target={target} tab={tabs[target].name}");
        }

        /// <summary>Announces the currently focused effect (number + description).</summary>
        private void AnnounceEffect(Il2CppYgomGame.Duel.SelectEffectDialog dlg,
            System.Collections.Generic.List<GameObject> tabs)
        {
            if (_selEffPendingFrames > 0 || SafeSelectedIdx(dlg) != _selEffIndex)
            {
                ScreenReader.Say(Loc.Get("duel_effect_mismatch"));
                return;
            }
            string eff = StripText(dlg.effectText);
            // Fall back to the tab's own label when effectText is empty.
            if (string.IsNullOrWhiteSpace(eff)
                && _selEffIndex >= 0 && _selEffIndex < tabs.Count)
                eff = LabelExtractor.GetLabel(tabs[_selEffIndex]);
            ScreenReader.Say(Loc.Get("duel_effect_item",
                _selEffIndex + 1, tabs.Count, eff));
        }

        /// <summary>
        /// Collects native effect tabs, including nested layout containers.
        /// Native selectedIdx readback must match before confirmation.
        /// </summary>
        private static System.Collections.Generic.List<GameObject> GetEffectTabs(
            Il2CppYgomGame.Duel.SelectEffectDialog dlg)
        {
            var list = new System.Collections.Generic.List<GameObject>();
            try
            {
                var parent = dlg.tabParent;
                if (parent == null) return list;
                // The game's tabs need not inherit Unity Button/Toggle and can
                // be nested inside layout containers. Prefer the native control.
                foreach (var tab in parent.GetComponentsInChildren<Il2CppYgomSystem.UI.YgomTabButton>(false))
                {
                    if (tab != null && tab.gameObject.activeInHierarchy)
                        list.Add(tab.gameObject);
                }
                if (list.Count > 0) return list;
                foreach (var toggle in parent.GetComponentsInChildren<UnityEngine.UI.Toggle>(false))
                    if (toggle != null && toggle.gameObject.activeInHierarchy)
                        list.Add(toggle.gameObject);
                if (list.Count > 0) return list;
                foreach (var button in parent.GetComponentsInChildren<UnityEngine.UI.Button>(false))
                    if (button != null && button.gameObject.activeInHierarchy)
                        list.Add(button.gameObject);
            }
            catch { }
            return list;
        }

        /// <summary>
        /// Selects effect tab <paramref name="index"/> by firing its own control,
        /// so the game runs its native selection path (updates selectedIdx +
        /// effectText). Toggles are preferred (tabs are usually radio toggles);
        /// a Button click is the fallback.
        /// </summary>
        private static bool SelectEffectTab(
            System.Collections.Generic.List<GameObject> tabs, int index)
        {
            if (index < 0 || index >= tabs.Count) return false;
            var go = tabs[index];
            if (go == null || !go.activeInHierarchy) return false;
            try
            {
                var eventData = new UnityEngine.EventSystems.PointerEventData(
                    UnityEngine.EventSystems.EventSystem.current);
                var nativeTab = go.GetComponent<Il2CppYgomSystem.UI.YgomTabButton>();
                if (nativeTab != null)
                {
                    nativeTab.OnPointerClick(eventData);
                    return true;
                }
                var toggle = go.GetComponentInChildren<UnityEngine.UI.Toggle>(true);
                if (toggle != null)
                {
                    if (!toggle.IsInteractable()) return false;
                    toggle.OnPointerClick(eventData);
                    return true;
                }

                var btn = go.GetComponentInChildren<UnityEngine.UI.Button>(true);
                if (btn != null && btn.IsInteractable()) { btn.OnPointerClick(eventData); return true; }

                DebugLogger.Log(LogCategory.Game, "DuelHandler",
                    $"SelectEffectTab: no interactive on tab {index} ('{go.name}')");
            }
            catch (System.Exception ex)
            {
                DebugLogger.Log(LogCategory.Game, "DuelHandler",
                    $"SelectEffectTab error: {ex.Message}");
            }
            return false;
        }

        /// <summary>Reads dlg.selectedIdx defensively (-1 on failure).</summary>
        private static int SafeSelectedIdx(Il2CppYgomGame.Duel.SelectEffectDialog dlg)
        {
            try { return dlg.selectedIdx; } catch { return -1; }
        }

        /// <summary>Strips rich text from a Unity Text, empty string on failure.</summary>
        private static string StripText(UnityEngine.UI.Text t)
        {
            try { return SpeechTextFormatter.StripRichText(t?.text ?? ""); }
            catch { return ""; }
        }

        /// <summary>
        /// Diagnostic dump of the SelectEffectDialog structure on first detection.
        /// Debug-log only (no TTS). Reveals the tab layout so the handler can be
        /// corrected if the tab→effect mapping turns out different in-game.
        /// </summary>
        private static void DumpSelectEffectState(
            Il2CppYgomGame.Duel.SelectEffectDialog dlg,
            System.Collections.Generic.List<GameObject> tabs)
        {
            try
            {
                string info = StripText(dlg.infoText);
                string eff = StripText(dlg.effectText);
                int sel = SafeSelectedIdx(dlg);
                int cancel = -1; try { cancel = dlg.cancelIdx; } catch { }
                bool ok1 = false, ok2 = false;
                try { ok1 = dlg.okButton != null && dlg.okButton.interactable; } catch { }
                try { ok2 = dlg.okButton2 != null && dlg.okButton2.interactable; } catch { }
                int tabParentChildren = -1;
                try { tabParentChildren = dlg.tabParent != null ? dlg.tabParent.childCount : -1; } catch { }

                DebugLogger.Log(LogCategory.Game, "DuelHandler",
                    $"[SelEff] info='{info}' effect='{eff}' selectedIdx={sel} "
                    + $"cancelIdx={cancel} tabParentChildren={tabParentChildren} "
                    + $"tabs={tabs.Count} ok1i={ok1} ok2i={ok2}");

                for (int i = 0; i < tabs.Count; i++)
                {
                    var go = tabs[i];
                    string label = "?";
                    try { label = LabelExtractor.GetLabel(go); } catch { }
                    bool hasToggle = false, hasButton = false;
                    try { hasToggle = go.GetComponentInChildren<UnityEngine.UI.Toggle>(true) != null; } catch { }
                    try { hasButton = go.GetComponentInChildren<UnityEngine.UI.Button>(true) != null; } catch { }
                    DebugLogger.Log(LogCategory.Game, "DuelHandler",
                        $"[SelEff]   tab[{i}] name='{go.name}' label='{label}' "
                        + $"toggle={hasToggle} button={hasButton}");
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.Log(LogCategory.Game, "DuelHandler",
                    $"[SelEff] dump error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the post-duel result screen (YOU WIN/LOSE with OK button).
        /// Finds the DuelEndMessage MonoBehaviour and calls OnNextButton() directly —
        /// this is the exact method the game's OK button invokes. It sets
        /// isNextButtonClicked=true, which TaskHUDDuelEnd.WaitWinLoseStep watches for.
        /// </summary>
        private void HandleDuelResult()
        {
            if (!_duelResultScanned)
            {
                if (!_resultScan.TryBegin(Time.unscaledTime)) return;
                _duelResultScanned = true;
                _duelEndMessage = null;

                try
                {
                    // Avoid Unity's stripped generic FindObjectOfType wrapper.
                    // Resources' non-generic native binding also finds inactive
                    // objects/assets, so explicitly accept only a live scene UI.
                    var candidates = Resources.FindObjectsOfTypeAll(
                        Il2CppInterop.Runtime.Il2CppType.Of<Il2Cpp.DuelEndMessage>());
                    foreach (var candidate in candidates)
                    {
                        var message = candidate?.TryCast<Il2Cpp.DuelEndMessage>();
                        if (message == null || message.gameObject == null
                            || !message.gameObject.activeInHierarchy
                            || !message.gameObject.scene.IsValid()
                            || !message.gameObject.scene.isLoaded
                            || message.isNextButtonClicked) continue;
                        // Multiple live result widgets are ambiguous. Do not
                        // announce or activate an arbitrary candidate.
                        if (_duelEndMessage != null)
                        {
                            _duelEndMessage = null;
                            break;
                        }
                        _duelEndMessage = message;
                    }
                    if (_duelEndMessage != null)
                    {
                        var result = _duelEndMessage.resultType;
                        string resultText = result switch
                        {
                            Il2CppYgomGame.Duel.Engine.ResultType.Win => Loc.Get("duel_result_win"),
                            Il2CppYgomGame.Duel.Engine.ResultType.Lose => Loc.Get("duel_result_lose"),
                            Il2CppYgomGame.Duel.Engine.ResultType.Draw => Loc.Get("duel_result_draw"),
                            _ => result.ToString()
                        };
                        ScreenReader.Say(Loc.Get("duel_result_screen", resultText));
                        DebugLogger.Log(LogCategory.Game, "DuelResult",
                            $"DuelEndMessage found, result={result}, nextClicked={_duelEndMessage.isNextButtonClicked}");
                    }
                    else
                    {
                        // Result UI can be created after the duel-end event.
                        _duelResultScanned = false;
                    }
                }
                catch (System.Exception ex)
                {
                    if (_resultScan.Failed(Time.unscaledTime, ex.GetType().FullName + ": " + ex.Message))
                        DebugLogger.Log(LogCategory.Game, "DuelResult",
                            $"Scan error (native result lookup; retry delayed): {ex}");
                    _duelEndMessage = null;
                    _duelResultScanned = false;
                }
            }

            if (_duelEndMessage == null) return;
            if (!_duelEndMessage.gameObject.activeInHierarchy || _duelEndMessage.isNextButtonClicked)
            {
                _duelEndMessage = null;
                _duelResultScanned = false;
                return;
            }

            // Enter/Space = click OK (call OnNextButton directly)
            if (InputManager.TryConsumeKeyDown(KeyCode.Return)
                || InputManager.TryConsumeKeyDown(KeyCode.KeypadEnter)
                || InputManager.TryConsumeKeyDown(KeyCode.Space))
            {
                try
                {
                    DebugLogger.Log(LogCategory.Game, "DuelResult",
                        "Calling DuelEndMessage.OnNextButton()");
                    _duelEndMessage.OnNextButton();
                    ScreenReader.Say("OK");
                    _duelEndMessage = null;
                }
                catch (System.Exception ex)
                {
                    DebugLogger.Log(LogCategory.Game, "DuelResult",
                        $"OnNextButton error: {ex.Message}");
                }
            }
        }

        #endregion
    }
}
