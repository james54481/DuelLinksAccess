using Xunit;

namespace DuelLinksAccess.Tests;

public sealed class AttackFallbackPolicyTests
{
    [Theory]
    [InlineData(true, 42, 42, false)]
    [InlineData(true, -42, -42, false)]
    [InlineData(true, 42, 99, true)]
    [InlineData(true, 42, 0, true)]
    [InlineData(true, 0, 42, true)]
    [InlineData(true, 0, 0, true)]
    [InlineData(false, 0, 0, false)]
    public void OnlyTheExactCurrentDuelReceiverMayPassCanvasBlocking(
        bool canvas, int duelProvider, int hitProvider, bool expected)
        => Assert.Equal(expected, AttackFallbackPolicy.IsBlockingCanvasHit(canvas, duelProvider, hitProvider));

    [Theory]
    [InlineData(0, 3, 0, 2, 0, 3, 0, 2, true)]
    [InlineData(0, 3, 0, 2, 1, 3, 0, 2, false)]
    [InlineData(0, 3, 0, 2, 0, 2, 0, 2, false)]
    [InlineData(0, 3, 0, 2, 0, 3, 1, 2, false)]
    [InlineData(0, 3, 0, 2, 0, 3, 0, 8, false)]
    [InlineData(0, 3, 0, 0, 0, 3, 0, 0, false)]
    public void NativeMenuCannotSupplyCommandsForAnotherCardOrStaleOccupant(
        int player, int locate, int index, int uid, int mp, int ml, int mi, int actualUid, bool expected)
        => Assert.Equal(expected, AttackFallbackPolicy.MatchesNativeMenu(player, locate, index, uid, mp, ml, mi, actualUid));

    [Theory]
    [InlineData(400, 300, 1, true)]
    [InlineData(0, 300, 1, false)]
    [InlineData(800, 300, 1, false)]
    [InlineData(400, -1, 1, false)]
    [InlineData(400, 600, 1, false)]
    [InlineData(400, 300, -1, false)]
    [InlineData(float.NaN, 300, 1, false)]
    [InlineData(400, float.PositiveInfinity, 1, false)]
    public void PointerFallbackRejectsOffscreenBehindCameraAndInvalidPoints(float x, float y, float depth, bool expected)
        => Assert.Equal(expected, AttackFallbackPolicy.IsVisibleClickPoint(x, y, depth, 800, 600));

    [Theory]
    [InlineData(true, true, 19842, 19842, 3, 3, true)]
    [InlineData(false, true, 19842, 19842, 3, 3, false)]
    [InlineData(true, false, 19842, 19842, 3, 3, false)]
    [InlineData(true, true, 19842, 4041, 3, 3, false)]
    [InlineData(true, true, 19842, 19842, 3, 4, false)]
    [InlineData(true, true, 19842, 19842, 0, 0, false)]
    public void FieldTapRequiresSameKnownLocalCardAndOpenInput(
        bool local, bool open, int expectedId, int actualId, int expectedUid, int actualUid, bool expected)
        => Assert.Equal(expected, AttackFallbackPolicy.CanRetryFieldTap(
            local, open, expectedId, actualId, expectedUid, actualUid));

    [Theory]
    [InlineData(false, true, false, 128, false)]
    [InlineData(false, true, true, 128, false)]
    [InlineData(true, true, false, 128, false)]
    [InlineData(true, false, true, 128, false)]
    [InlineData(true, null, true, 128, false)]
    [InlineData(true, true, null, 128, false)]
    [InlineData(true, true, true, 0, false)]
    [InlineData(true, true, true, 128, true)]
    public void AttackFallbackRequiresKnownAttackPositionAndBattlePhase(
        bool battle, bool? faceUp, bool? attack, int mask, bool expected)
        => Assert.Equal(expected, AttackFallbackPolicy.CanOfferAttack(battle, faceUp, attack, mask));

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, false)]
    public void ShouldRetry_RequiresFailedGestureAndActiveBattleInput(
        bool autoAttack, bool battleInputActive, bool expected)
    {
        Assert.Equal(expected, AttackFallbackPolicy.ShouldRetry(
            autoAttack, battleInputActive));
    }
}
