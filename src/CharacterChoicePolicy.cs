using System.Collections.Generic;

namespace DuelLinksAccess
{
    internal static class CharacterChoicePolicy
    {
        public static int FindPortraitIndex(int requested, IReadOnlyList<int> boundIds)
        {
            if (requested <= 0 || boundIds == null) return -1;
            int found = -1;
            for (int i = 0; i < boundIds.Count; i++)
                if (boundIds[i] == requested)
                {
                    if (found >= 0) return -1;
                    found = i;
                }
            return found;
        }

        public static int ReadSingle(IReadOnlyList<int> selected)
            => selected != null && selected.Count == 1 && selected[0] > 0 ? selected[0] : -1;

        public static bool CanConfirm(int requested, IReadOnlyList<int> selected, bool available, bool enabled)
            => requested > 0 && ReadSingle(selected) == requested && available && enabled;
    }
}
