using System.Diagnostics;
using Sproutoku.Core.Gardens.Puzzles;

namespace Sproutoku.Core.Gardens.Generation;

/// <summary>
/// The second step of making a garden: grows a bed out from each plant until the beds fill the garden, in one of several styles.
/// </summary>
public static class BedGrower {
    private const int NoBed = -1;

    // How often each style is picked, out of 100. Sprawling makes the most one-answer gardens, so it stays the most common.
    private static readonly (BedStyle Style, int Weight)[] StyleWeights = [
        (BedStyle.Sprawling, 50),
        (BedStyle.Winding, 30),
        (BedStyle.Even, 20),
    ];

    /// <summary>
    /// A random style for one garden, from its random sequence.
    /// </summary>
    public static BedStyle PickStyle(SeededRandom random) {
        ArgumentNullException.ThrowIfNull(random);

        int roll = random.Next(StyleWeights.Sum(option => option.Weight));
        foreach (var (style, weight) in StyleWeights) {
            if (roll < weight) return style;
            roll -= weight;
        }

        throw new UnreachableException();
    }

    /// <summary>
    /// Bed ids for every cell, as beds[row][column]. Bed b holds plants[b].
    /// Every cell gets a bed, and every bed is connected and holds its plant.
    /// </summary>
    public static int[][] Grow(IReadOnlyList<Cell> plants, SeededRandom random, BedStyle style = BedStyle.Sprawling) {
        ArgumentNullException.ThrowIfNull(plants);
        ArgumentNullException.ThrowIfNull(random);

        int size = plants.Count;
        if (size < PuzzleValidator.MinSize || size > PuzzleValidator.MaxSize) {
            throw new ArgumentException($"A garden needs {PuzzleValidator.MinSize} to {PuzzleValidator.MaxSize} plants.", nameof(plants));
        }

        var beds = new int[size][];
        for (int row = 0; row < size; row++) {
            beds[row] = new int[size];
            Array.Fill(beds[row], NoBed);
        }

        for (int bed = 0; bed < size; bed++) {
            var plant = plants[bed];
            if (!plant.IsInside(size) || beds[plant.Row][plant.Column] != NoBed) {
                throw new ArgumentException($"Plant {plant} is outside the garden or on another plant's cell.", nameof(plants));
            }

            beds[plant.Row][plant.Column] = bed;
        }

        // How many cells each bed has so far; the Even style uses it.
        var bedSizes = new int[size];
        Array.Fill(bedSizes, 1);

        // Each pass adds one empty cell to a bed next to it, until no empty cells are left.
        var choices = new List<(Cell Cell, int Bed, int Weight)>();
        for (int emptyCells = size * size - size; emptyCells > 0; emptyCells--) {
            choices.Clear();

            for (int row = 0; row < size; row++) {
                for (int column = 0; column < size; column++) {
                    if (beds[row][column] == NoBed) {
                        AddChoices(beds, bedSizes, new Cell(row, column), style, choices);
                    }
                }
            }

            // There's always at least one choice: the garden is one connected grid and every bed has a cell.
            var (next, nextBed, _) = PickWeighted(choices, random);
            beds[next.Row][next.Column] = nextBed;
            bedSizes[nextBed]++;
        }

        return beds;
    }

    // The ways an empty cell could join a bed next to it, each with how likely it is.
    private static void AddChoices(int[][] beds, int[] bedSizes, Cell cell, BedStyle style, List<(Cell Cell, int Bed, int Weight)> choices) {
        int size = beds.Length;

        if (style == BedStyle.Winding) {
            // One choice per bed the cell touches, much likelier when it touches that bed on only one side.
            var touched = new List<(int Bed, int Sides)>();
            foreach (var neighbor in cell.EdgeNeighbors()) {
                if (!neighbor.IsInside(size) || beds[neighbor.Row][neighbor.Column] == NoBed) continue;

                int bed = beds[neighbor.Row][neighbor.Column];
                int index = touched.FindIndex(entry => entry.Bed == bed);
                if (index < 0) {
                    touched.Add((bed, 1));
                } else {
                    touched[index] = (bed, touched[index].Sides + 1);
                }
            }

            foreach (var (bed, sides) in touched) {
                choices.Add((cell, bed, sides switch { 1 => 12, 2 => 3, _ => 1 }));
            }

            return;
        }

        // One choice per shared edge, so a cell touching a bed on two sides is twice as likely to join it.
        // Sprawling: every edge counts the same. Even: an edge counts more the smaller its bed.
        foreach (var neighbor in cell.EdgeNeighbors()) {
            if (!neighbor.IsInside(size) || beds[neighbor.Row][neighbor.Column] == NoBed) continue;

            int bed = beds[neighbor.Row][neighbor.Column];
            int weight = style == BedStyle.Even ? 840 / bedSizes[bed] : 1;
            choices.Add((cell, bed, weight));
        }
    }

    // Rolls a number below the total weight, then walks the list until the roll lands inside a choice's share.
    private static (Cell Cell, int Bed, int Weight) PickWeighted(List<(Cell Cell, int Bed, int Weight)> choices, SeededRandom random) {
        int roll = random.Next(choices.Sum(choice => choice.Weight));
        foreach (var choice in choices) {
            if (roll < choice.Weight) return choice;
            roll -= choice.Weight;
        }

        throw new UnreachableException();
    }
}
