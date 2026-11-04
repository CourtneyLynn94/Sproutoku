using Sproutoku.Core.Puzzles;
using Sproutoku.Core.Solving;

namespace Sproutoku.Core.Generation;

/// <summary>
/// A generated garden with its difficulty grade, how many logic steps it takes, and how many attempts it took to make (for the dev view).
/// </summary>
public sealed record GeneratedGarden(Puzzle Puzzle, DifficultyGrade Grade, int Steps, int Attempts);

/// <summary>
/// Makes a new garden from a seed: plants, beds, then reshaping until there's exactly one answer that logic alone can reach.
/// The same size and seed always give the same garden.
/// </summary>
public static class GardenGenerator {
    /// <summary>
    /// Attempts before giving up. Real gardens need about 1 to 40, so this is never reached in practice.
    /// </summary>
    public const int MaxAttempts = 1000;

    public static GeneratedGarden Generate(int size, ulong seed, DifficultyGrade easiest = DifficultyGrade.Gentle, DifficultyGrade hardest = DifficultyGrade.Expert)
        => Generate(size, new SeededRandom(seed), easiest, hardest);

    /// <summary>
    /// A garden whose grade is between <paramref name="easiest"/> and <paramref name="hardest"/>, inclusive.
    /// </summary>
    public static GeneratedGarden Generate(int size, SeededRandom random, DifficultyGrade easiest = DifficultyGrade.Gentle, DifficultyGrade hardest = DifficultyGrade.Expert) {
        ArgumentNullException.ThrowIfNull(random);
        if (easiest > hardest) {
            throw new ArgumentException($"{easiest} is harder than {hardest}.", nameof(easiest));
        }

        // A failed attempt keeps drawing from the same random sequence, so the next one starts fresh but is still set by the seed.
        for (int attempt = 1; attempt <= MaxAttempts; attempt++) {
            var plants = PlantPlacer.Place(size, random);
            var beds = BedGrower.Grow(plants, random);

            if (!BedReshaper.TryMakeUnique(beds, plants, random, maxMoves: size * size)) continue;

            // One answer isn't enough; the player must be able to reach it without guessing.
            var solve = LogicSolver.Solve(GardenFacts.Blank(beds));
            if (!solve.IsSolved) continue;

            var grade = DifficultyGrader.Grade(solve);
            if (grade < easiest || grade > hardest) continue;

            return new GeneratedGarden(Puzzle.Create(beds, plants), grade, solve.Steps.Count, attempt);
        }

        throw new InvalidOperationException($"No {size}×{size} garden from {easiest} to {hardest} found in {MaxAttempts} attempts.");
    }
}
