using System;
using UnityEngine;
using Il2CppYgomGame.Deck;
namespace DuelLinksAccess
{
    internal static class DeckCopySpeech
    {
        internal static readonly DeckCopyState State = new();
        internal static GameObject CurrentRoot;
        internal static DeckCopyStage CurrentStage;
        internal static string Key(DeckInfo info) => info == null ? null
            : $"{info.CharaId}:{info.DeckId}:{info.StructureId}:{info.Name}";
        internal static string Pair() => Loc.Get("deck_copy_pair",
            State.SourceLabel ?? Loc.Get("deck_select_unknown"), State.DestinationLabel ?? Loc.Get("deck_select_unknown"));
        internal static string Confirmation(GameObject content, string text)
        {
            if (content == null || content != CurrentRoot || string.IsNullOrWhiteSpace(text)
                || (CurrentStage != DeckCopyStage.DestinationSelection && CurrentStage != DeckCopyStage.DestinationPreview)) return text;
            if (!text.Contains("複製", StringComparison.Ordinal) && !text.Contains("覆蓋", StringComparison.Ordinal)
                && !text.Contains("copy", StringComparison.OrdinalIgnoreCase) && !text.Contains("overwrite", StringComparison.OrdinalIgnoreCase)) return text;
            return Pair() + "。" + text;
        }
    }
}
