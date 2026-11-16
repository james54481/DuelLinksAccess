using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Il2CppYgomGame.Deck;

namespace DuelLinksAccess
{
    public partial class ScreenButtonHandler
    {
        private void AddDeckRecipeLabels(GameObject root)
        {
            var vc = root?.GetComponent<DeckRecipeViewController>();
            if (vc == null) return;
            try
            {
                _items.RemoveAll(it => it.Go != null && it.Go.GetComponentInParent<Scrollbar>() != null);
                foreach (var row in root.GetComponentsInChildren<DeckRecipeItem>(false))
                {
                    if (row == null || row.Data == null || !row.gameObject.activeInHierarchy || row.Data.RecipeId <= 0) continue;
                    var data = row.Data;
                    string summary = DeckRecipePolicy.Summary(
                        LabelExtractor.StripRichText(row.deckText?.text ?? ""),
                        LabelExtractor.StripRichText(row.nameText?.text ?? ""),
                        LabelExtractor.StripRichText(row.skillText?.text ?? ""),
                        LabelExtractor.StripRichText(row.ratioText?.text ?? ""),
                        LabelExtractor.StripRichText(row.dateText?.text ?? ""));
                    if (string.IsNullOrWhiteSpace(summary)) continue;
                    int insert = _items.FindIndex(it => it.Go != null && it.Go.transform.IsChildOf(row.transform));
                    _items.RemoveAll(it => it.Go != null && it.Go.transform.IsChildOf(row.transform));
                    Action<bool> open = info => {
                        if (_screenRoot != root || !LabelExtractor.IsAlive(row.gameObject)
                            || !row.gameObject.activeInHierarchy || row.Data == null || row.Data.Pointer != data.Pointer) return;
                        if (info) row.InfoClicked(); else row.BaseClicked();
                    };
                    if (insert < 0) insert = _items.Count;
                    _items.Insert(insert, new ScreenItem { Go = row.gameObject, Type = ItemType.Button,
                        Label = Loc.Get("deck_recipe_open", summary), NativeAction = () => open(false) });
                    if (row.infoButton != null && row.infoButton.activeInHierarchy)
                        _items.Insert(insert + 1, new ScreenItem { Go = row.gameObject, Type = ItemType.Button,
                            Label = Loc.Get("deck_recipe_info", summary), NativeAction = () => open(true) });
                }
                foreach (var item in _items)
                {
                    if (item.Go?.name == "BackButton") item.Label = Loc.Get("screen_back");
                    if (item.Go?.name == "HomeButton") item.Label = Loc.Get("deck_view_home");
                }
                LabelRecipeToggle(root, vc.distributeButton, true);
                LabelRecipeToggle(root, vc.contributeButton, true);
                LabelRecipeToggle(root, vc.maskComped, false);
                LabelRecipeToggle(root, vc.maskIncomped, false);
                LabelRecipeToggle(root, vc.maskFav, false);
                LabelRecipeToggle(root, vc.maskSpeed, false);
                LabelRecipeToggle(root, vc.maskRush, false);
            }
            catch (Exception ex) { DebugLogger.Log(LogCategory.Handler, "DeckRecipes", ex.ToString()); }
        }

        private void LabelRecipeToggle(GameObject root, Toggle toggle, bool tab)
        {
            if (toggle == null || !toggle.gameObject.activeInHierarchy) return;
            var item = _items.FirstOrDefault(it => it.Go == toggle.gameObject);
            if (item == null) return;
            string text = LabelExtractor.GetLabel(toggle.gameObject);
            item.LiveLabel = () => Loc.Get(toggle.isOn ? "deck_auto_selected" : "deck_auto_not_selected", text);
            item.Label = item.LiveLabel();
            item.NativeAction = () => {
                if (_screenRoot != root || !toggle.gameObject.activeInHierarchy || !toggle.interactable) return;
                if (!tab || !toggle.isOn) toggle.isOn = !toggle.isOn;
                ScreenReader.Say(item.LiveLabel());
                _noticeTabFocus = item.LiveLabel();
                _tabRescanDelay = 0.5f;
            };
        }
    }
}
