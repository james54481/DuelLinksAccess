using Xunit;

namespace DuelLinksAccess.Tests;

public sealed class TicketExchangePolicyTests
{
    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(false, true, true, false)]
    public void NativeControlRequiresVisibleEnabledButton(bool present, bool active, bool enabled, bool expected)
        => Assert.Equal(expected, TicketExchangePolicy.IsUsable(present, active, enabled));

    [Theory]
    // decideButton present: it is the mode's real gate and wins outright.
    [InlineData(true, true, false, false, TicketExchangeAction.ConfirmViaDecide)]
    [InlineData(true, false, false, false, TicketExchangeAction.Reject)]
    // decideButton present-but-disabled is rejected even if exchangeButton
    // would be interactable — never confirm against the wrong control.
    [InlineData(true, false, true, true, TicketExchangeAction.Reject)]
    // decideButton absent: fall back to exchangeButton's own gate.
    [InlineData(false, false, true, true, TicketExchangeAction.InvokeExchangeButton)]
    [InlineData(false, false, true, false, TicketExchangeAction.Reject)]
    // Neither control available: refuse rather than blindly calling
    // DecideClicked() (the old bug that hard-errored the game).
    [InlineData(false, false, false, false, TicketExchangeAction.Reject)]
    public void ChooseAction_GatesOnActiveConfirmButton(
        bool decidePresent,
        bool decideInteractable,
        bool exchangePresent,
        bool exchangeInteractable,
        TicketExchangeAction expected)
    {
        Assert.Equal(expected, TicketExchangePolicy.ChooseAction(
            decidePresent, decideInteractable,
            exchangePresent, exchangeInteractable));
    }
}
