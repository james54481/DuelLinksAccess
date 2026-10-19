using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SettingsVC = Il2CppYgomGame.Settings.SettingsDialogViewController;

namespace DuelLinksAccess
{
    public partial class DialogHandler
    {
        private SettingsVC _settingsVc;
        private int _settingsTab = -1;

        private static SettingsVC FindSettings(GameObject root)
        {
            if (root == null) return null;
            var vc = root.GetComponent<SettingsVC>();
            return vc != null && vc.gameObject.activeInHierarchy ? vc : null;
        }

        private void PollSettingsPage()
        {
            if (!_scanned || _dialogRoot == null) return;
            var vc = FindSettings(_dialogRoot);
            if (vc == null) { _settingsVc = null; _settingsTab = -1; return; }
            if (vc.currentTab == _settingsTab) return;
            // Works for both native mouse tabs and keyboard tabs.
            _scanned = false; _scanDelay = 0.15f; _scanAttempts = 0;
            _textMode = false; _lastDialogText = "";
        }

        private void ApplySettingsItems(GameObject root)
        {
            _settingsVc = FindSettings(root);
            if (_settingsVc == null) { _settingsTab = -1; return; }
            var vc = _settingsVc;
            _settingsTab = vc.currentTab;
            // The current PC layout uses a native button in the shared download
            // section; the legacy voiceDataButton/lcardDataButton fields are hidden.
            var downloadButtons = FindDownloadSectionButtons(vc, _items);
            _items.Clear();
            var rows = new List<DialogItem>();
            var seen = new HashSet<int>();

            bool Live(GameObject go) => go != null && go.activeInHierarchy;
            bool Current() => vc != null && vc.gameObject.activeInHierarchy
                && GetTopVcInstanceId(_activeManager) == root.GetInstanceID()
                && vc.currentTab == _settingsTab;

            void Add(GameObject target, Func<string> label, Action action, Slider slider = null, bool checkSelectable = true, bool announce = true)
            {
                if (!Live(target) || !seen.Add(target.GetInstanceID())) return;
                rows.Add(new DialogItem
                {
                    Go = root, SettingsTarget = target, Label = label(), LiveLabel = label,
                    Type = slider != null ? ItemType.Slider : ItemType.Button,
                    SliderComponent = slider,
                    NativeAction = () =>
                    {
                        if (!Current() || !Live(target)) return;
                        var selectable = target.GetComponent<Selectable>();
                        if (checkSelectable && selectable != null && !selectable.IsInteractable()) return;
                        action?.Invoke();
                        if (vc != null && vc.gameObject.activeInHierarchy && vc.currentTab == _settingsTab)
                        {
                            if (announce) AnnounceCurrentItem();
                        }
                    }
                });
            }
            void SliderRow(Slider slider, string key)
            {
                if (slider == null) return;
                Add(slider.gameObject, () => Loc.Get(key), () => AnnounceCurrentItem(), slider);
            }
            void AudioRow(Slider slider, Image muteButton, string key, string muteKey, Action mute)
            {
                if (slider == null) return;
                Add(slider.gameObject, () => Loc.Get(key) + ". " + Loc.Get(muteKey), () =>
                {
                    if (muteButton == null || !Live(muteButton.gameObject)) return;
                    if (vc.disabledButton != null && muteButton.sprite == vc.disabledButton) return;
                    var selectable = muteButton.GetComponent<Selectable>();
                    if (selectable != null && !selectable.IsInteractable()) return;
                    mute();
                    ScreenReader.Say(Loc.Get("settings_mute_changed"));
                }, slider, checkSelectable: false, announce: false);
            }
            void ToggleRow(Toggle toggle, string title)
            {
                if (toggle == null) return;
                Add(toggle.gameObject,
                    () => Loc.Get("settings_toggle_item", title,
                        Loc.Get(toggle.isOn ? "settings_on" : "settings_off"))
                        + (toggle.IsInteractable() ? "" : " " + Loc.Get("settings_disabled")),
                    () =>
                    {
                        if (!toggle.IsInteractable()) return;
                        toggle.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(
                            UnityEngine.EventSystems.EventSystem.current));
                    });
            }
            void RadioGroup(Il2CppSystem.Collections.Generic.Dictionary<int, Toggle> group, string key)
            {
                if (group == null) return;
                var choices = new List<Toggle>();
                foreach (var pair in group)
                    if (pair.Value != null && Live(pair.Value.gameObject)) choices.Add(pair.Value);
                choices.Sort((a, b) => SettingsAccessibilityPolicy.ComparePaths(
                    SettingsPath(a.transform, root.transform), SettingsPath(b.transform, root.transform)));
                if (choices.Count == 0) return;
                int pending = choices.FindIndex(toggle => toggle.isOn);
                Func<string> label = () => pending < 0
                    ? Loc.Get(key) + ": " + Loc.Get("settings_unknown_option")
                    : Loc.Get("settings_radio_item", Loc.Get(key), SettingsOptionLabel(choices[pending]),
                        Loc.Get(choices[pending].isOn ? "settings_selected" : "settings_not_selected"))
                        + (choices[pending].IsInteractable() ? "" : " " + Loc.Get("settings_disabled"))
                        + ". " + Loc.Get("settings_choice_help");
                int before = rows.Count;
                Add(choices[0].gameObject, label, () =>
                {
                    if (pending < 0) return;
                    var toggle = choices[pending];
                    if (!Live(toggle.gameObject) || !toggle.IsInteractable() || toggle.isOn
                        || SettingsOptionLabel(toggle) == Loc.Get("settings_unknown_option")) return;
                    toggle.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(
                        UnityEngine.EventSystems.EventSystem.current));
                }, checkSelectable: false);
                if (rows.Count == before) return;
                rows[rows.Count - 1].AdjustChoice = direction =>
                {
                    if (!Current()) return;
                    pending = pending < 0 ? 0 : (pending + direction + choices.Count) % choices.Count;
                    var toggle = choices[pending];
                    RevealSettingsTarget(toggle.gameObject);
                    ScreenReader.Say(SettingsOptionLabel(toggle)
                        + (toggle.IsInteractable() ? "" : " " + Loc.Get("settings_disabled")));
                };
            }
            void Button(Component component, string key, Action action)
            {
                if (component == null) return;
                Add(component.gameObject, () => Loc.Get(key), () =>
                {
                    var image = component.TryCast<Image>();
                    if (image != null && vc.disabledButton != null && image.sprite == vc.disabledButton) return;
                    action();
                });
            }
            void Info(GameObject target, string key)
            {
                if (!Live(target)) return;
                string text = SettingsText(target);
                Add(target, () => Loc.Get(key) + (string.IsNullOrWhiteSpace(text) ? "" : ": " + text),
                    () => { });
            }

