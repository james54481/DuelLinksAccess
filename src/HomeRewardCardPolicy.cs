using System;
using System.Collections.Generic;
using System.Globalization;

namespace DuelLinksAccess
{
    internal static class HomeRewardCardPolicy
    {
        // Numeric texture names were matched against the native detail IDs.
        // Never parse the level, GO name, sprite atlas, or a partly numeric filename.
        internal static int Resolve(IEnumerable<string> textureNames)
        {
            int found = 0;
            if (textureNames == null) return 0;
            foreach (string name in textureNames)
            {
                if (string.IsNullOrEmpty(name) || name[0] == '0') continue;
                bool digits = true;
                foreach (char c in name) if (c < '0' || c > '9') { digits = false; break; }
                if (!digits || !int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out int id) || id <= 0) continue;
                if (found != 0 && found != id) return 0;
                found = id;
            }
            return found;
        }
    }
}
