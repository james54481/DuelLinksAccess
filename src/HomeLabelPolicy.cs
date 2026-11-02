namespace DuelLinksAccess
{
    internal static class HomeLabelPolicy
    {
        internal static string Key(string view, string name, string parent)
        {
            if (view == "Home" && name == "MenuButton") return "home_return_world";
            if (view == "Home") return name switch
            {
                "CharaList" => "home_chara_list",
                "ExpUp" => "home_level_up_button",
                "SeriesLogo" or "ChangeSeries" => "home_chara_change_series",
                "switch" => "home_chara_change_style",
                "mission" => "home_chara_mission",
                "DeckIconBg" => "home_deck_preview",
                "BackButton" => "screen_back",
                _ => null
            };
            if (view != "Single") return null;
            if (name == "YgomButton" && parent == "GeneralInfomationButton") return "home_events";
            return name switch
            {
                "ArrowL" => "home_area_left",
                "ArrowR" => "home_area_right",
                "PresentButton" => "home_gifts",
                "InfoButton" => "home_info",
                "UserInfoButton" => "home_user_info",
                "OptionButton" => "home_option",
                "NeuronCode(Clone)" => "home_neuron_code",
                "SelectChara" => "home_select_character",
                "SelectDeck" => "home_deck_select",
                "EditDeck" => "home_deck_edit",
                "SeriesLogo" or "SeriesButton" => "home_series",
                "MenuButton" => "home_character_button",
                "MenuRightBase" => "home_character_details",
                _ => null
            };
        }
    }
}
