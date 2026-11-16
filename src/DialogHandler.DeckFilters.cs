using System;
using UnityEngine;
using UnityEngine.UI;
using Il2CppYgomGame.Deck;

namespace DuelLinksAccess
{
    public partial class DialogHandler
    {
        private GameObject _deckFilterFocusGo;
        private GameObject _deckFilterFocusRoot;

        private static bool IsDeckFilter(GameObject root) =>
            root != null && root.GetComponent<FilterSettingViewController>() != null;

        private void ApplyDeckFilterLabels(GameObject root)
        {
            if (!IsDeckFilter(root)) return;
            try
            {
                var vc = root.GetComponent<FilterSettingViewController>();
                // Remove navigation chrome from module browsing only, not from the game.
                _items.RemoveAll(it => it.Go != null
                    && it.Go.GetComponentInParent<Scrollbar>() != null);
                foreach (var choice in root.GetComponentsInChildren<FilterSettingItem>(true))
                {
                    if (choice == null || !choice.gameObject.activeInHierarchy) continue;
                    string text = LabelExtractor.StripRichText(choice.label?.text ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    // Identify the actual native group, not the numeric value or list order.
                    bool isLevel = vc.levels != null && choice.transform.IsChildOf(vc.levels);
                    bool isScale = vc.scales != null && choice.transform.IsChildOf(vc.scales);
                    string groupKey = DeckFilterPolicy.ChoiceGroupKey(text, isLevel, isScale);
                    if (groupKey != null) text = Loc.Get(groupKey, text);
                    var control = choice.myself;
                    if (control == null || !control.gameObject.activeInHierarchy) continue;
                    var matches = _items.FindAll(it => it.Go != null
                        && (it.Go == choice.gameObject || it.Go.transform.IsChildOf(choice.transform)));
                    if (matches.Count == 0) continue;
                    var item = matches[0];
                    foreach (var duplicate in matches)
                        if (!ReferenceEquals(item, duplicate)) _items.Remove(duplicate);
                    item.Go = control.gameObject;
                    item.LiveLabel = () => Loc.Get(choice.IsSelected
                        ? "deck_auto_selected" : "deck_auto_not_selected", text);
                    item.Label = item.LiveLabel();
                    item.DeferredActivationLabel = true;
                }
                foreach (var item in _items)
                {
                    if (item.Go == null) continue;
                    var tab = item.Go.GetComponent<Il2CppYgomSystem.UI.YgomTabButton>();
                    if (tab == null) continue;
                    string text = LabelExtractor.StripRichText((tab.normalLabel ?? tab.selectLabel)?.text ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    item.LiveLabel = () => Loc.Get(tab.isSelected
                        ? "deck_filter_tab_selected" : "deck_filter_tab", text);
                    item.Label = item.LiveLabel();
                }
                LabelDeckFilterNumber(vc.minAtk, "minAtk");
                LabelDeckFilterNumber(vc.maxAtk, "maxAtk");
                LabelDeckFilterNumber(vc.minDef, "minDef");
                LabelDeckFilterNumber(vc.maxDef, "maxDef");
                DebugLogger.Log(LogCategory.Handler, "DeckFilters", $"Labeled native filters; items={_items.Count}");
            }
            catch (Exception ex)
            {
                DebugLogger.Log(LogCategory.Handler, "DeckFilters", ex.ToString());
            }
        }

        private void LabelDeckFilterNumber(InputField input, string field)
        {
            string key = DeckFilterPolicy.NumericKey(field);
            if (input == null || !input.gameObject.activeInHierarchy || key == null) return;
            var item = _items.Find(it => it.Go != null
                && (it.Go == input.gameObject || it.Go.transform.IsChildOf(input.transform)));
            if (item == null) return;
            item.LiveLabel = () => Loc.Get("deck_filter_number", Loc.Get(key),
                string.IsNullOrWhiteSpace(input.text) ? Loc.Get("deck_filter_unrestricted") : input.text);
            item.Label = item.LiveLabel();
        }
    }
}
