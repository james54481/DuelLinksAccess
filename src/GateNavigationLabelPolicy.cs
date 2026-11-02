namespace DuelLinksAccess
{
    internal static class GateNavigationLabelPolicy
    {
        public static string ButtonKey(string view, string name)
        {
            if (view != "Gate") return null;
            return name switch
            {
                "Back" or "ArrowL" => "gate_previous_duelist",
                "Next" => "gate_next_duelist",
                "CharaList(Clone)" => "gate_change_style",
                "BackButton" => "screen_back",
                _ => null
            };
        }

        public static bool HideDuplicate(string view, string name, bool hasSwitchButton)
            => view == "Gate" && name == "CurrentSeriesLogo" && hasSwitchButton;

        public static bool HideEmptyMissionSwitch(string view, string name, bool isLiveMission, int? characterCount)
            => view == "Gate" && name == "switch" && isLiveMission && characterCount == 0;
    }
}
