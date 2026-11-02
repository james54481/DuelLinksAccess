using Xunit;
namespace DuelLinksAccess.Tests;
public class BannerDuplicatePolicyTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void CoverRequiresVerifiedIdentityAndAnotherUsableExit(bool native, bool exit, bool hidden)
        => Assert.Equal(hidden, BannerDuplicatePolicy.HideCover(native, exit));
    [Fact] public void CarouselWrapperDoesNotMakeListCloneDifferent()
    {
        Assert.Equal(BannerDuplicatePolicy.ArtKey(new[] { "GUI_T_HomeBannerBg1(Clone)", "productA" }, new[] { "card1" }),
            BannerDuplicatePolicy.ArtKey(new[] { "productA" }, new[] { "card1" }));
    }
    [Fact] public void SameCardsWithDifferentPromotionArtRemainDistinct()
    {
        Assert.NotEqual(BannerDuplicatePolicy.ArtKey(new[] { "productA" }, new[] { "card1" }),
            BannerDuplicatePolicy.ArtKey(new[] { "productB" }, new[] { "card1" }));
        Assert.Null(BannerDuplicatePolicy.ArtKey(new[] { "GUI_T_HomeBannerBg1(Clone)" }, new string[0]));
    }
    [Fact] public void OnlyNativeLoopTailCopiesAreHidden()
    {
        string[] art = { "a", "b", "c", "d", "a", "d" };
        Assert.True(BannerDuplicatePolicy.IsLoopCopy(true, 4, 4, art));
        Assert.True(BannerDuplicatePolicy.IsLoopCopy(true, 4, 5, art));
        Assert.False(BannerDuplicatePolicy.IsLoopCopy(true, 4, 0, art));
        Assert.False(BannerDuplicatePolicy.IsLoopCopy(false, 4, 4, art));
    }
    [Fact] public void DifferentOrUnknownArtAndUnexpectedLayoutsArePreserved()
    {
        Assert.False(BannerDuplicatePolicy.IsLoopCopy(true, 2, 2, new[] { "same cards;art1", "b", "same cards;art2", "b" }));
        Assert.False(BannerDuplicatePolicy.IsLoopCopy(true, 2, 2, new[] { "a", "b", "", "b" }));
        Assert.False(BannerDuplicatePolicy.IsLoopCopy(true, 2, 2, new[] { "a", "b", "a" }));
    }
}
