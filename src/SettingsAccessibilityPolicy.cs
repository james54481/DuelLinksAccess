using System.Collections.Generic;

namespace DuelLinksAccess
{
    public static class SettingsAccessibilityPolicy
    {
        // Layout sibling paths keep each row's controls together, including
        // off-screen rows; screen coordinates change while scrolling.
        public static int ComparePaths(IReadOnlyList<int> left, IReadOnlyList<int> right)
        {
            int count = System.Math.Min(left.Count, right.Count);
            for (int i = 0; i < count; i++)
            {
                int result = left[i].CompareTo(right[i]);
                if (result != 0) return result;
            }
            return left.Count.CompareTo(right.Count);
        }
    }
}
