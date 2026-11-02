using System.Text.RegularExpressions;
namespace DuelLinksAccess
{
    internal static class GateCostPolicy
    {
        internal static int? ItemId(string sprite)
        {
            var m = Regex.Match(sprite ?? "", @"^ItemA([0-9]{4})$");
            return m.Success && int.TryParse(m.Groups[1].Value, out int id) && id > 0 ? id : null;
        }
        internal static bool Counts(string left, string right, out int required, out int owned)
        {
            required = owned = 0;
            left = left?.Trim(); right = right?.Trim();
            return Regex.IsMatch(left ?? "", @"^[0-9]+$")
                && Regex.IsMatch(right ?? "", @"^/[0-9]+$")
                && int.TryParse(left, out required) && int.TryParse(right.Substring(1), out owned);
        }
    }
}
