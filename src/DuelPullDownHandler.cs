using System;
using System.Collections.Generic;
using UnityEngine;
using Il2CppYgomGame.Duel;

namespace DuelLinksAccess
{
    // DuelPullDown is owned by worker2d, outside the menu VC dialog stack.
    // Use native entry indices and callbacks, never a number parsed from a label.
    internal sealed class DuelPullDownHandler
    {
        private int _id = -1, _focus, _pending, _requested = -1;
        private string _signature = "";
        private bool _viewing, _submitted;
        private bool _expected, _verified;

        public bool Update()
        {
            DuelPullDown dlg = null;
            try
            {
                dlg = DuelClient.instance?.worker2d?.pullDown;
                if (dlg == null || !dlg.gameObject.activeInHierarchy)
                { Reset(); return false; }
                if (dlg.viewing)
                {
                    if (!_viewing) ScreenReader.Say(Loc.Get("duel_effect_field_return"));
                    _viewing = true;
                    if (ConfirmKey() || InputManager.TryConsumeKeyDown(KeyCode.Escape)) dlg.OnBack();
                    return true;
                }
                _viewing = false;
                if (dlg.content == null || !dlg.content.activeInHierarchy)
                { Reset(); return false; }
                var labels = ReadLabels(dlg);
                string signature = string.Join("\u001f", labels);
                if (_id != dlg.GetInstanceID() || signature != _signature)
                {
                    Reset(); _id = dlg.GetInstanceID(); _signature = signature;
                    _focus = dlg.isSingle && dlg.crntCheck >= 0 && dlg.crntCheck < labels.Count
                        ? dlg.crntCheck : 0;
                    _verified = ReadSelection(dlg, labels.Count) != null;
                    ScreenReader.Say(Loc.Get("duel_choice_intro", Text(dlg.labelText)));
                    Announce(dlg, labels);
                    MelonLoader.MelonLogger.Msg($"[DuelPullDown] open single={dlg.isSingle} current={dlg.crntCheck} labels={signature}");
                }
                if (_submitted) return true;
                if (_pending > 0)
                {
                    _pending--;
                    var selected = ReadSelection(dlg, labels.Count);
                    if (selected != null && _requested >= 0 && _requested < selected.Count
                        && selected[_requested] == _expected
                        && (!dlg.isSingle || dlg.crntCheck == _requested))
                    {
                        _pending = 0; _verified = true; Announce(dlg, labels);
                        MelonLoader.MelonLogger.Msg($"[DuelPullDown] selection readback index={_requested} current={dlg.crntCheck}");
                    }
                    else if (_pending == 0) ScreenReader.Say(Loc.Get("duel_choice_unavailable"));
                    return true;
                }
                if (InputManager.TryConsumeKeyDown(KeyCode.DownArrow))
                { _focus = (_focus + 1) % (labels.Count + 1); Announce(dlg, labels); }
                else if (InputManager.TryConsumeKeyDown(KeyCode.UpArrow))
                { _focus = (_focus + labels.Count) % (labels.Count + 1); Announce(dlg, labels); }
                else if (InputManager.TryConsumeKeyDown(KeyCode.Tab))
                { _focus = _focus == labels.Count ? 0 : labels.Count; Announce(dlg, labels); }
                else if (InputManager.TryConsumeKeyDown(KeyCode.Escape))
                { ScreenReader.Say(Loc.Get("duel_choice_help")); }
                else if (ConfirmKey())
                {
                    if (_focus < labels.Count)
                    {
                        var entry = dlg.entrys?[_focus];
                        if (entry == null || !entry.activeInHierarchy || string.IsNullOrWhiteSpace(labels[_focus]))
                        { ScreenReader.Say(Loc.Get("duel_choice_unavailable")); return true; }
                        var before = ReadSelection(dlg, labels.Count);
                        if (before == null)
                        { ScreenReader.Say(Loc.Get("duel_choice_unavailable")); return true; }
                        _expected = dlg.isSingle || !before[_focus];
                        _verified = false;
                        _requested = _focus; _pending = 30;
                        // false explicitly prevents auto-confirmation after selecting a row.
                        dlg.OnCheck(_focus, false);
                        MelonLoader.MelonLogger.Msg($"[DuelPullDown] OnCheck index={_focus} label={labels[_focus]}");
                    }
                    else
                    {
                        bool enabled = dlg.okButton != null && dlg.okButton.gameObject.activeInHierarchy
                            && dlg.okButton.IsInteractable();
                        var selected = ReadSelection(dlg, labels.Count);
                        bool known = labels.Count > 0 && labels.TrueForAll(s => !string.IsNullOrWhiteSpace(s))
                            && string.Join("\u001f", ReadLabels(dlg)) == _signature;
                        if (_requested >= 0)
                            known &= selected != null && _requested < selected.Count
                                && selected[_requested] == _expected
                                && (!dlg.isSingle || dlg.crntCheck == _requested);
                        if (!DuelPullDownPolicy.CanConfirm(dlg.isSingle, dlg.numSelect, selected,
                            dlg.crntCheck, enabled, _pending > 0 || !_verified, known))
                        { ScreenReader.Say(Loc.Get("duel_choice_unavailable")); return true; }
                        MelonLoader.MelonLogger.Msg($"[DuelPullDown] OnOk current={dlg.crntCheck} selected={string.Join(",", selected)}");
                        _submitted = true;
                        dlg.OnOk();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"[DuelPullDown] {ex.Message}");
                // An active native prompt must not leak Enter/P into field actions.
                return dlg != null && dlg.gameObject.activeInHierarchy;
            }
        }

        private static bool ConfirmKey() => InputManager.TryConsumeKeyDown(KeyCode.Return)
            || InputManager.TryConsumeKeyDown(KeyCode.KeypadEnter)
            || InputManager.TryConsumeKeyDown(KeyCode.Space);

        private void Reset()
        { _id = -1; _focus = 0; _pending = 0; _requested = -1; _signature = ""; _submitted = false; _viewing = false; }

        private static string Text(UnityEngine.UI.Text text) =>
            SpeechTextFormatter.StripRichText(text?.text ?? "");

        private static List<string> ReadLabels(DuelPullDown dlg)
        {
            var labels = new List<string>();
            if (dlg.entrys == null || dlg.maxEntry <= 0 || dlg.entrys.Length < dlg.maxEntry) return labels;
            for (int i = 0; i < dlg.maxEntry; i++)
            {
                var parts = new List<string>();
                var entry = dlg.entrys[i];
                if (entry != null && entry.activeInHierarchy)
                    foreach (var text in entry.GetComponentsInChildren<UnityEngine.UI.Text>(false))
                    {
                        string value = Text(text);
                        if (!string.IsNullOrWhiteSpace(value) && !parts.Contains(value)) parts.Add(value);
                    }
                labels.Add(string.Join(" ", parts));
            }
            return labels;
        }

        private static List<bool> ReadSelection(DuelPullDown dlg, int count)
        {
            if (dlg.isSingle)
            {
                var single = new List<bool>();
                for (int i = 0; i < count; i++) single.Add(i == dlg.crntCheck);
                return single;
            }
            if (dlg.results == null || dlg.results.Length != count) return null;
            var selected = new List<bool>();
            for (int i = 0; i < count; i++) selected.Add(dlg.results[i]);
            return selected;
        }

        private void Announce(DuelPullDown dlg, List<string> labels)
        {
            if (_focus == labels.Count)
            { ScreenReader.Say(Loc.Get("duel_choice_confirm")); return; }
            var selected = ReadSelection(dlg, labels.Count);
            string label = labels[_focus];
            if (string.IsNullOrWhiteSpace(label)) label = Loc.Get("duel_choice_unavailable");
            ScreenReader.Say(Loc.Get("duel_choice_item", _focus + 1, labels.Count, label,
                selected != null && selected[_focus] ? Loc.Get("duel_choice_selected") : Loc.Get("duel_choice_not_selected")));
        }
    }
}
