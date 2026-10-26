using System.IO;
using Xunit;
namespace DuelLinksAccess.Tests;
public sealed class PhaseAdvancePolicyTests
{
    [Theory]
    [InlineData(true, false, 3, "Main1", true, false, true)]
    [InlineData(false, false, 3, "Main1", true, false, false)]
    [InlineData(true, true, 3, "Main1", true, false, false)]
    [InlineData(true, false, 4, "Main1", true, false, false)]
    [InlineData(true, false, 3, "Battle", true, false, false)]
    [InlineData(true, false, 3, "Main1", false, false, false)]
    [InlineData(true, false, 3, "Main1", true, true, false)]
    public void MenuCancellationMustNotDispatchIntoChangedTurnPhaseOrSelection(
        bool inDuel, bool ended, int turn, string phase, bool open, bool selecting, bool expected)
        => Assert.Equal(expected, PhaseAdvancePolicy.CanContinueAfterMenuCancellation(
            inDuel, ended, 3, turn, "Main1", phase, open, selecting));

    [Theory]
    [InlineData("Main1", true, true, PhaseAdvanceTarget.Battle)]
    [InlineData("Main1", false, true, PhaseAdvanceTarget.End)]
    [InlineData("Main2", false, true, PhaseAdvanceTarget.End)]
    [InlineData("Battle", true, true, PhaseAdvanceTarget.End)]
    [InlineData("Battle", true, false, PhaseAdvanceTarget.None)]
    [InlineData("Draw", true, true, PhaseAdvanceTarget.None)]
    [InlineData("End", true, true, PhaseAdvanceTarget.None)]
    [InlineData("Main1", false, false, PhaseAdvanceTarget.None)]
    public void SelectsOnlyAnAvailableForwardNativeAction(string phase, bool battle, bool end, PhaseAdvanceTarget expected)
        => Assert.Equal(expected, PhaseAdvancePolicy.Choose(phase,battle,end));
    [Fact]
    public void PhaseRouteDoesNotMixEngineCommandsAndNativeCallbacks()
    {
        string source = File.ReadAllText(Path.Combine(LocalizationCatalogTests.FindRepositoryRoot(),"src","DuelFieldNavigator.cs"));
        string phase = source[source.IndexOf("private void AdvancePhase()")..source.IndexOf("private static void DumpPhaseRelatedButtons()")];
        Assert.DoesNotContain("DLL_DuelComMovePhase(",phase);
        Assert.DoesNotContain("DLL_DuelComGetMovablePhase(",phase);
        Assert.DoesNotContain("_lastPhaseAttempt",phase);
        Assert.DoesNotContain("DuelMenu.ChangePhase",phase);
        Assert.Contains("_phaseRequestPending",phase);
        Assert.Contains("TryNativePhaseClick(out failed) || failed",phase);
        Assert.Contains("DuelState.TurnNumber != turn",phase);
        Assert.Contains("!button.interactable",phase);
    }
}
