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
    public static SolutionCount Count(int[][] regions) {
        ArgumentNullException.ThrowIfNull(regions);

        int size = regions.Length;
        var regionOf = new int[size, size];
        for (int row = 0; row < size; row++) {
            if (regions[row].Length != size) {
                throw new ArgumentException("The bed grid must be square.", nameof(regions));
            }

            for (int column = 0; column < size; column++) {
                regionOf[row, column] = regions[row][column];
            }
        }

        return Count(regionOf);
    }

    private static SolutionCount Count(int[,] regionOf) {
        int size = regionOf.GetLength(0);
        int allRegions = (1 << size) - 1;
        int found = 0;

        Search(row: 0, usedColumns: 0, usedRegions: 0, previousColumn: -2);

        return found switch {
            0 => SolutionCount.None,
            1 => SolutionCount.One,
            _ => SolutionCount.TwoOrMore,
        };

        // usedColumns and usedRegions are bit sets: bit c is on once column c (or bed c) has a plant.
        void Search(int row, int usedColumns, int usedRegions, int previousColumn) {
            if (row == size) {
                found++;
                return;
            }

            for (int column = 0; column < size && found < 2; column++) {
                int region = regionOf[row, column];

                if ((usedColumns & (1 << column)) != 0) continue;
                if ((usedRegions & (1 << region)) != 0) continue;

                // One plant per row, so a plant can only touch the plant in the row just above.
                if (Math.Abs(column - previousColumn) <= 1) continue;

                int columns = usedColumns | (1 << column);
                int regions = usedRegions | (1 << region);
                if (!EveryOpenBedCanStillGrow(row + 1, columns, regions)) continue;

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
