using System.Diagnostics;
using Sproutoku.Core.Boards;
using Sproutoku.Core.Generation;
using Sproutoku.Core.Solving;
using static Sproutoku.Core.Solving.DifficultyGrade;

namespace Sproutoku.Core.Levels;

public enum GardenTier {
    Normal,

    /// <summary>
    /// Every 10th level after the intro: 9×9 or 10×10, Tricky or harder.
    /// </summary>
    Hard,

    /// <summary>
    /// Every 50th level: 10×10, Expert, one sun.
    /// </summary>
    SuperHard,
}

/// <summary>
/// What a level's garden should be: its size, the grades it may have, its tier, and how many suns it starts with.
/// </summary>
public sealed record LevelRecipe(int Size, DifficultyGrade Easiest, DifficultyGrade Hardest, GardenTier Tier, int Suns) {
    public bool IsOneSun => Suns == 1;
}

/// <summary>
/// The level plan.
/// Levels 1–30 introduce one deduction at a time. After that, sizes are a weighted random pick that slowly favors bigger gardens,
/// grades vary from Gentle to Tricky, and Hard and Super hard levels come at regular milestones.
/// Every choice is drawn from the level's own random sequence, so a level is the same for every player.
/// </summary>
public static class LevelPlan {
    public const int IntroLevels = 30;
    public const int HardEvery = 10;
    public const int SuperHardEvery = 50;

    /// <summary>
    /// After the intro, 1 in this many regular levels gives one sun.
    /// </summary>
    public const int OneSunChance = 12;

    // (size, weight): a size's chance is its weight out of the total.
    private static readonly (int Size, int Weight)[] Learning = [(5, 7), (6, 3)];                                      // Levels 4–10
    private static readonly (int Size, int Weight)[] Growing = [(5, 3), (6, 4), (7, 3)];                               // Levels 11–20
    private static readonly (int Size, int Weight)[] Branching = [(5, 15), (6, 30), (7, 35), (8, 20)];                 // Levels 21–30
    private static readonly (int Size, int Weight)[] Early = [(5, 10), (6, 20), (7, 30), (8, 25), (9, 15)];            // Levels 31–60
    private static readonly (int Size, int Weight)[] Middle = [(5, 5), (6, 15), (7, 25), (8, 25), (9, 20), (10, 10)];  // Levels 61–120
    private static readonly (int Size, int Weight)[] Late = [(5, 5), (6, 10), (7, 20), (8, 25), (9, 20), (10, 20)];    // Levels 121+
    private static readonly (int Size, int Weight)[] HardSizes = [(9, 1), (10, 1)];

    /// <summary>
    /// The recipe for a level. Pass the level's random sequence; the generator keeps drawing from it afterwards.
    /// </summary>
    public static LevelRecipe For(int level, SeededRandom random) {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        ArgumentNullException.ThrowIfNull(random);

        return level switch {
            // The intro. Levels 4, 11 and 21 are each new deduction's first garden; 18 and 27 are its two One sun levels.
            <= 3 => Normal(5, Gentle, Gentle),
            4 => Normal(5, Easy, Easy),
            <= 10 => Normal(Pick(Learning, random), Gentle, Easy),
            11 => Normal(Pick(Growing, random), Medium, Medium),
            18 => OneSun(Pick(Learning, random), Easy, Easy),
            <= 20 => Normal(Pick(Growing, random), Easy, Medium),
            21 => Normal(Pick(Branching, random), Tricky, Tricky),
            27 => OneSun(Pick(Growing, random), Easy, Medium),
            <= IntroLevels => Normal(Pick(Branching, random), Easy, Tricky),

            // After the intro.
            _ when level % SuperHardEvery == 0 => new LevelRecipe(10, Expert, Expert, GardenTier.SuperHard, Suns: 1),
            _ when level % HardEvery == 0 => new LevelRecipe(Pick(HardSizes, random), Tricky, Expert, GardenTier.Hard, Board.DefaultSuns),
            _ => Regular(level, random),
        };
    }

    private static LevelRecipe Regular(int level, SeededRandom random) {
        var sizes = level <= 60 ? Early : level <= 120 ? Middle : Late;
        int size = Pick(sizes, random);
        int suns = random.Next(OneSunChance) == 0 ? 1 : Board.DefaultSuns;
        return new LevelRecipe(size, Gentle, Tricky, GardenTier.Normal, suns);
    }

    private static LevelRecipe Normal(int size, DifficultyGrade easiest, DifficultyGrade hardest) =>
        new(size, easiest, hardest, GardenTier.Normal, Board.DefaultSuns);

    private static LevelRecipe OneSun(int size, DifficultyGrade easiest, DifficultyGrade hardest) =>
        new(size, easiest, hardest, GardenTier.Normal, Suns: 1);

    // Rolls a number below the total weight, then walks the list until the roll lands inside a size's share.
    private static int Pick((int Size, int Weight)[] options, SeededRandom random) {
        int roll = random.Next(options.Sum(option => option.Weight));
        foreach (var (size, weight) in options) {
            if (roll < weight) return size;
            roll -= weight;
        }

        throw new UnreachableException();
    }
}
