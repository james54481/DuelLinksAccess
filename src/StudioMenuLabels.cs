namespace DuelLinksAccess
{
    // james54481, 2026-11-11: native banner ID/image pairs checked against
    // the Traditional Chinese CardCenterMainMenu. Never use list position.
    internal static class StudioMenuLabels
    {
        internal static string Key(int id, string image) => (id, image) switch
        {
            (0, "0") => "studio_deck_editor",
            (1, "1") => "studio_card_catalog",
            (16, "16") => "studio_chronicle",
            (9, "9_1") => "studio_skills",
            (12, "12") => "studio_rank_popular",
            (11, "11_1") => "studio_beginner",
            (7, "7") => "studio_kc_report",
            (8, "8") => "studio_deck_lists",
            (4, "4") => "studio_customize",
            (2, "10") => "studio_summon_animations",
            (15, "15") => "studio_scene_album",
            _ => null
        };
    }
}
