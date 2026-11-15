using System.Diagnostics;
using Sproutoku.Core.Game.Boards;
using Sproutoku.Core.Gardens.Generation;
using Sproutoku.Core.Gardens.Solving;
using static Sproutoku.Core.Gardens.Solving.DifficultyGrade;

namespace Sproutoku.Core.Game.Levels;

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
/// What a level's garden should be: its size, the grades it may have, its tier, how many suns it starts with,
/// how many one-cell beds it may have, and whether it's a breather (a lighter level between harder ones).
/// </summary>
public sealed record LevelRecipe(
    int Size,
    DifficultyGrade Easiest,
    DifficultyGrade Hardest,
    GardenTier Tier,
    int Suns,
    int MaxOneCellBeds = GardenGenerator.AnyOneCellBeds,
    bool IsBreather = false) {
    public bool IsOneSun => Suns == 1;
}

/// <summary>
/// The level plan.
/// Levels 1–20 introduce one deduction at a time. After that, levels come in blocks of ten: nine regular levels, then a Hard one
/// (every 50th is Super hard instead). Each block's nine regular levels are a shuffled mix of grades that gets harder as levels climb,
/// with exactly one breather (the lightest grade allowed at that point) and one One sun level.
/// A regular level's size is picked to suit its grade, so small gardens still turn up.
/// The block's mix comes from the block's own seed and a level's size from the level's random sequence, so a level is the same for every player.
/// </summary>
public static class LevelPlan {
    public const int IntroLevels = 20;
    public const int HardEvery = 10;
    public const int SuperHardEvery = 50;

    /// <summary>
    /// Up to this level, the breather is Easy and gardens may have two one-cell beds. After it, the breather is Medium,
    /// a breather may have one one-cell bed and every other garden has none.
    /// </summary>
    public const int EasyBreathersUntil = 50;

    /// <summary>
    /// The breather is one of the first this many levels of its block, so any five regular levels in a row have at most one.
    /// </summary>
    public const int BreatherSlots = 5;

    public const int EarlyOneCellBeds = 2;
    public const int BreatherOneCellBeds = 1;
    public const int LaterOneCellBeds = 0;

    // Keeps block seeds apart from level seeds, which are made from (version, level).
    private const long BlockSeedPart = -1;

    // Each block's regular levels besides its breather: eight grades, shuffled by the block's seed.
    private static readonly DifficultyGrade[] FirstMix = [Medium, Medium, Medium, Medium, Medium, Tricky, Tricky, Tricky];   // Levels 21–29
    private static readonly DifficultyGrade[] EarlyMix = [Medium, Medium, Medium, Tricky, Tricky, Tricky, Tricky, Tricky];   // Levels 31–49
    private static readonly DifficultyGrade[] MiddleMix = [Tricky, Tricky, Tricky, Tricky, Tricky, Tricky, Tricky, Expert];  // Levels 51–149
    private static readonly DifficultyGrade[] LateMix = [Tricky, Tricky, Tricky, Tricky, Tricky, Tricky, Expert, Expert];    // Levels 151+

    // (size, weight): a size's chance is its weight out of the total.
    // The intro.
    private static readonly (int Size, int Weight)[] Learning = [(5, 7), (6, 3)];                       // Levels 4–6 and 11
    private static readonly (int Size, int Weight)[] Growing = [(5, 3), (6, 4), (7, 3)];                // Levels 7–13 and 18
    private static readonly (int Size, int Weight)[] Branching = [(6, 30), (7, 40), (8, 30)];           // Levels 14–20

    // After the intro, by grade. Tricky is rare at 5×5 and 6×6, and Expert below 9×9, so those sizes are left out or kept small.
    private static readonly (int Size, int Weight)[] EasySizes = [(5, 25), (6, 35), (7, 30), (8, 10)];
    private static readonly (int Size, int Weight)[] MediumSizes = [(5, 10), (6, 25), (7, 30), (8, 20), (9, 15)];
    private static readonly (int Size, int Weight)[] TrickySizes = [(6, 5), (7, 20), (8, 30), (9, 25), (10, 20)];
    private static readonly (int Size, int Weight)[] ExpertSizes = [(9, 1), (10, 2)];
    private static readonly (int Size, int Weight)[] HardSizes = [(9, 1), (10, 1)];

