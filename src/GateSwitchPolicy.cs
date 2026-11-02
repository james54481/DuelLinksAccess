using System.Text.RegularExpressions;
namespace DuelLinksAccess
{
    internal static class GateSwitchPolicy
    {
        internal static bool IsGateIcon(string path) => path != null
            && (Regex.IsMatch(path, @"^Images/Gate/#/GUI_T_GateLogo_s[0-9]{3}$")
                || Regex.IsMatch(path, @"^Temp/Event/Ev[0-9]+/Images/#/GUI_T_GateLogo$"));
        internal static string Series(string path)
        {
            if (!IsGateIcon(path)) return null;
            if (path.StartsWith("Temp/Event/", System.StringComparison.Ordinal)) return "EVENT";
            return path switch
            {
                "Images/Gate/#/GUI_T_GateLogo_s001" => "DM",
                "Images/Gate/#/GUI_T_GateLogo_s004" => "DSOD",
                "Images/Gate/#/GUI_T_GateLogo_s002" => "GX",
                "Images/Gate/#/GUI_T_GateLogo_s003" => "5D's",
                "Images/Gate/#/GUI_T_GateLogo_s005" => "ZEXAL",
                "Images/Gate/#/GUI_T_GateLogo_s006" => "ARC-V",
                "Images/Gate/#/GUI_T_GateLogo_s007" => "VRAINS",
                _ => null
            };
        }
    }
}
