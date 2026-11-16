using System;

namespace DuelLinksAccess
{
    // james54481, 2026-11-06: event-specific identity validation.
    public static class DuelDestructionPolicy
    {
        public static string ResolveName(int locate, int uniqueId, int observedUid,
            string observedName, Func<int, string> lookup)
        {
            // Only field locations; never interpret a position as an alternate UID.
            bool field = locate == 1 || locate == 2 || locate == 3 || locate == 5
                || locate == 6 || locate == 8 || locate == 9 || locate == 10
                || locate == 11 || locate == 12;
            if (!field || uniqueId <= 0) return null;
            if (observedUid > 0 && observedUid != uniqueId) return null;
            if (observedUid == uniqueId && !string.IsNullOrWhiteSpace(observedName))
                return observedName;
            return lookup(uniqueId);
        }
    }
}
