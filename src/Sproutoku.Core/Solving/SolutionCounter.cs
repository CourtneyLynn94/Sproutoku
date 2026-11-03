using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Solving;

public enum SolutionCount {
    None,
    One,
    TwoOrMore,
}

/// <summary>
/// Counts how many ways a garden's beds can be solved, stopping at two:
/// all the game needs to know is none, exactly one, or more than one.
/// It tries plant positions row by row and ignores any answer the puzzle has stored.
/// </summary>
public static class SolutionCounter {
    /// <summary>
    /// Counts the solutions for a puzzle's beds.
    /// </summary>
    public static SolutionCount Count(Puzzle puzzle) {
        ArgumentNullException.ThrowIfNull(puzzle);

        var regionOf = new int[puzzle.Size, puzzle.Size];
        for (int row = 0; row < puzzle.Size; row++) {
            for (int column = 0; column < puzzle.Size; column++) {
                regionOf[row, column] = puzzle.RegionAt(new Cell(row, column));
            }
        }

        return Count(regionOf);
    }

    /// <summary>
    /// Counts the solutions for a square grid of bed ids (regions[row][column], ids 0 to N−1).
    /// The grid isn't validated; use this for bed layouts that aren't puzzles yet.
    /// </summary>
    public static SolutionCount Count(int[][] regions) => Count(ToGrid(regions));

    /// <summary>
    /// A solution for the beds other than <paramref name="known"/>, with its plants in row order, or null if <paramref name="known"/> is the only one.
    /// The generator uses it to see which cells a second answer needs.
    /// </summary>
    public static IReadOnlyList<Cell>? FindOther(int[][] regions, IReadOnlyList<Cell> known) {
        ArgumentNullException.ThrowIfNull(known);

        var regionOf = ToGrid(regions);
        var knownCells = known.ToHashSet();

        // If there are two or more answers, at least one of the first two found isn't the known one.
        foreach (var columns in FirstTwoSolutions(regionOf)) {
            var plants = columns.Select((column, row) => new Cell(row, column)).ToArray();
            if (!plants.All(knownCells.Contains)) {
                return plants;
            }
        }

        return null;
    }

    private static int[,] ToGrid(int[][] regions) {
        ArgumentNullException.ThrowIfNull(regions);

        int size = regions.Length;
        if (size < 1 || size > PuzzleValidator.MaxSize) {
            throw new ArgumentOutOfRangeException(nameof(regions), size, $"A garden is 1 to {PuzzleValidator.MaxSize} rows.");
        }

        var regionOf = new int[size, size];
        for (int row = 0; row < size; row++) {
            if (regions[row].Length != size) {
                throw new ArgumentException("The bed grid must be square.", nameof(regions));
            }

            for (int column = 0; column < size; column++) {
                regionOf[row, column] = regions[row][column];
            }
        }

        return regionOf;
    }

    private static SolutionCount Count(int[,] regionOf) => FirstTwoSolutions(regionOf).Count switch {
        0 => SolutionCount.None,
        1 => SolutionCount.One,
        _ => SolutionCount.TwoOrMore,
    };

    // Finds up to two solutions, each as the plant's column in every row. Two is all the game ever needs.
    private static List<int[]> FirstTwoSolutions(int[,] regionOf) {
        int size = regionOf.GetLength(0);
        int allRegions = (1 << size) - 1;
        var found = new List<int[]>();
        var columnInRow = new int[size];

        Search(row: 0, usedColumns: 0, usedRegions: 0, previousColumn: -2);

        return found;

        // usedColumns and usedRegions are bit sets: bit c is on once column c (or bed c) has a plant.
        void Search(int row, int usedColumns, int usedRegions, int previousColumn) {
            if (row == size) {
                found.Add((int[])columnInRow.Clone());
                return;
            }

            for (int column = 0; column < size && found.Count < 2; column++) {
                int region = regionOf[row, column];

                if ((usedColumns & (1 << column)) != 0) continue;
                if ((usedRegions & (1 << region)) != 0) continue;

                // One plant per row, so a plant can only touch the plant in the row just above.
                if (Math.Abs(column - previousColumn) <= 1) continue;

                int columns = usedColumns | (1 << column);
                int regions = usedRegions | (1 << region);
                if (!EveryOpenBedCanStillGrow(row + 1, columns, regions)) continue;

                columnInRow[row] = column;
                Search(row + 1, columns, regions, column);
            }
        }

        // Every bed without a plant must still have a cell in a later row and a free column.
        bool EveryOpenBedCanStillGrow(int fromRow, int usedColumns, int usedRegions) {
            int reachable = usedRegions;
            for (int row = fromRow; row < size; row++) {
                for (int column = 0; column < size; column++) {
                    if ((usedColumns & (1 << column)) == 0) {
                        reachable |= 1 << regionOf[row, column];
                    }
                }
            }

            return reachable == allRegions;
        }
    }
}
