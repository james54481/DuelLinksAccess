using System;
using UnityEngine;
using Il2CppYgomGame.Duel;

namespace DuelLinksAccess
{
    internal sealed class DuelSkillHandler
    {
        private readonly DrawSkillChoice _choice = new();
        private bool _announced;
        private int _focus;
        private float _lastActivation = -10f;
        public bool BlockAutomaticDraw => _choice.BlockDraw;

        public void Reset() { _choice.Reset(); _announced = false; _focus = 0; }

        // Probe every frame before either the auto gesture or direct draw fallback.
        public void Observe(bool eligibleDraw)
        {
            _choice.Observe(eligibleDraw, eligibleDraw ? Available() : false);
            if (!eligibleDraw) { _announced = false; _focus = 0; }
        }

        public bool ProcessInput(bool eligibleDraw, bool canOpenSkill)
        {
            if (eligibleDraw && _choice.BlockDraw)
            {
                if (_choice.Submitted)
                {
                    // Native follow-up dialogs retain their own keys. K reopens
                    // this choice after cancellation; never auto-draw on timeout.
                    if (canOpenSkill && InputManager.TryConsumeKeyDown(KeyCode.K))
                    { _choice.Reopen(); _announced = false; }
                    return false;
                }
                if (!_announced)
                {
                    _announced = true; _focus = 0;
                    ScreenReader.Say(Loc.Get("duel_skill_draw_prompt"));
                    MelonLoader.MelonLogger.Msg("[DuelSkill] automatic draw paused for player choice");
                }
                if (InputManager.TryConsumeKeyDown(KeyCode.UpArrow)
                    || InputManager.TryConsumeKeyDown(KeyCode.DownArrow)
                    || InputManager.TryConsumeKeyDown(KeyCode.Tab))
                { _focus = 1 - _focus; ReadChoice(); }
                else if (InputManager.TryConsumeKeyDown(KeyCode.K))
                { _focus = 0; ReadChoice(); }
                else if (InputManager.TryConsumeKeyDown(KeyCode.Return)
                    || InputManager.TryConsumeKeyDown(KeyCode.KeypadEnter)
                    || InputManager.TryConsumeKeyDown(KeyCode.Space))
                {
                    if (_focus == 1)
                    {
                        _choice.ChooseNormal();
                        ScreenReader.Say(Loc.Get("duel_skill_normal_chosen"));
                        MelonLoader.MelonLogger.Msg("[DuelSkill] player chose normal draw");
                    }
                    else if (TryActivate(true)) _choice.SkillSubmitted();
                }
                else if (InputManager.TryConsumeKeyDown(KeyCode.Escape))
                    ScreenReader.Say(Loc.Get("duel_skill_draw_prompt"));
                return true;
            }

            if (canOpenSkill && InputManager.TryConsumeKeyDown(KeyCode.K))
            { TryActivate(false); return true; }
            return false;
        }

        private void ReadChoice() => ScreenReader.Say(Loc.Get(
            _focus == 0 ? "duel_skill_activate_choice" : "duel_skill_normal_choice"));

        private static bool? Available()
        {
            try
            {
                // Native UI is also positive evidence while an activation
                // sequence is being presented. Neither source is forced on.
                var ui = DuelClient.instance?.duelHUD?.skillActivation;
                if (ui != null && ui.isShowing && ui.gameObject.activeInHierarchy
                    && ui.activateButton != null && ui.activateButton.IsInteractable()) return true;
                int state = Engine.DLL_DuelSkillIsActivatable(DuelState.MyPlayerNum());
                return state < 0 ? (bool?)null : state > 0;
            }
            catch { return null; }
        }

        private bool TryActivate(bool draw)
        {
            if (Time.unscaledTime - _lastActivation < 0.75f) return false;
            try
            {
                if (Available() != true)
                { ScreenReader.Say(Loc.Get("duel_skill_unavailable")); return false; }
                var client = DuelClient.instance;
                var ui = client?.duelHUD?.skillActivation;
                if (ui != null && ui.isShowing && !ui.clicked && ui.gameObject.activeInHierarchy
                    && ui.activateButton != null && ui.activateButton.gameObject.activeInHierarchy
                    && ui.activateButton.IsInteractable())
                {
                    _lastActivation = Time.unscaledTime;
                    ui.OnClicked();
                    MelonLoader.MelonLogger.Msg("[DuelSkill] native SkillActivation.OnClicked");
                    return true;
                }
                if (!draw)
                {
                    var command = client?.worker2d?.emotionalCommand;
                    var go = command?.directSkillButton;
                    var button = go?.GetComponentInChildren<Il2CppYgomSystem.UI.YgomButton>(false);
                    if (go != null && go.activeInHierarchy && button != null && button.IsInteractable())
                    {
                        _lastActivation = Time.unscaledTime;
                        var data = new UnityEngine.EventSystems.PointerEventData(
                            UnityEngine.EventSystems.EventSystem.current);
                        button.OnPointerClick(data);
                        MelonLoader.MelonLogger.Msg("[DuelSkill] native directSkillButton.OnPointerClick");
                        return true;
                    }
                }
                ScreenReader.Say(Loc.Get("duel_skill_not_ready"));
                MelonLoader.MelonLogger.Msg($"[DuelSkill] native button not ready draw={draw}; no activation sent");
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"[DuelSkill] {ex.Message}");
                ScreenReader.Say(Loc.Get("duel_skill_not_ready"));
            }
            return false;
        }
    }
}
