using UnityEngine;
namespace DuelLinksAccess
{
    public partial class ScreenButtonHandler
    {
        private void AddScenarioAccessibility(GameObject root)
        {
            if (!ScenarioControlPolicy.IsScenario(root?.name)) return;
            foreach (var item in _items)
            {
                if (item.Go == null) continue;
                string key = ScenarioControlPolicy.ButtonKey(root.name, item.Go.name);
                if (key == null) continue;
                item.Label = Loc.Get(key);
                // Never resolve a playback button from nearby speaker text.
                item.LiveLabel = () => Loc.Get(key);
            }
            _textMode = false;
            _items.Insert(0, new ScreenItem {
                Go = root, Type = ItemType.Button,
                Label = Loc.Get("scenario_dialogue"),
                LiveLabel = () => !string.IsNullOrWhiteSpace(_lastReadText) ? _lastReadText : Loc.Get("scenario_dialogue"),
                NativeAction = () => { if (root == _screenRoot) AdvanceScreen(); },
                ReadDetails = () => {
                    if (root != _screenRoot) return;
                    string text = ReadScenarioMessageText(root);
                    ScreenReader.Say(string.IsNullOrWhiteSpace(text) ? Loc.Get("scenario_dialogue") : text);
                }
            });
            // PollTextChanges continues independently of the button list. It
            // waits for the typewriter text to settle and suppresses mirrored
            // title dialogs, without automatically advancing the scenario.
        }
    }
}
