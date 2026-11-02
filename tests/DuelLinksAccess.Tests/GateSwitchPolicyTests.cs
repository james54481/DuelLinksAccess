using Xunit;
namespace DuelLinksAccess.Tests;
public class GateSwitchPolicyTests
{
    [Theory]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s001", "DM")]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s004", "DSOD")]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s002", "GX")]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s003", "5D's")]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s005", "ZEXAL")]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s006", "ARC-V")]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s007", "VRAINS")]
    [InlineData("Temp/Event/Ev0201/Images/#/GUI_T_GateLogo", "EVENT")]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s999", null)]
    [InlineData("Images/Gate/#/GUI_T_GateLogo_s001_other", null)]
    [InlineData("Other/GUI_T_GateLogo_s001", null)]
    [InlineData(null, null)]
    public void UsesResourceIdentityNotListOrdinal(string path, string expected)
        => Assert.Equal(expected, GateSwitchPolicy.Series(path));
}