    /// <summary>
    /// The recipe for a level. Pass the level's random sequence; the generator keeps drawing from it afterwards.
    /// </summary>
    public static LevelRecipe For(int level, SeededRandom random) {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        ArgumentNullException.ThrowIfNull(random);

        return level switch {
            // The intro. Levels 3, 7 and 14 are each new deduction's first garden; 11 and 18 are its two One sun levels.
            <= 2 => Normal(5, Gentle, Gentle),
            3 => Normal(5, Easy, Easy),
            <= 6 => Normal(Pick(Learning, random), Gentle, Easy),
            7 => Normal(Pick(Growing, random), Medium, Medium),
            11 => OneSun(Pick(Learning, random), Easy, Easy),
            <= 13 => Normal(Pick(Growing, random), Easy, Medium),
            14 => Normal(Pick(Branching, random), Tricky, Tricky),
            18 => OneSun(Pick(Growing, random), Easy, Medium),
            <= IntroLevels => Normal(Pick(Branching, random), Medium, Tricky),

            // After the intro.
            _ when level % SuperHardEvery == 0 => new LevelRecipe(10, Expert, Expert, GardenTier.SuperHard, Suns: 1, MaxOneCellBeds: OneCellBedsFor(level, breather: false)),
            _ when level % HardEvery == 0 => new LevelRecipe(Pick(HardSizes, random), Tricky, Expert, GardenTier.Hard, Board.DefaultSuns, MaxOneCellBeds: OneCellBedsFor(level, breather: false)),
            _ => Regular(level, random),
        };
    }

    private static LevelRecipe Regular(int level, SeededRandom random) {
        // Level 21 is the first slot of block 2, level 29 its last; level 30 is the block's Hard level.
        var slot = BlockMix(level / HardEvery)[level % HardEvery - 1];

        var sizes = slot.Grade switch {
            DifficultyGrade.Easy => EasySizes,
            DifficultyGrade.Medium => MediumSizes,
            DifficultyGrade.Tricky => TrickySizes,
            _ => ExpertSizes,
        };

        return new LevelRecipe(
            Pick(sizes, random),
            slot.Grade,
            slot.Grade,
            GardenTier.Normal,
            slot.IsOneSun ? 1 : Board.DefaultSuns,
            OneCellBedsFor(level, slot.IsBreather),
            slot.IsBreather);
    }

    // The nine regular levels of one block of ten, in order.
    private static Slot[] BlockMix(int block) {
        int firstLevel = block * HardEvery + 1;
        bool early = firstLevel <= EasyBreathersUntil;
        var mix = firstLevel <= IntroLevels + HardEvery ? FirstMix
            : early ? EarlyMix
            : firstLevel <= 150 ? MiddleMix
            : LateMix;

        var random = new SeededRandom(SeededRandom.SeedFrom(GardenGenerator.Version, BlockSeedPart, block));
        var grades = mix.ToList();
        random.Shuffle(grades);

        int breather = random.Next(BreatherSlots);
        grades.Insert(breather, early ? Easy : Medium);

        // One sun goes on a Medium or Tricky level that isn't the breather, and never on the level just before a Super hard one.
        bool endsInSuperHard = (block + 1) * HardEvery % SuperHardEvery == 0;
        var oneSunChoices = Enumerable.Range(0, grades.Count)
            .Where(index => index != breather)
            .Where(index => grades[index] is DifficultyGrade.Medium or DifficultyGrade.Tricky)
            .Where(index => !(endsInSuperHard && index == grades.Count - 1))
            .ToList();
        int oneSun = random.Pick(oneSunChoices);

        return grades.Select((grade, index) => new Slot(grade, index == breather, index == oneSun)).ToArray();
    }

    private static int OneCellBedsFor(int level, bool breather) =>
        level <= IntroLevels ? GardenGenerator.AnyOneCellBeds
        : level <= EasyBreathersUntil ? EarlyOneCellBeds
        : breather ? BreatherOneCellBeds
        : LaterOneCellBeds;

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

    private readonly record struct Slot(DifficultyGrade Grade, bool IsBreather, bool IsOneSun);
}
