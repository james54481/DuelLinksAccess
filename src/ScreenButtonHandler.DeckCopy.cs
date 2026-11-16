using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Il2CppYgomGame.Deck;

namespace DuelLinksAccess
{
    public partial class ScreenButtonHandler
    {
        private string _lastDeckCopySignature;
        private void PollDeckCopyTransition(string view)
        {
            if (view != "DeckSelect" && view != "DeckDetail_PC") { _lastDeckCopySignature = null; return; }
            try
            {
                var root = GetActiveScreenRoot();
                if (root == null) return;
                var select = root.GetComponent<DeckSelectViewController>();
                var detail = root.GetComponent<DeckDetailViewController>();
                string signature = root.GetInstanceID() + ":" + (select != null ? select.Mode.ToString() : detail?.Mode.ToString());
                if (detail != null && detail.isCopyMode())
                    signature += $":{detail.setupDone}:{detail.deckInfo?.mainDeck?.Count}:{detail.deckInfo?.extraDeck?.Count}";
                if (_lastDeckCopySignature != null && _lastDeckCopySignature != signature)
                { _scanned = false; _scanDelay = 0.5f; _scanAttempts = 0; }
                _lastDeckCopySignature = signature;
            }
            catch { }
        }

        private void CaptureDeckCopySelection(DeckSelectItem item, string label)
        {
            var vc = _screenRoot?.GetComponent<DeckSelectViewController>();
            if (vc == null || item.Info == null) return;
            var stage = DeckCopyPolicy.Stage(true, vc.Mode.ToString());
            if (stage == DeckCopyStage.SourceSelection)
                DeckCopySpeech.State.Source(DeckCopySpeech.Key(item.Info), label);
            if (stage == DeckCopyStage.DestinationSelection)
                DeckCopySpeech.State.Destination(DeckCopySpeech.Key(item.Info), label, _screenRoot.GetInstanceID());
        }

        private static string CopyInfoLabel(DeckInfo info)
        {
            if (info == null) return Loc.Get("deck_select_unknown");
            string name = LabelExtractor.StripRichText(info.Name ?? "");
            string owner = LabelExtractor.ResolveCharaName(info.CharaId);
            return Loc.Get("deck_copy_slot_unknown", string.IsNullOrWhiteSpace(owner) ? name : Loc.Get("deck_slot_owner", owner, name));
        }

