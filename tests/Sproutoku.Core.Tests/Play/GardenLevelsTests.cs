using Sproutoku.Core.Play;
using Sproutoku.Core.Solving;
using Sproutoku.Core.Tests.Puzzles;

namespace Sproutoku.Core.Tests.Play;

public class GardenLevelsTests {
    [Theory]
    [InlineData(1)]
    [InlineData(18)]
    [InlineData(40)]
    public void ALevel_IsTheSameGardenEveryTime(int level) {
        var first = GardenLevels.ForLevel(level);
        var second = GardenLevels.ForLevel(level);

        Assert.Equal(first.Seed, second.Seed);
        Assert.Equal(first.Recipe, second.Recipe);
        Assert.Equal(TestGardens.BedRows(first.Puzzle), TestGardens.BedRows(second.Puzzle));
    }

    [Fact]
    public void IntroLevels_FollowThePlan() {
        for (int level = 1; level <= LevelPlan.IntroLevels; level++) {
            var garden = GardenLevels.ForLevel(level);

            Assert.Equal(garden.Recipe.Size, garden.Puzzle.Size);
            Assert.InRange(garden.Garden.Grade, garden.Recipe.Easiest, garden.Recipe.Hardest);
            Assert.Equal(garden.Recipe.Suns, garden.Start().StartingSuns);
        }
    }

    [Fact]
    public void EarlyLevels_AreAllDifferentGardens() {
        var gardens = Enumerable.Range(1, 10)
            .Select(level => string.Join("/", TestGardens.BedRows(GardenLevels.ForLevel(level).Puzzle)))
            .ToHashSet();

        Assert.Equal(10, gardens.Count);
    }

    [Fact]
    public void ASuperHardLevel_GrowsAnExpertGarden_WithOneSun() {
        var garden = GardenLevels.ForLevel(50);

        Assert.Equal(GardenTier.SuperHard, garden.Recipe.Tier);
        Assert.Equal(DifficultyGrade.Expert, garden.Garden.Grade);
        Assert.Equal(1, garden.Start().StartingSuns);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void LevelsBelowOne_Throw(int level) {
        Assert.Throws<ArgumentOutOfRangeException>(() => GardenLevels.ForLevel(level));
        Assert.Throws<ArgumentOutOfRangeException>(() => GardenLevels.SeedFor(level));
    }
}
