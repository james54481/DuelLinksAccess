using Xunit;

namespace DuelLinksAccess.Tests;

public class StudioMenuLabelsTests
{
    [Theory]
    [InlineData(0, "0", "studio_deck_editor")]
    [InlineData(1, "1", "studio_card_catalog")]
    [InlineData(16, "16", "studio_chronicle")]
    [InlineData(9, "9_1", "studio_skills")]
    [InlineData(12, "12", "studio_rank_popular")]
    [InlineData(11, "11_1", "studio_beginner")]
    [InlineData(7, "7", "studio_kc_report")]
    [InlineData(8, "8", "studio_deck_lists")]
    [InlineData(4, "4", "studio_customize")]
    [InlineData(2, "10", "studio_summon_animations")]
    [InlineData(15, "15", "studio_scene_album")]
    public void VerifiedNativePairsHaveLabels(int id, string image, string key)
        => Assert.Equal(key, StudioMenuLabels.Key(id, image));

    [Theory]
    [InlineData(15, "new-art")]
    [InlineData(2, "2")]
    [InlineData(99, "15")]
    [InlineData(0, null)]
    public void UnknownOrChangedPairsDoNotBorrowAnotherLabel(int id, string image)
        => Assert.Null(StudioMenuLabels.Key(id, image));
}