            if (Live(vc.content0))
            {
                AudioRow(vc.bgmSlider, vc.bgmButton, "settings_bgm", "settings_bgm_mute", () => vc.OnBgmMute());
                AudioRow(vc.seSlider, vc.seButton, "settings_se", "settings_se_mute", () => vc.OnSeMute());
                AudioRow(vc.voiceSlider, vc.voiceButton, "settings_voice", "settings_voice_mute", () => vc.OnVoiceMute());
                ToggleRow(vc.notifyToggle, Loc.Get("settings_notify"));
                ToggleRow(vc.nightToggle, Loc.Get("settings_night"));
                ToggleRow(vc.loginStatusToggle, Loc.Get("settings_login"));
                ToggleRow(vc.ignoreLimitToggle, Loc.Get("settings_ignore_limit"));
                SliderRow(vc.batterySlider, "settings_performance");
                Info(vc.resolutionField, "settings_resolution");
                if (downloadButtons.Count == 0)
                {
                    Info(vc.cardField, "settings_card_data");
                    Info(vc.languageField, "settings_language");
                    Info(vc.voiceField, "settings_voice_language");
                    Button(vc.voiceDataButton, "settings_download", () => vc.OnVoiceData());
                    Button(vc.lcardDataButton, "settings_card_download", () => vc.OnLCardData());
                }
                foreach (var item in downloadButtons)
                {
                    if (item.Go == null || !seen.Add(item.Go.GetInstanceID())) continue;
                    item.SettingsTarget = item.Go;
                    item.Label = Loc.Get("settings_download");
                    item.LiveLabel = () => Loc.Get("settings_download");
                    rows.Add(item);
                }
                if (vc.localeButton != null)
                    Add(vc.localeButton.gameObject,
                        () => Loc.Get("settings_region") + ": " + (vc.localeText?.text ?? ""),
                        () => vc.OnLocaleChange());
            }
            if (Live(vc.content1))
            {
                RadioGroup(vc.cameraButtonList, "settings_camera");
                RadioGroup(vc.positionButtonList, "settings_position");
                RadioGroup(vc.chainButtonList, "settings_chain");
                RadioGroup(vc.actorderButtonList, "settings_order");
                RadioGroup(vc.manualButtonList, "settings_activation");
                RadioGroup(vc.cardScrollButtonList, "settings_card_list");
                RadioGroup(vc.chrocardButtonList, "settings_chronicle");
                ToggleRow(vc.productionToggle, Loc.Get("settings_3d"));
                ToggleRow(vc.talkToggle, Loc.Get("settings_talk"));
                ToggleRow(vc.emoteToggle, Loc.Get("settings_emote"));
                ToggleRow(vc.serifLogToggle, Loc.Get("settings_serif_log"));
                ToggleRow(vc.unameToggle, Loc.Get("settings_player_name"));
            }

