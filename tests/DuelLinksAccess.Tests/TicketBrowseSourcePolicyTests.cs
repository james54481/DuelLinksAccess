using Xunit;
namespace DuelLinksAccess.Tests;
public class TicketBrowseSourcePolicyTests
{
    [Theory]
    [InlineData(4840,0,TicketBrowseSource.Choice)]
    [InlineData(4840,1,TicketBrowseSource.Choice)]
    [InlineData(4839,1,TicketBrowseSource.Choice)]
    [InlineData(0,1,TicketBrowseSource.None)]
    [InlineData(0,0,TicketBrowseSource.None)]
    public void BoxChipKeepsCatalogueAfterSelectionCancelOrExchange(int choices,int pending,TicketBrowseSource expected)
        => Assert.Equal(expected,TicketBrowseSourcePolicy.Choose("BoxChip",choices,pending));
    [Theory]
    [InlineData("RespectOrb",4,1,TicketBrowseSource.Exchange)]
    [InlineData("Ticket",4,1,TicketBrowseSource.Exchange)]
    [InlineData("Ticket",4,0,TicketBrowseSource.Choice)]
    [InlineData("Ticket",0,0,TicketBrowseSource.None)]
    public void OtherModesKeepExistingSourcePriority(string mode,int choices,int exchanges,TicketBrowseSource expected)
        => Assert.Equal(expected,TicketBrowseSourcePolicy.Choose(mode,choices,exchanges));
}
