using Xunit;
namespace DuelLinksAccess.Tests;
public class AlbumSeriesLabelsTests
{
    [Theory]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s001", "album_series_dm")]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s004", "album_series_dsod")]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s002", "album_series_gx")]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s003", "album_series_5ds")]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s005", "album_series_zexal")]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s006", "album_series_arcv")]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s007", "album_series_vrains")]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s008", "album_series_sevens")]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s009", "album_series_gorush")]
    [InlineData("Images/Series/DM/logo", "album_series_dm")]
    [InlineData("GUI_DSOD_logo", "album_series_dsod")]
    [InlineData("series/gx.png", "album_series_gx")]
    [InlineData("banner_5ds", "album_series_5ds")]
    [InlineData("/ZEXAL/", "album_series_zexal")]
    [InlineData("series_ARCV", "album_series_arcv")]
    [InlineData("VRAINS_logo", "album_series_vrains")]
    [InlineData("SEVENS", "album_series_sevens")]
    [InlineData("GORUSH!!", "album_series_gorush")]
    public void WholeResourceTokensResolve(string resource, string expected)
        => Assert.Equal(expected, AlbumSeriesLabels.Key(resource));
    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("15")]
    [InlineData("RANDOM_logo")] [InlineData("GXNEW")]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s010")]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s001_changed")]
    public void UnknownResourcesRemainUnknown(string resource)
        => Assert.Null(AlbumSeriesLabels.Key(resource));
}
