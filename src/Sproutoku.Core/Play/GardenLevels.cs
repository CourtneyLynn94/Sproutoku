using Sproutoku.Core.Generation;
using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Play;

/// <summary>
/// A level's garden: the level, the seed it came from, its recipe from the level plan, and the generated garden.
/// </summary>
public sealed record LevelGarden(int Level, ulong Seed, LevelRecipe Recipe, GeneratedGarden Garden) {
    public Puzzle Puzzle => Garden.Puzzle;

    /// <summary>
    /// A fresh board with the level's own suns.
    /// </summary>
    public Board Start() => Board.Start(Puzzle, Recipe.Suns);
}

/// <summary>
/// Which garden each level plays, without end. Every level follows the level plan.
/// A level's garden comes only from its number, so it's the same for every player, on every device, in every build.
/// </summary>
public static class GardenLevels {
    /// <summary>
    /// Backup seeds to try if a level's own seed can't grow its garden. They're made from the level's seed, so every player gets the same ones.
    /// </summary>
    public const int MaxBackups = 10;

    /// <summary>
    /// A level's seed.
    /// </summary>
    public static ulong SeedFor(int level) {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        return SeededRandom.SeedFrom(GardenGenerator.Version, level);
    }

    /// <summary>
    /// A level's garden: the recipe first, then the garden, both from the same random sequence.
    /// Never fails: if the seed and its backups can't reach the recipe's grade, the level takes a garden of any grade.
    /// </summary>
    public static LevelGarden ForLevel(int level) {
        ulong seed = SeedFor(level);

        for (int backup = 0; backup <= MaxBackups; backup++) {
            ulong gardenSeed = backup == 0 ? seed : SeededRandom.SeedFrom(unchecked((long)seed), backup);
            var random = new SeededRandom(gardenSeed);
            var recipe = LevelPlan.For(level, random);

            if (GardenGenerator.TryGenerate(recipe.Size, random, recipe.Easiest, recipe.Hardest) is { } garden) {
                return new LevelGarden(level, seed, recipe, garden);
            }
        }

        // Last resort, never expected in practice: the recipe's size and suns, but any grade.
        // A one-answer garden turns up within a few attempts at every size, so the level always exists and the player keeps their progress.
        var lastRandom = new SeededRandom(seed);
        var lastRecipe = LevelPlan.For(level, lastRandom);
        return new LevelGarden(level, seed, lastRecipe, GardenGenerator.Generate(lastRecipe.Size, lastRandom));
    }
}
