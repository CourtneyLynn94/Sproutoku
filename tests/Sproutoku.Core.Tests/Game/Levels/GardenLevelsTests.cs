using Sproutoku.Core.Game.Levels;
using Sproutoku.Core.Gardens.Generation;
using Sproutoku.Core.Gardens.Solving;

namespace Sproutoku.Core.Tests.Game.Levels;

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

    [Fact]
    public void NoLevel_LooksLikeTheFirstChoicesOfTheFiveBeforeIt_AndSomeMovedToABackup() {
        int moved = 0;

        for (int level = 2; level <= 100; level++) {
            var garden = GardenLevels.ForLevel(level);
            for (int earlier = Math.Max(1, level - GardenLevels.LookAlikeNeighbors); earlier < level; earlier++) {
                Assert.False(
                    GardenLikeness.LookAlike(garden.Puzzle, GardenLevels.FirstChoiceFor(earlier).Puzzle),
                    $"Level {level} looks like level {earlier}.");
            }

            if (garden.Fingerprint != GardenLevels.FirstChoiceFor(level).Fingerprint) moved++;
        }

        // The check really is at work: some first choices looked like a neighbor and were replaced.
        Assert.True(moved > 0);
    }

    [Fact]
    public void TheFirst200Levels_AreAllDifferentGardens() {
        AssertAllDifferent(200);
    }

    [Fact]
    [Trait("Speed", "Slow")]
    public void TheFirst2000Levels_AreAllDifferentGardens() {
        // Slow (it grows 2,000 gardens), so CI skips it. Run it by hand before a release.
        AssertAllDifferent(2000);
    }

    private static void AssertAllDifferent(int levels) {
        var seen = new Dictionary<string, int>();
        for (int level = 1; level <= levels; level++) {
            string fingerprint = GardenLevels.ForLevel(level).Fingerprint;
            Assert.False(seen.TryGetValue(fingerprint, out int earlier), $"Level {level} repeats level {earlier}.");
            seen[fingerprint] = level;
        }
    }
}
