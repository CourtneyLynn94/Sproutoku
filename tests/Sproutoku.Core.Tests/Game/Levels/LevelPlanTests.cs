using Sproutoku.Core.Game.Levels;
using Sproutoku.Core.Gardens.Generation;
using Sproutoku.Core.Gardens.Solving;
using static Sproutoku.Core.Gardens.Solving.DifficultyGrade;

namespace Sproutoku.Core.Tests.Game.Levels;

public class LevelPlanTests {
    // Recipes for levels 1–2,000, each from a seed made from its level number.
    private static readonly LevelRecipe[] Recipes = Enumerable.Range(1, 2000)
        .Select(level => LevelPlan.For(level, new SeededRandom(SeededRandom.SeedFrom(level))))
        .ToArray();

    private static LevelRecipe Level(int level) => Recipes[level - 1];

    // Regular levels after the intro: not Hard or Super hard.
    private static List<int> RegularLevels(int from, int to) =>
        Enumerable.Range(from, to - from + 1).Where(level => level % LevelPlan.HardEvery != 0).ToList();

    private static double Share(List<int> levels, Func<LevelRecipe, bool> matches) =>
        levels.Count(level => matches(Level(level))) / (double)levels.Count;

    #region Every level
    [Fact]
    public void EveryLevel_HasAPlayableRecipe() {
        Assert.All(Recipes, recipe => {
            Assert.InRange(recipe.Size, 5, 10);
            Assert.True(recipe.Easiest <= recipe.Hardest);
            Assert.Contains(recipe.Suns, new[] { 1, Board.DefaultSuns });
            Assert.True(recipe.MaxOneCellBeds >= 0);
        });
    }

    [Fact]
    public void TheSameSeed_GivesTheSameRecipe() {
        Assert.Equal(LevelPlan.For(77, new SeededRandom(5)), LevelPlan.For(77, new SeededRandom(5)));
    }

