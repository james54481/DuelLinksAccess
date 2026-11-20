namespace DuelLinksAccess
{
    public static class HomeInformationPolicy
    {
        public static string RowPath(string path) => path != null
            && System.Text.RegularExpressions.Regex.IsMatch(path,
                @"^Information\.page\.[A-Za-z0-9_]+(?:\.[A-Za-z0-9_]+)*\.list[0-9]*\.[0-9]+$") ? path : null;
        public static bool BlockNativeBack(bool requested, bool closing, bool atHome, bool dialogOpen)
            => (requested || closing) && atHome && !dialogOpen;
        public static string BannerKey(string path)
        {
            string name = path == null ? "" : path.Substring(path.LastIndexOf('/') + 1);
            return name switch
            {
                "Convenient_Banner1" => "home_information_useful_features",
                "GUI_T_KCreport_Banner1" => "home_information_kc_report",
                "Column_Banner1" => "home_information_duel_column",
                "SeriesIntro_Banner1" => "home_information_series_intro",
                "Special11_Banner2" => "home_information_duelist_point",
                "GUI_T_Banner1_PVPRank2" => "home_information_ranked",
                "Campaign142_Banner1" => "home_information_anniversary_countdown",
                "Campaign130_Banner1" => "home_information_ninth_anniversary",
                "GUI_T_Banner1_PVPRankCard" => "home_information_ticket_exchange",
                _ => null
            };
        }
        public static bool GenericTitle(string title, string term) => string.IsNullOrWhiteSpace(title)
            || title == term || title == "Button" || title == "button" || title == "vl" || title == "card";
        public static bool? SelectedTab(string title, System.Collections.Generic.IEnumerable<string> rows)
        {
            int kind = 0;
            foreach (string row in rows)
            {
                if (RowPath(row) == null) continue;
                int next = System.Text.RegularExpressions.Regex.IsMatch(row, @"\.list2\.[0-9]+$") ? 2
                    : System.Text.RegularExpressions.Regex.IsMatch(row, @"\.list\.[0-9]+$") ? 1 : 0;
                if (next == 0 || (kind != 0 && kind != next)) return null;
                kind = next;
            }
            if (kind == 0 || !IsTabTitle(title)) return null;
            bool useful = title == "有用的資訊" || title == "Useful Information";
            return useful ? kind == 2 : kind == 1;
        }
        public static string ItemKey(string rowPath, int target, string action = "open") => RowPath(rowPath) is string row
            ? "row:" + row + ":" + action : "target:" + target;
        public static bool SkipContainer(string rowPath, int containedRows)
            => RowPath(rowPath) == null && containedRows > 1;
        public static bool IsTabTitle(string title) => title == "正在進行的活動" || title == "有用的資訊"
            || title == "Ongoing Events" || title == "Useful Information" || title == "Current Events";
        public static bool CanOwn(bool requested, string content, string panel)
            => requested && content == "Single" && panel == "BannerList";
        public static bool HasVisibleArea(float left, float bottom, float right, float top, int width, int height)
            => System.Math.Min(right, width) - System.Math.Max(left, 0) > 40
                && System.Math.Min(top, height) - System.Math.Max(bottom, 0) > 40;
    }
}
