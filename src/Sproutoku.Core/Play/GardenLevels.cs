using System.Collections.Concurrent;
using Sproutoku.Core.Generation;
using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Play;

/// <summary>
/// A level's garden: the level, the seed it came from, its recipe from the level plan, and the generated garden;
/// and never looks like the first choices of the five levels before it.
/// </summary>
public sealed record LevelGarden(int Level, ulong Seed, LevelRecipe Recipe, GeneratedGarden Garden) {
    public Puzzle Puzzle => Garden.Puzzle;

    /// <summary>
    /// The garden's fingerprint: saved with the board so it only restores onto this garden.
    /// </summary>
    public string Fingerprint => GardenLikeness.Fingerprint(Puzzle);

    /// <summary>
    /// A fresh board with the level's own suns.
    /// </summary>
    public Board Start() => Board.Start(Puzzle, Recipe.Suns);
}

/// <summary>
/// Which garden each level plays, without end. Every level follows the level plan, and never looks like the five levels before it.
/// A level's garden comes only from its number, so it's the same for every player, on every device, in every build.
/// </summary>
public static class GardenLevels {
    /// <summary>
    /// Backup seeds to try after a level's own seed. They're made from the level's seed, so every player gets the same ones.
    /// </summary>
    public const int MaxBackups = 20;

    /// <summary>
    /// How many levels before this one a level must not look like.
    /// </summary>
    public const int LookAlikeNeighbors = 5;

    // First choices already worked out. Each one is compared against the next five levels, so it's kept.
    private static readonly ConcurrentDictionary<int, LevelGarden> FirstChoices = new();

    // Levels already chosen, so asking for one again (resuming, tests) doesn't grow it again.
    private static readonly ConcurrentDictionary<int, LevelGarden> Chosen = new();

    /// <summary>
    /// A level's seed.
    /// </summary>
    public static ulong SeedFor(int level) {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        return SeededRandom.SeedFrom(GardenGenerator.Version, level);
    }

    /// <summary>
    /// A level's garden: its first garden at the recipe's grade that doesn't look like the first choices of the five levels before it.
    /// Never fails: if every candidate looks like a neighbor, the level keeps its first choice.
    /// </summary>
    public static LevelGarden ForLevel(int level) {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        return Chosen.GetOrAdd(level, Choose);
    }

    private static LevelGarden Choose(int level) {
        var neighbors = Enumerable.Range(Math.Max(1, level - LookAlikeNeighbors), Math.Min(LookAlikeNeighbors, level - 1))
            .Select(FirstChoiceFor)
            .ToList();

        var first = FirstChoiceFor(level);
        if (!LooksLikeAny(first, neighbors)) {
            return first;
        }

        foreach (var candidate in Candidates(level).Skip(1)) {
            if (!LooksLikeAny(candidate, neighbors)) {
                return candidate;
            }
        }

        // Every candidate looked like a neighbor (never expected in practice), so the level still exists.
        return first;
    }

    /// <summary>
    /// A level's first choice: its first garden at the recipe's grade, before the look-alike check.
    /// Levels are compared with their neighbors' first choices, so no level depends on how the levels before it were chosen, and any level can be built on its own.
    /// </summary>
    public static LevelGarden FirstChoiceFor(int level) {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        return FirstChoices.GetOrAdd(level, key => Candidates(key).First());
    }

    private static bool LooksLikeAny(LevelGarden garden, List<LevelGarden> neighbors) =>
        neighbors.Any(neighbor => GardenLikeness.LookAlike(garden.Puzzle, neighbor.Puzzle));

    // A level's gardens in order: one for each of its seeds (its own, then its backups) that reaches the recipe's grade,
    // then a last resort of any grade, so there's always at least one.
    private static IEnumerable<LevelGarden> Candidates(int level) {
        ulong seed = SeedFor(level);

        for (int backup = 0; backup <= MaxBackups; backup++) {
            ulong gardenSeed = backup == 0 ? seed : SeededRandom.SeedFrom(unchecked((long)seed), backup);
            var random = new SeededRandom(gardenSeed);
            var recipe = LevelPlan.For(level, random);

            if (GardenGenerator.TryGenerate(recipe.Size, random, recipe.Easiest, recipe.Hardest) is { } garden) {
                yield return new LevelGarden(level, seed, recipe, garden);
            }
        }

        // Last resort, never expected in practice: the recipe's size and suns, but any grade.
        var lastRandom = new SeededRandom(seed);
        var lastRecipe = LevelPlan.For(level, lastRandom);
        yield return new LevelGarden(level, seed, lastRecipe, GardenGenerator.Generate(lastRecipe.Size, lastRandom));
    }
}
