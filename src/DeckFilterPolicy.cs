namespace DuelLinksAccess
{
    public static class DeckFilterPolicy
    {
        public static string ChoiceGroupKey(string label, bool isLevel, bool isScale)
        {
            if (!int.TryParse(label, out _) || isLevel == isScale) return null;
            return isLevel ? "deck_filter_level_choice" : "deck_filter_scale_choice";
        }

        // Match verified native fields, never card names or arbitrary text.
        public static string NumericKey(string field) => field switch
        {
            "minAtk" => "deck_filter_atk_min",
            "maxAtk" => "deck_filter_atk_max",
            "minDef" => "deck_filter_def_min",
            "maxDef" => "deck_filter_def_max",
            _ => null
        };
    }
}
