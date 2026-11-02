using Sproutoku.Core.Puzzles;
using Sproutoku.Core.Solving;

namespace Sproutoku.Core.Generation;

/// <summary>
/// Makes a new garden from a seed: plants, beds, then reshaping until there's exactly one answer that logic alone can reach.
/// The same size and seed always give the same garden.
/// </summary>
public static class GardenGenerator {
    /// <summary>
    /// Attempts before giving up. Real gardens need about 1 to 40, so this is never reached in practice.
    /// </summary>
    public const int MaxAttempts = 1000;

    public static Puzzle Generate(int size, ulong seed) => Generate(size, new SeededRandom(seed));

    public static Puzzle Generate(int size, SeededRandom random) {
        ArgumentNullException.ThrowIfNull(random);

        // A failed attempt keeps drawing from the same random sequence, so the next one starts fresh but is still set by the seed.
        for (int attempt = 0; attempt < MaxAttempts; attempt++) {
            var plants = PlantPlacer.Place(size, random);
            var beds = BedGrower.Grow(plants, random);

            if (!BedReshaper.TryMakeUnique(beds, plants, random, maxMoves: size * size)) continue;

            // One answer isn't enough; the player must be able to reach it without guessing.
            if (!LogicSolver.Solve(GardenFacts.Blank(beds)).IsSolved) continue;

            return Puzzle.Create(beds, plants);
        }

        throw new InvalidOperationException($"No {size}×{size} garden found in {MaxAttempts} attempts.");
    }
}
