using System;
using System.Text.RegularExpressions;
namespace DuelLinksAccess
{
    // james54481, 2026-10-26. Negative values mean UNKNOWN, never "enough".
    public static class OrbExchangePolicy
    {
        // Verified CardTrader RespectOrb entry itemId (2026-10-26 trace).
        public const int RespectOrbItemId = 212;
        public static bool TryReadCostText(string text, out int cost, out long balance)
        {
            cost = -1; balance = -1;
            var match = Regex.Match(text ?? "", @"^\s*([0-9]+)\s*(?:[/／]\s*([0-9]+))?\s*$");
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out cost)) return false;
            if (match.Groups[2].Success && !long.TryParse(match.Groups[2].Value, out balance)) return false;
            return true;
        }
        public static long Shortfall(int required, long owned)
            => required < 0 || owned < 0 ? -1 : Math.Max(0L, (long)required - owned);
    }
}
