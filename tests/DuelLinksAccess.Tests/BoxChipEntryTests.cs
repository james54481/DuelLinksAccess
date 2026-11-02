using Xunit;
namespace DuelLinksAccess.Tests;
public class BoxChipEntryTests
{
    [Theory]
    [InlineData(true,true,true,true)]
    [InlineData(false,true,true,false)]
    [InlineData(true,false,true,false)]
    [InlineData(true,true,false,false)]
    [InlineData(false,false,false,false)]
    public void PickerRequiresCorrectOfferAndNativeReadyButton(bool selected,bool active,bool ready,bool expected)
        => Assert.Equal(expected,TradeExecutionPolicy.CanOpenBoxChipPicker(selected,active,ready));
}
