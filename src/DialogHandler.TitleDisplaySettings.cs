using System;
using Il2CppYgomGame.Settings;
using UnityEngine;

namespace DuelLinksAccess
{
    public partial class DialogHandler
    {
        private void ApplyTimezonePickerLabels(string dialogText)
        {
            if (string.IsNullOrEmpty(dialogText)
                || !dialogText.Contains("Etc/GMT+12", StringComparison.Ordinal)
                || !dialogText.Contains("Pacific/Honolulu", StringComparison.Ordinal))
                return;

            _items.RemoveAll(item => item.Go != null &&
                ((item.Go.name == "PullDownEntry(Clone)" && item.Label == "Button")
                    || (item.Go.name == "PullDown" && item.Label == "OK")));

            foreach (var item in _items)
            {
                if (item.Type != ItemType.Button || item.Go == null) continue;
                if (item.Go.name == "CancelButton" || item.Go.name == "BackButton" || item.Label == "Back")
                    item.Label = Loc.Get("screen_back");
                else if (item.Label == "OK")
                    item.Label = Loc.Get("title_timezone_confirm");
            }
        }

        private void ApplyTitleDisplaySettings(GameObject root)
        {
            var view = root.GetComponent<ScreenSettingsDialogViewController>();
            if (view == null) return;

            var slider = view.screenSizeSlider;
            var fullscreen = view.fullscreenToggle;
            var ratios = view.aspectRatioToggleList;
            _items.RemoveAll(item => item.Go != null &&
                ((slider != null && item.Go.transform.IsChildOf(slider.transform))
                    || (fullscreen != null && item.Go.transform.IsChildOf(fullscreen.transform))
                    || (view.aspectRatioGroup != null
                        && item.Go.transform.IsChildOf(view.aspectRatioGroup.transform))));

            bool IsCurrent() => root != null && GetTopVcInstanceId(_activeManager) == root.GetInstanceID();
            int index = 0;
            if (ratios != null && ratios.Count > 0)
            {
                string RatioLabel()
                {
                    int selected = view.currentAspectRatioIndex;
                    if (selected < 0 || selected >= ratios.Count || ratios[selected] == null) return "";
                    return AccessibleUiText.Read(ratios[selected].gameObject);
                }

                _items.Insert(index++, new DialogItem
                {
                    Go = root,
                    Type = ItemType.Button,
                    LiveLabel = () => Loc.Get("title_display_ratio", RatioLabel()),
                    AdjustChoice = direction =>
                    {
                        if (!IsCurrent()) return;
                        int next = Mathf.Clamp(view.currentAspectRatioIndex + direction, 0, ratios.Count - 1);
                        var toggle = ratios[next];
                        if (toggle != null && toggle.isActiveAndEnabled && toggle.IsInteractable())
                            toggle.isOn = true;
                        ScreenReader.Say(RatioLabel());
                    }
                });
            }

            if (slider != null)
            {
                _items.Insert(index++, new DialogItem
                {
                    Go = root,
                    Type = ItemType.Button,
                    LiveLabel = () => Loc.Get("title_display_resolution", view.screenSizeText?.text ?? ""),
                    AdjustChoice = direction =>
                    {
                        if (!IsCurrent() || !slider.isActiveAndEnabled || !slider.IsInteractable()) return;
                        slider.value = Mathf.Clamp(slider.value + direction, slider.minValue, slider.maxValue);
                        ScreenReader.Say(view.screenSizeText?.text ?? "");
                    }
                });
            }

            if (fullscreen != null)
            {
                string State() => Loc.Get(fullscreen.isOn ? "title_display_on" : "title_display_off");
                _items.Insert(index, new DialogItem
                {
                    Go = root,
                    Type = ItemType.Button,
                    LiveLabel = () => Loc.Get("title_display_fullscreen", State()),
                    NativeAction = () =>
                    {
                        if (!IsCurrent() || !fullscreen.isActiveAndEnabled || !fullscreen.IsInteractable()) return;
                        fullscreen.isOn = !fullscreen.isOn;
                        ScreenReader.Say(State());
                    }
                });
            }
        }
    }
}
