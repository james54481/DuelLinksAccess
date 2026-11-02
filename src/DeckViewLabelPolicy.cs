namespace DuelLinksAccess
{
    internal static class DeckViewLabelPolicy
    {
        // Verified read-only opponent deck screen. Do not apply to editors.
        public static string ScreenKey(string view) => view == "DeckDetail_PC" ? "screen_deck_view" : null;

        public static string ButtonKey(string view, string name)
        {
            if (view != "DeckDetail_PC") return null;
            return name switch
            {
                "HomeButton" => "deck_view_home",
                "BackButton" => "screen_back",
                "SkillInfoButton" => "deck_view_skill_info",
                _ => null
            };
        }
    }
}
