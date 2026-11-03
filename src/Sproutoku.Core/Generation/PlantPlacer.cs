using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Generation;

/// <summary>
/// The first step of making a garden: where its plants go, before any beds exist.
/// One plant per row and per column, and no two plants touching, even diagonally.
/// </summary>
public static class PlantPlacer {
    /// <summary>
    /// A random valid layout for an N×N garden, the same for the same seed. Item r is row r's plant.
    /// </summary>
    public static IReadOnlyList<Cell> Place(int size, SeededRandom random) {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, PuzzleValidator.MinSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, PuzzleValidator.MaxSize);
        ArgumentNullException.ThrowIfNull(random);

        var columns = new int[size];
        var used = new bool[size];

        if (!PlaceRow(0)) {
            // Can't happen for 4 or more: there's always a valid layout.
            throw new InvalidOperationException($"No plant layout exists for a {size}×{size} garden.");
        }

        return columns.Select((column, row) => new Cell(row, column)).ToArray();

        // Tries this row's columns in a shuffled order, backing up when a later row has nowhere to go.
        bool PlaceRow(int row) {
            if (row == size) return true;

            var choices = Enumerable.Range(0, size).ToArray();
            random.Shuffle(choices);

            foreach (int column in choices) {
                if (used[column]) continue;

                // One plant per row, so a plant can only touch the plant in the row just above.
                if (row > 0 && Math.Abs(columns[row - 1] - column) <= 1) continue;

                columns[row] = column;
                used[column] = true;

                if (PlaceRow(row + 1)) return true;

                used[column] = false;
            }

            return false;
        }
    }
}
