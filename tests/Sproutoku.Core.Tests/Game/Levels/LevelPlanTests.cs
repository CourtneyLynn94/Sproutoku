using Sproutoku.Core.Boards;
using Sproutoku.Core.Generation;
using Sproutoku.Core.Levels;
using static Sproutoku.Core.Solving.DifficultyGrade;

namespace Sproutoku.Core.Tests.Levels;

public class LevelPlanTests {
    // Recipes for levels 1–2,000, each from a seed made from its level number.
    private static readonly LevelRecipe[] Recipes = Enumerable.Range(1, 2000)
        .Select(level => LevelPlan.For(level, new SeededRandom(SeededRandom.SeedFrom(level))))
        .ToArray();

    private static LevelRecipe Level(int level) => Recipes[level - 1];

    private static IEnumerable<int> RegularLevels(int from, int to) =>
        Enumerable.Range(from, to - from + 1).Where(level => level % LevelPlan.HardEvery != 0);

    [Fact]
    public void EveryLevel_HasAPlayableRecipe() {
        Assert.All(Recipes, recipe => {
            Assert.InRange(recipe.Size, 5, 10);
            Assert.True(recipe.Easiest <= recipe.Hardest);
            Assert.Contains(recipe.Suns, new[] { 1, Board.DefaultSuns });
        });
    }

    [Fact]
    public void TheSameSeed_GivesTheSameRecipe() {
        Assert.Equal(LevelPlan.For(77, new SeededRandom(5)), LevelPlan.For(77, new SeededRandom(5)));
    }

    [Fact]
    public void TheFirstThreeLevels_AreGentle5x5Gardens() {
        for (int level = 1; level <= 3; level++) {
            Assert.Equal(new LevelRecipe(5, Gentle, Gentle, GardenTier.Normal, Board.DefaultSuns), Level(level));
        }
    }

    [Fact]
    public void EachNewDeduction_GetsAGuaranteedFirstGarden() {
        Assert.Equal((Easy, Easy), (Level(4).Easiest, Level(4).Hardest));
        Assert.Equal((Medium, Medium), (Level(11).Easiest, Level(11).Hardest));
        Assert.Equal((Tricky, Tricky), (Level(21).Easiest, Level(21).Hardest));
    }

    [Fact]
    public void TheIntro_RampsUpGently_WithNoHardLevels() {
        for (int level = 1; level <= LevelPlan.IntroLevels; level++) {
            var recipe = Level(level);
            Assert.Equal(GardenTier.Normal, recipe.Tier);

            var (biggest, hardest) = level switch {
                <= 10 => (6, Easy),
                <= 20 => (7, Medium),
                _ => (8, Tricky),
            };
            Assert.True(recipe.Size <= biggest, $"Level {level} is {recipe.Size}×{recipe.Size}.");
            Assert.True(recipe.Hardest <= hardest, $"Level {level} allows {recipe.Hardest}.");

            // Not too easy: after the first ten levels, no Gentle gardens in the intro.
            if (level > 10) Assert.True(recipe.Easiest >= Easy, $"Level {level} allows Gentle.");
        }
    }

    [Fact]
    public void TheIntro_HasExactlyTwoOneSunLevels() {
        var oneSun = Enumerable.Range(1, LevelPlan.IntroLevels).Where(level => Level(level).IsOneSun);

        Assert.Equal([18, 27], oneSun);
    }

    [Fact]
    public void HardLevels_ComeEveryTenth_AndSuperHardEveryFiftieth_AfterTheIntro() {
        for (int level = LevelPlan.IntroLevels + 1; level <= 2000; level++) {
            var recipe = Level(level);

            if (level % 50 == 0) {
                Assert.Equal(new LevelRecipe(10, Expert, Expert, GardenTier.SuperHard, 1), recipe);
            } else if (level % 10 == 0) {
                Assert.Equal(GardenTier.Hard, recipe.Tier);
                Assert.True(recipe.Size >= 9);
                Assert.Equal(Tricky, recipe.Easiest);
                Assert.Equal(Board.DefaultSuns, recipe.Suns);
            } else {
                Assert.Equal(GardenTier.Normal, recipe.Tier);
                Assert.Equal((Gentle, Tricky), (recipe.Easiest, recipe.Hardest));
            }
        }
    }

    [Fact]
    public void AfterTheIntro_EverySizeStillAppears() {
        var sizes = RegularLevels(200, 1200).Select(level => Level(level).Size).ToHashSet();

        Assert.Equal([5, 6, 7, 8, 9, 10], sizes.Order());
        Assert.Contains(RegularLevels(121, 2000), level => Level(level).Size == 5); // Small gardens never disappear.
    }

    [Fact]
    public void BiggerGardens_GetMoreLikely_AsLevelsClimb() {
        double early = RegularLevels(31, 60).Average(level => Level(level).Size);
        double late = RegularLevels(121, 1000).Average(level => Level(level).Size);

        Assert.True(early < late, $"Average size {early:0.0} early vs {late:0.0} later.");
    }

    [Fact]
    public void OneSun_IsALowChance_AfterTheIntro() {
        var levels = RegularLevels(31, 2000).ToList();
        double share = levels.Count(level => Level(level).IsOneSun) / (double)levels.Count;

        Assert.InRange(share, 0.05, 0.12); // About 1 in 12 (8%).
    }

    [Fact]
    public void EveryIntroLevel_CanBeGrown_AtItsGrade() {
        for (int level = 1; level <= LevelPlan.IntroLevels; level++) {
            var random = new SeededRandom(SeededRandom.SeedFrom(level));
            var recipe = LevelPlan.For(level, random);
            var garden = GardenGenerator.Generate(recipe.Size, random, recipe.Easiest, recipe.Hardest);

            Assert.Equal(recipe.Size, garden.Puzzle.Size);
            Assert.InRange(garden.Grade, recipe.Easiest, recipe.Hardest);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void LevelsBelowOne_Throw(int level) {
        Assert.Throws<ArgumentOutOfRangeException>(() => LevelPlan.For(level, new SeededRandom(1)));
    }
}
