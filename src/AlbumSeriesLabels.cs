using System;
using System.Text.RegularExpressions;

namespace DuelLinksAccess
{
    internal static class AlbumSeriesLabels
    {
        // Match a complete resource-name token, not a list position or partial
        // word (for example DM must not match a filename containing RANDOM).
        internal static string Key(string resource)
        {
            if (string.IsNullOrWhiteSpace(resource)) return null;
            string nativeSeries = resource switch
            {
                "Images/Gate/#/GUI_T_GateLogo_s001" => "dm",
                "Images/Gate/#/GUI_T_GateLogo_s004" => "dsod",
                "Images/Gate/#/GUI_T_GateLogo_s002" => "gx",
                "Images/Gate/#/GUI_T_GateLogo_s003" => "5ds",
                "Images/Gate/#/GUI_T_GateLogo_s005" => "zexal",
                "Images/Gate/#/GUI_T_GateLogo_s006" => "arcv",
                "Images/Gate/#/GUI_T_GateLogo_s007" => "vrains",
                "Images/Gate/#/GUI_T_GateLogo_s008" => "sevens",
                "Images/Gate/#/GUI_T_GateLogo_s009" => "gorush",
                _ => null
            };
            if (nativeSeries != null) return "album_series_" + nativeSeries;
            foreach (string token in Regex.Split(resource.ToUpperInvariant(), @"[^A-Z0-9]+"))
            {
                string suffix = token switch
                {
                    "DM" => "dm", "DSOD" => "dsod", "GX" => "gx",
                    "5DS" => "5ds", "ZEXAL" => "zexal", "ARCV" => "arcv",
                    "VRAINS" => "vrains", "SEVENS" => "sevens", "GORUSH" => "gorush",
                    _ => null
                };
                if (suffix != null) return "album_series_" + suffix;
            }
            return null;
        }
    }
}
