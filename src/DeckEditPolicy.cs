namespace DuelLinksAccess
{
    public readonly record struct DeckCountResult(
        bool IsExtraDeck, int Count);

    public static class DeckEditPolicy
    {
        public static int RestoreFocus(System.Collections.Generic.IReadOnlyList<int> cards, int mrk, int previousIndex)
        {
            if (cards.Count == 0) return 0;
            // Duplicate copies retain their existing position when possible.
            if (previousIndex >= 0 && previousIndex < cards.Count && cards[previousIndex] == mrk)
                return previousIndex;
            for (int i = 0; i < cards.Count; i++)
                if (mrk > 0 && cards[i] == mrk) return i;
            return System.Math.Max(0, System.Math.Min(previousIndex, cards.Count - 1));
        }

        public static bool UseFilteredCollection(bool filterExists)
        {
            return filterExists;
        }

        public static DeckCountResult ResolveAddedCount(
            int mainBefore,
            int extraBefore,
            int mainAfter,
            int extraAfter)
        {
            return extraAfter > extraBefore
                ? new DeckCountResult(true, extraAfter)
                : new DeckCountResult(false, mainAfter);
        }

        public static DeckCountResult ResolveRemovedCount(
            bool removedFromExtraDeck,
            int mainAfter,
            int extraAfter)
        {
            return removedFromExtraDeck
                ? new DeckCountResult(true, extraAfter)
                : new DeckCountResult(false, mainAfter);
        }
    }
}