        private void AddDeckCopyAccessibility(GameObject root)
        {
            var select = root.GetComponent<DeckSelectViewController>();
            var detail = root.GetComponent<DeckDetailViewController>();
            var stage = select != null ? DeckCopyPolicy.Stage(true, select.Mode.ToString())
                : detail != null ? DeckCopyPolicy.Stage(false, detail.Mode.ToString()) : DeckCopyStage.None;
            DeckCopySpeech.CurrentRoot = root;
            DeckCopySpeech.CurrentStage = stage;
            if (stage == DeckCopyStage.None) { DeckCopySpeech.State.End(); return; }
            if (stage == DeckCopyStage.SourceSelection) DeckCopySpeech.State.BeginSource(root.GetInstanceID());
            var parent = detail?.copyFromVC?.TryCast<DeckSelectViewController>();
            var source = select?.copySrc ?? parent?.copySrc;
            if (source != null && DeckCopySpeech.State.SourceKey != DeckCopySpeech.Key(source))
                DeckCopySpeech.State.Source(DeckCopySpeech.Key(source), CopyInfoLabel(source));
            var destination = select?.copyDst ?? parent?.copyDst;
            if (destination != null && DeckCopySpeech.State.DestinationKey != DeckCopySpeech.Key(destination))
                DeckCopySpeech.State.Destination(DeckCopySpeech.Key(destination), CopyInfoLabel(destination), slotKnown: false);
            if (stage == DeckCopyStage.SourcePreview && detail.deckInfo != null
                && DeckCopySpeech.State.SourceKey != DeckCopySpeech.Key(detail.deckInfo))
                DeckCopySpeech.State.Source(DeckCopySpeech.Key(detail.deckInfo), CopyInfoLabel(detail.deckInfo));

            string context = stage switch {
                DeckCopyStage.SourceSelection => Loc.Get("deck_copy_choose_source"),
                DeckCopyStage.SourcePreview => Loc.Get("deck_copy_preview_source", DeckCopySpeech.State.SourceLabel ?? CopyInfoLabel(detail.deckInfo)),
                DeckCopyStage.DestinationSelection => Loc.Get("deck_copy_choose_destination", DeckCopySpeech.State.SourceLabel ?? Loc.Get("deck_select_unknown")),
                DeckCopyStage.DestinationPreview => Loc.Get("deck_copy_preview_destination") + "。" + DeckCopySpeech.Pair(),
                _ => ""
            };
            _lastReadText = context;
            _items.Insert(0, new ScreenItem { Go = root, Label = context, Type = ItemType.Information });
            if (detail == null) return;
            int insert = 1;
            insert = AddCopyPreviewCards(root, detail, detail.deckInfo?.mainDeck, "deck_zone_main_name", insert);
            AddCopyPreviewCards(root, detail, detail.deckInfo?.extraDeck, "deck_zone_extra_name", insert);
            string expectedSource = DeckCopySpeech.Key(source) ?? DeckCopySpeech.State.SourceKey;
            string expectedDestination = DeckCopySpeech.Key(destination) ?? DeckCopySpeech.State.DestinationKey;
            bool destinationEvidence = destination != null || (parent != null
                && DeckCopySpeech.State.DestinationSelectionRoot == parent.gameObject.GetInstanceID());
            foreach (var buttonItem in _items)
            {
                if (buttonItem.Go == null || !buttonItem.Go.name.StartsWith("FooterButton", StringComparison.Ordinal)) continue;
                var button = buttonItem.Go.GetComponent<Button>();
                if (button == null) continue;
                buttonItem.Label = stage == DeckCopyStage.SourcePreview ? Loc.Get("deck_copy_continue_source")
                    : Loc.Get("deck_copy_confirm_action", DeckCopySpeech.Pair());
                buttonItem.NativeAction = () => {
                    if (_screenRoot != root || GetActiveScreenRoot() != root || !button.gameObject.activeInHierarchy || !button.interactable) return;
                    if (stage == DeckCopyStage.DestinationPreview && (!destinationEvidence || !DeckCopySpeech.State.Matches(
                        expectedSource, expectedDestination)))
                    { ScreenReader.Say(Loc.Get("deck_copy_context_unknown")); return; }
                    button.onClick.Invoke();
                };
            }
            DebugLogger.Log(LogCategory.Handler, "DeckCopy", $"stage={stage} {context}");
        }

        private int AddCopyPreviewCards(GameObject root, DeckDetailViewController vc,
            Il2CppSystem.Collections.Generic.List<CardAndRarity> cards, string zoneKey, int insert)
        {
            _items.Insert(insert++, new ScreenItem { Go = root, Type = ItemType.Information,
                Label = Loc.Get("deck_copy_cards_heading", Loc.Get(zoneKey), cards?.Count ?? 0) });
            if (cards == null) return insert;
            string deckKey = DeckCopySpeech.Key(vc.deckInfo);
            for (int i = 0; i < cards.Count; i++)
            {
                int mrk = cards[i].Mrk;
                int position = i + 1;
                _items.Insert(insert++, new ScreenItem { Go = root, Type = ItemType.Information,
                    Label = Loc.Get("deck_copy_card_position", CardFormatter.FormatCompact(mrk), position, cards.Count),
                    ReadDetails = () => {
                        if (GetActiveScreenRoot() == root && DeckCopySpeech.Key(vc.deckInfo) == deckKey)
                            ScreenReader.Say(CardFormatter.FormatVerbose(mrk));
                    } });
            }
            return insert;
        }
    }
}