    [Fact]
    public void ALevelsGrade_DoesntDependOnItsSeed() {
        // The block's mix decides a level's grade, breather and suns, so a backup seed only changes its size and garden.
        for (int level = LevelPlan.IntroLevels + 1; level <= 200; level++) {
            var first = LevelPlan.For(level, new SeededRandom(1));
            var backup = LevelPlan.For(level, new SeededRandom(2));

            Assert.Equal((first.Easiest, first.Hardest, first.Suns, first.IsBreather), (backup.Easiest, backup.Hardest, backup.Suns, backup.IsBreather));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void LevelsBelowOne_Throw(int level) {
        Assert.Throws<ArgumentOutOfRangeException>(() => LevelPlan.For(level, new SeededRandom(1)));
    }
    #endregion

    #region The intro
    [Fact]
    public void TheFirstTwoLevels_AreGentle5x5Gardens() {
        for (int level = 1; level <= 2; level++) {
            Assert.Equal(new LevelRecipe(5, Gentle, Gentle, GardenTier.Normal, Board.DefaultSuns), Level(level));
        }
    }

    [Fact]
    public void EachNewDeduction_GetsAGuaranteedFirstGarden() {
        Assert.Equal((Easy, Easy), (Level(3).Easiest, Level(3).Hardest));
        Assert.Equal((Medium, Medium), (Level(7).Easiest, Level(7).Hardest));
        Assert.Equal((Tricky, Tricky), (Level(14).Easiest, Level(14).Hardest));
    }

    [Fact]
    public void TheIntro_RampsUpGently_WithNoHardLevels() {
        for (int level = 1; level <= LevelPlan.IntroLevels; level++) {
            var recipe = Level(level);
            Assert.Equal(GardenTier.Normal, recipe.Tier);
            Assert.Equal(GardenGenerator.AnyOneCellBeds, recipe.MaxOneCellBeds);

            var (biggest, hardest) = level switch {
                <= 6 => (6, Easy),
                <= 13 => (7, Medium),
                _ => (8, Tricky),
            };
            Assert.True(recipe.Size <= biggest, $"Level {level} is {recipe.Size}×{recipe.Size}.");
            Assert.True(recipe.Hardest <= hardest, $"Level {level} allows {recipe.Hardest}.");

            // Not too easy: after level 6, no Gentle gardens.
            if (level > 6) Assert.True(recipe.Easiest >= Easy, $"Level {level} allows Gentle.");
        }
    }

    [Fact]
    public void TheIntro_HasExactlyTwoOneSunLevels() {
        var oneSun = Enumerable.Range(1, LevelPlan.IntroLevels).Where(level => Level(level).IsOneSun);

        Assert.Equal([11, 18], oneSun);
    }
    #endregion

    #region Milestones
    [Fact]
    public void HardLevels_ComeEveryTenth_AndSuperHardEveryFiftieth_AfterTheIntro() {
        for (int level = LevelPlan.IntroLevels + 1; level <= 2000; level++) {
            var recipe = Level(level);

            if (level % 50 == 0) {
                Assert.Equal(GardenTier.SuperHard, recipe.Tier);
                Assert.Equal((10, Expert, Expert, 1), (recipe.Size, recipe.Easiest, recipe.Hardest, recipe.Suns));
            } else if (level % 10 == 0) {
                Assert.Equal(GardenTier.Hard, recipe.Tier);
                Assert.True(recipe.Size >= 9);
                Assert.Equal((Tricky, Expert), (recipe.Easiest, recipe.Hardest));
                Assert.Equal(Board.DefaultSuns, recipe.Suns);
            } else {
                Assert.Equal(GardenTier.Normal, recipe.Tier);
                Assert.Equal(recipe.Easiest, recipe.Hardest); // Each regular level has one grade from its block's mix.
            }
        }
    }

    [Fact]
    public void TheFirstHardLevel_IsLevel30() {
        Assert.Equal(30, Enumerable.Range(1, 2000).First(level => Level(level).Tier == GardenTier.Hard));
    }
    #endregion

    #region Grades after the intro
    [Fact]
    public void AfterTheIntro_ThereAreNoGentleGardens_AndEasyOnlyUntilLevel50() {
        Assert.All(RegularLevels(LevelPlan.IntroLevels + 1, 2000), level => {
            var floor = level <= LevelPlan.EasyBreathersUntil ? Easy : Medium;
            Assert.True(Level(level).Easiest >= floor, $"Level {level} allows {Level(level).Easiest}.");
        });
    }

    [Fact]
    public void Breathers_AreTheOnlyLevelsAtTheFloor() {
        Assert.All(RegularLevels(LevelPlan.IntroLevels + 1, 2000), level => {
            var floor = level <= LevelPlan.EasyBreathersUntil ? Easy : Medium;
            Assert.Equal(Level(level).IsBreather, Level(level).Easiest == floor);
        });
    }

    [Fact]
    public void EachBlock_HasOneBreather_InItsFirstFiveLevels() {
        for (int block = 2; block < 200; block++) {
            var breathers = Enumerable.Range(block * 10 + 1, 9).Where(level => Level(level).IsBreather).ToList();

            int breather = Assert.Single(breathers);
            Assert.InRange(breather % 10, 1, LevelPlan.BreatherSlots);
        }
    }

    [Fact]
    public void AnyFiveRegularLevelsInARow_HaveAtMostOneBreather() {
        var levels = RegularLevels(LevelPlan.IntroLevels + 1, 2000);

        for (int start = 0; start + 5 <= levels.Count; start++) {
            var window = levels.Skip(start).Take(5).ToList();
            Assert.True(window.Count(level => Level(level).IsBreather) <= 1, $"Levels {window[0]}–{window[^1]} have two breathers.");
        }
    }

    [Fact]
    public void Medium_IsUncommon_AfterLevel50() {
        double medium = Share(RegularLevels(51, 2000), recipe => recipe.Easiest == Medium);

        Assert.InRange(medium, 0.05, 0.15); // One breather in each block's nine regular levels: about 11%.
    }

    [Fact]
    public void TrickyAndExpert_GetMoreCommon_AsLevelsClimb() {
        static bool Hard(LevelRecipe recipe) => recipe.Easiest >= Tricky;

        double early = Share(RegularLevels(21, 50), Hard);
        double middle = Share(RegularLevels(51, 150), Hard);
        double late = Share(RegularLevels(151, 2000), Hard);
        double lateExpert = Share(RegularLevels(151, 2000), recipe => recipe.Easiest == Expert);

        Assert.True(early < middle, $"Tricky or harder: {early:P0} early vs {middle:P0} in the middle.");
        Assert.True(middle <= late, $"Tricky or harder: {middle:P0} in the middle vs {late:P0} late.");
        Assert.InRange(lateExpert, 0.15, 0.30); // Two Expert levels in each block's nine regular levels.
    }
    #endregion

    #region One sun
    [Fact]
    public void EachBlock_HasOneOneSunLevel_ThatIsntTheBreather() {
        for (int block = 2; block < 200; block++) {
            var oneSun = Enumerable.Range(block * 10 + 1, 9).Where(level => Level(level).IsOneSun).ToList();

            int level = Assert.Single(oneSun);
            Assert.False(Level(level).IsBreather);
            Assert.Contains(Level(level).Easiest, new[] { Medium, Tricky });
        }
    }

    [Fact]
    public void TheLevelBeforeASuperHardLevel_IsNeverOneSun() {
        for (int level = 99; level <= 2000; level += 50) {
            Assert.False(Level(level).IsOneSun, $"Level {level} is One sun.");
        }
    }
    #endregion

    #region One-cell beds
    [Fact]
    public void OneCellBeds_AreLimited_AfterTheIntro() {
        for (int level = LevelPlan.IntroLevels + 1; level <= 2000; level++) {
            var recipe = Level(level);
            int expected = level <= LevelPlan.EasyBreathersUntil ? LevelPlan.EarlyOneCellBeds
                : recipe.IsBreather ? LevelPlan.BreatherOneCellBeds
                : LevelPlan.LaterOneCellBeds;

            Assert.Equal(expected, recipe.MaxOneCellBeds);
        }
    }
    #endregion

    #region Sizes
    [Fact]
    public void AfterTheIntro_EverySizeStillAppears() {
        var sizes = RegularLevels(200, 1200).Select(level => Level(level).Size).ToHashSet();

        Assert.Equal([5, 6, 7, 8, 9, 10], sizes.Order());
        Assert.Contains(RegularLevels(151, 2000), level => Level(level).Size == 5); // Small gardens never disappear.
    }

    [Fact]
    public void BiggerGardens_GetMoreLikely_AsLevelsClimb() {
        double early = RegularLevels(21, 50).Average(level => Level(level).Size);
        double late = RegularLevels(151, 1000).Average(level => Level(level).Size);

        Assert.True(early < late, $"Average size {early:0.0} early vs {late:0.0} later.");
    }
    #endregion

    #region Growing
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
    #endregion
}
