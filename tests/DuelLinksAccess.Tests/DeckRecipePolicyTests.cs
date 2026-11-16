using Xunit;
namespace DuelLinksAccess.Tests;
public class DeckRecipePolicyTests
{
    [Fact]
    public void SummaryKeepsEachRowsActualNameSkillProgressAndDate()
    {
        Assert.Equal("KC盃 #2，無限誕生，0%，10/2 15:00", DeckRecipePolicy.Summary("KC盃 #2", "", "無限誕生", "0%", "10/2 15:00"));
        Assert.NotEqual(DeckRecipePolicy.Summary("KC盃 #1", "技能甲"), DeckRecipePolicy.Summary("KC盃 #2", "技能乙"));
    }
    [Fact]
    public void EmptyAndDuplicateFieldsDoNotCreateInventedInformation()
    {
        Assert.Equal("0%", DeckRecipePolicy.Summary(null, "", "0%", "0%"));
        Assert.Equal("", DeckRecipePolicy.Summary(null, " "));
    }
}
