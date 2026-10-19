using System;
using UnityEngine;
using UnityEngine.UI;
using DownloadVC = Il2CppYgomGame.Settings.DownloadSettingsDialogViewController;

namespace DuelLinksAccess
{
    public partial class DialogHandler
    {
        private void ApplyDownloadSettingsItems(GameObject root)
        {
            var vc = root.GetComponent<DownloadVC>();
            if (vc == null || !vc.gameObject.activeInHierarchy) return;
            _items.Clear();

            bool Current() => vc != null && vc.gameObject.activeInHierarchy
                && GetTopVcInstanceId(_activeManager) == root.GetInstanceID();
            void Add(GameObject target, Func<string> label, Action action)
            {
                if (target == null || !target.activeInHierarchy) return;
                _items.Add(new DialogItem
                {
                    Go = root, SettingsTarget = target, Type = ItemType.Button,
                    Label = label(), LiveLabel = label,
                    NativeAction = () =>
                    {
                        if (!Current() || target == null || !target.activeInHierarchy) return;
                        var selectable = target.GetComponent<Selectable>();
                        if (selectable != null && !selectable.IsInteractable()) return;
                        action();
                    }
                });
            }

            if (vc.cardCheckBox != null)
                Add(vc.cardCheckBox.gameObject,
                    () => Loc.Get("settings_toggle_item", Loc.Get("download_hd_images"),
                        Loc.Get(vc.cardCheckBox.isOn ? "settings_on" : "settings_off")),
                    () =>
                    {
                        vc.cardCheckBox.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(
                            UnityEngine.EventSystems.EventSystem.current));
                        AnnounceCurrentItem();
                    });
            if (vc.languageText != null)
                Add(vc.languageText.gameObject,
                    () => Loc.Get("settings_language") + ": " + vc.languageText.text,
                    () => vc.OnLanguage());
            if (vc.voiceText != null && vc.voiceField != null && vc.voiceField.activeInHierarchy)
                Add(vc.voiceText.gameObject,
                    () => Loc.Get("settings_voice_language") + ": " + vc.voiceText.text,
                    () => vc.OnVoice());
            if (vc.bgdlCheckBox != null && vc.bgdlField != null && vc.bgdlField.activeInHierarchy)
                Add(vc.bgdlCheckBox.gameObject,
                    () => Loc.Get("settings_toggle_item", SettingsText(vc.bgdlField),
                        Loc.Get(vc.bgdlCheckBox.isOn ? "settings_on" : "settings_off")),
                    () =>
                    {
                        vc.bgdlCheckBox.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(
                            UnityEngine.EventSystems.EventSystem.current));
                        AnnounceCurrentItem();
                    });
            Add(vc.cancelButton, () => Loc.Get("settings_close"), () => vc.OnBack(vc.cancelButton));
            if (vc.downloadButton != null)
                Add(vc.downloadButton.gameObject,
                    () => SettingsText(vc.downloadButton.gameObject)
                        + (vc.downloadButton.IsInteractable() ? "" : " " + Loc.Get("settings_disabled")),
                    () =>
                    {
                        if (!vc.downloadButton.IsInteractable()) return;
                        vc.downloadButton.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(
                            UnityEngine.EventSystems.EventSystem.current));
                    });
            _focusIndex = 0;
            MelonLoader.MelonLogger.Msg($"[DownloadSettingsAccess] controls={_items.Count}");
        }
    }
}
