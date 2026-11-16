using System;
using UnityEngine;
using UnityEngine.UI;
using Il2CppYgomGame.Deck;

namespace DuelLinksAccess
{
    public partial class DialogHandler
    {
        private float _deckTrialRefreshAt;
        private GameObject _deckTrialRefreshRoot;

        private void ApplyNativeDeckMenu(GameObject root)
        {
            var vc = root.GetComponent<DeckMenuDialogViewController>();
            if (vc == null) return;
            _items.RemoveAll(it => it.Go != null && it.Go.GetComponentInParent<Scrollbar>() != null);
            foreach (var item in _items)
            {
                if (item.Go == null) continue;
                if (vc.deepLinkButton != null && item.Go == vc.deepLinkButton.gameObject)
                    item.Label = Loc.Get("deck_menu_copy_url");
                if (vc.drawTestButton != null && item.Go == vc.drawTestButton.gameObject)
                    item.Label = Loc.Get("deck_menu_trial");
                if (vc.selKiraButton != null && item.Go == vc.selKiraButton.gameObject)
                    item.LiveLabel = () => Loc.Get("deck_menu_finish_order", LabelExtractor.StripRichText(vc.selKiraCurrent?.text ?? ""));
                if (vc.historyButton != null && item.Go == vc.historyButton.gameObject)
                {
                    string label = LabelExtractor.GetLabel(item.Go);
                    item.LiveLabel = () => Loc.Get(vc.historyButton.isOn ? "deck_auto_selected" : "deck_auto_not_selected", label);
                    item.DeferredActivationLabel = true;
                }
                if (item.LiveLabel != null) item.Label = item.LiveLabel();
            }
        }

        private void PollDeckTrialRefresh()
        {
            if (_deckTrialRefreshRoot == null || _deckTrialRefreshAt <= 0f) return;
            if (GetTopVcRoot("dialog") != _deckTrialRefreshRoot)
            { _deckTrialRefreshRoot = null; _deckTrialRefreshAt = 0f; return; }
            if (Time.unscaledTime < _deckTrialRefreshAt) return;
            var vc = _deckTrialRefreshRoot.GetComponent<DrawTestViewController>();
            if (vc == null || !vc.setupDone || vc.initButton == null || !vc.initButton.interactable) return;
            _deckTrialRefreshAt = 0f;
            _deckTrialRefreshRoot = null;
            ScanDialog();
        }

        private void ApplyDeckTrial(GameObject root)
        {
            var vc = root.GetComponent<DrawTestViewController>();
            if (vc == null) return;
            _items.Clear();
            if (vc.deckCards != null)
                for (int i = 0; i < vc.deckCards.Count; i++)
                {
                    var card = vc.deckCards[i];
                    if (card == null || !card.gameObject.activeInHierarchy || card.IsMasked || card.Mrk <= 0
                        || vc.drawnCards?.content == null || !card.transform.IsChildOf(vc.drawnCards.content)) continue;
                    int mrk = card.Mrk;
                    _items.Add(new DialogItem {
                        Go = root, Type = ItemType.Button, Label = CardFormatter.FormatCompact(mrk),
                        ReadDetails = () => ScreenReader.Say(CardFormatter.FormatVerbose(mrk)),
                        NativeAction = () => {
                            if (card == null || !card.gameObject.activeInHierarchy || card.Mrk != mrk) return;
                            vc.cardClicked(card);
                            ScreenReader.Say(CardFormatter.FormatVerbose(mrk));
                        }
                    });
                }
            AddDeckTrialAction(root, vc.initButton, "deck_trial_redraw", () => vc.DrawClicked());
            AddDeckTrialAction(root, vc.addButton, "deck_trial_add", () => vc.AddClicked());
            _items.Add(new DialogItem { Go = root, Type = ItemType.Button, Label = Loc.Get("screen_back"),
                NativeAction = () => vc.BackClicked() });
            DebugLogger.Log(LogCategory.Handler, "DeckTrial", $"Visible cards={_items.Count - 3}");
        }

        private void AddDeckTrialAction(GameObject root, Button button, string key, Action action)
        {
            if (button == null || !button.gameObject.activeInHierarchy) return;
            _items.Add(new DialogItem { Go = root, Type = ItemType.Button, Label = Loc.Get(key), NativeAction = () => {
                if (!button.interactable) { ScreenReader.Say(Loc.Get("deck_operations_unavailable")); return; }
                action();
                _deckTrialRefreshRoot = root;
                _deckTrialRefreshAt = Time.unscaledTime + 0.5f;
            }});
        }
    }
}