            // The two tabs always precede active-page content.
            for (int index = 0; index < 2; index++)
            {
                int tab = index;
                _items.Add(new DialogItem
                {
                    Go = root, SettingsTarget = vc.tabParent,
                    Type = ItemType.Button,
                    LiveLabel = () => Loc.Get("settings_tab_item",
                        Loc.Get(tab == 0 ? "settings_all_tab" : "settings_duel_tab"),
                        Loc.Get(vc.currentTab == tab ? "settings_selected" : "settings_not_selected")),
                    NativeAction = () =>
                    {
                        if (!Current()) return;
                        vc.OnClickTab(tab);
                        _scanned = false; _scanDelay = 0.15f; _scanAttempts = 0;
                    }
                });
            }
            rows.Sort((left, right) => SettingsAccessibilityPolicy.ComparePaths(
                SettingsPath(left.SettingsTarget.transform, root.transform),
                SettingsPath(right.SettingsTarget.transform, root.transform)));
            _items.AddRange(rows);
            _items.Add(new DialogItem
            {
                Go = root, Type = ItemType.Button, Label = Loc.Get("settings_close"),
                NativeAction = () => { if (Current()) vc.OnOkButton(); }
            });
            _focusIndex = vc.currentTab == 1 ? 1 : 0;
            MelonLoader.MelonLogger.Msg($"[SettingsAccess] tab={vc.currentTab} controls={rows.Count} total={_items.Count}");
        }

        private static List<int> SettingsPath(Transform target, Transform root)
        {
            var path = new List<int>();
            for (var t = target; t != null && t != root; t = t.parent) path.Add(t.GetSiblingIndex());
            path.Reverse(); return path;
        }

        private static List<DialogItem> FindDownloadSectionButtons(SettingsVC vc, List<DialogItem> original)
        {
            var result = new List<DialogItem>();
            if (vc.currentTab != 0 || vc.content0 == null || vc.languageText == null
                || vc.voiceText == null || vc.cardText == null) return result;
            var section = vc.languageText.transform.parent;
            while (section != null && section != vc.content0.transform && section != vc.transform)
            {
                if (vc.voiceText.transform.IsChildOf(section) && vc.cardText.transform.IsChildOf(section))
                {
                    foreach (var item in original)
                    {
                        if (item.Type != ItemType.Button || item.Go == null || !item.Go.activeInHierarchy
                            || !item.Go.transform.IsChildOf(section)) continue;
                        // Retain the real native button and the existing full click
                        // path, rather than calling an unrelated legacy callback.
                        result.Add(item);
                    }
                    break;
                }
                section = section.parent;
            }
            // Graphic scanning can produce several entries for one visual button.
            // Prefer the actual selectable and retain a single navigation stop.
            result.Sort((a, b) => (a.Go.GetComponent<Selectable>() == null ? 1 : 0)
                .CompareTo(b.Go.GetComponent<Selectable>() == null ? 1 : 0));
            if (result.Count > 1) result.RemoveRange(1, result.Count - 1);
            return result;
        }

        private static string SettingsText(GameObject go)
        {
            var parts = new List<string>();
            foreach (var text in go.GetComponentsInChildren<Text>(false))
            {
                string value = SpeechTextFormatter.StripRichText(text.text ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(value) && !parts.Contains(value)) parts.Add(value);
            }
            return string.Join(" ", parts);
        }

        private static string SettingsOptionLabel(Toggle toggle)
        {
            // Radio labels belong to the individual row, not the full group.
            var own = SettingsText(toggle.gameObject);
            if (!string.IsNullOrWhiteSpace(own)) return own;
            var parent = toggle.transform.parent;
            for (int depth = 0; parent != null && depth < 3; depth++, parent = parent.parent)
            {
                var toggles = parent.GetComponentsInChildren<Toggle>(false);
                if (toggles.Length != 1) break;
                string row = SettingsText(parent.gameObject);
                if (!string.IsNullOrWhiteSpace(row)) return row;
            }
            return Loc.Get("settings_unknown_option");
        }

        private static void RevealSettingsTarget(GameObject target)
        {
            try
            {
                var rect = target.GetComponent<RectTransform>();
                var scroll = target.GetComponentInParent<ScrollRect>();
                if (rect == null || scroll == null || scroll.content == null || !scroll.vertical) return;
                var viewport = scroll.viewport ?? scroll.GetComponent<RectTransform>();
                if (viewport == null) return;
                var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, rect);
                float hidden = scroll.content.rect.height - viewport.rect.height;
                if (hidden <= 0) return;
                float delta = bounds.min.y < viewport.rect.yMin ? viewport.rect.yMin - bounds.min.y
                    : bounds.max.y > viewport.rect.yMax ? viewport.rect.yMax - bounds.max.y : 0;
                scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition - delta / hidden);
            }
            catch { }
        }
    }
}
