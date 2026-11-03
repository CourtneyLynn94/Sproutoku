using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Generation;

/// <summary>
/// The second step of making a garden: grows a bed out from each plant until the beds fill the garden.
/// </summary>
public static class BedGrower {
    private const int NoBed = -1;

    /// <summary>
    /// Bed ids for every cell, as beds[row][column]. Bed b holds plants[b].
    /// Every cell gets a bed, and every bed is connected and holds its plant.
    /// </summary>
    public static int[][] Grow(IReadOnlyList<Cell> plants, SeededRandom random) {
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

        // Each pass adds one empty cell to a bed next to it, until no empty cells are left.
        var edges = new List<(Cell Cell, int Bed)>();
        for (int emptyCells = size * size - size; emptyCells > 0; emptyCells--) {
            edges.Clear();

            for (int row = 0; row < size; row++) {
                for (int column = 0; column < size; column++) {
                    if (beds[row][column] != NoBed) continue;

                    var cell = new Cell(row, column);
                    foreach (var neighbor in cell.EdgeNeighbors()) {
                        if (neighbor.IsInside(size) && beds[neighbor.Row][neighbor.Column] != NoBed) {
                            edges.Add((cell, beds[neighbor.Row][neighbor.Column]));
                        }
                    }
                }
            }

            // There's always at least one edge: the garden is one connected grid and every bed has a cell.
            var (next, nextBed) = random.Pick(edges);
            beds[next.Row][next.Column] = nextBed;
        }

        return beds;
    }
}
