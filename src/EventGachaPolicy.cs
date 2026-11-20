using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
namespace DuelLinksAccess
{
    public static class EventGachaPolicy
    {
        // Verified single-draw receipt: it shows only "R gem x3" and uses
        // num=0 to omit the separate multiplicity column.
        public static bool IsSingleQuantityReceipt(int rows, long copies) => rows == 1 && copies == 0;
        public static bool CanAcknowledge(bool pendingDraw, bool samePage, bool actionAtPoint, bool cardAtPoint)
            => pendingDraw && samePage && !actionAtPoint && !cardAtPoint;
        public static bool IsReceiptRow(string path) => Regex.IsMatch(path ?? "", @"^DialogGet\.get_data\.item\.\d+$");
        public static bool TryUnitLabel(string text, out string name, out long units)
        {
            var match = Regex.Match(text ?? "", @"^(.+?)\s*[×x]\s*([0-9]+)\s*$");
            name = match.Success ? match.Groups[1].Value.Trim() : text;
            units = -1;
            return match.Success && long.TryParse(match.Groups[2].Value, out units);
        }
        public static long? TotalUnits(long units, long copies)
            => units < 0 || copies < 0 || (copies != 0 && units > long.MaxValue / copies) ? null : units * copies;
        public static bool IsRewardRow(string path) => Regex.IsMatch(path ?? "",
            @"^EventGacha\.htjson\.item_list\.cwork\.tab\.\d+\.reward\.list\.\d+\.group\.\d+\.list\.\d+$");
        public static string UniqueQuantity(IEnumerable<string> parts)
        {
            var values = parts.Where(IsQuantity).Select(text => text.Trim()).Distinct().ToArray();
            return values.Length == 1 ? values[0] : "UNKNOWN";
        }
        public static bool IsDrawPath(string path) => path == "EventGacha.state.button.gachaL" || path == "EventGacha.state.button.gachaR";
        public static bool ValidDraw(long number, long cost) => number > 0 && cost >= 0;
        public static bool IsQuantity(string text) => Regex.IsMatch(text ?? "", @"^\s*(?:[0-9]+|[0-9]{1,3}(?:,[0-9]{3})+)\s*$");
    }
}
