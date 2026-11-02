using System;
using System.Globalization;
using System.Text.RegularExpressions;
namespace DuelLinksAccess
{
    internal static class ChronicleItemPolicy
    {
        internal static bool IsRowPath(string path)
            => Regex.IsMatch(path ?? "", @"^Response\.data\.box\.list\.(0|[1-9][0-9]*)$");
        internal static bool Counts(string value, int numericRemaining, int numericTotal, out int remaining, out int total)
        {
            remaining = numericRemaining; total = numericTotal;
            var match = Regex.Match(value ?? "", @"^\s*([0-9]+)\s*/\s*([0-9]+)\s*$");
            if (match.Success)
            {
                if (!int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out remaining)
                    || !int.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out total)) return false;
            }
            return remaining >= 0 && total >= 0 && remaining <= total;
        }
    }
}
