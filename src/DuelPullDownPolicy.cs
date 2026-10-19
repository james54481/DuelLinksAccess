using System.Collections.Generic;

namespace DuelLinksAccess
{
    public static class DuelPullDownPolicy
    {
        public static bool CanConfirm(bool single, int required, IReadOnlyList<bool> selected,
            int current, bool enabled, bool pending, bool labelsMatch)
        {
            if (!enabled || pending || !labelsMatch || selected == null || selected.Count == 0)
                return false;
            int count = 0;
            for (int i = 0; i < selected.Count; i++) if (selected[i]) count++;
            return single
                ? count == 1 && current >= 0 && current < selected.Count && selected[current]
                : required > 0 && count == required;
        }
    }
}
