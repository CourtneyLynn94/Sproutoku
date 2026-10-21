using Sproutoku.Core.Puzzles;
using Cell = Sproutoku.Core.Puzzles.Cell;

namespace Sproutoku.App.DevTools;

/// <summary>
/// Throwaway generator for eyeballing grids. 
/// Always makes a valid garden, but it may have more than one solution. 
/// The real generator coming soon..
/// </summary>
public static class RoughGenerator {
    public static Puzzle Generate(int size, Random random) {
        // 1. One plant per row: plantColumns[row] is the column of that row's plant.
        var plantColumns = new int[size];
        PlacePlants(0, plantColumns, random);

        // 2. Each plant starts its own region. Region r belongs to the plant in row r.
        var regions = new int[size][];
        var solution = new Cell[size];
        for (int row = 0; row < size; row++) {
            regions[row] = Enumerable.Repeat(-1, size).ToArray();
            regions[row][plantColumns[row]] = row;
            solution[row] = new Cell(row, plantColumns[row]);
        }

        // 3. Grow: give a random empty cell the region of one of its neighbors, until the grid is full.
        int unassigned = (size * size) - size;
        while (unassigned > 0) {
            var cell = new Cell(random.Next(size), random.Next(size));
            if (regions[cell.Row][cell.Column] != -1) {
                continue;
            }

            var grownNeighbors = cell.EdgeNeighbors()
                .Where(n => n.IsInside(size) && regions[n.Row][n.Column] != -1)
                .ToList();
            if (grownNeighbors.Count == 0) {
                continue;
            }

            var pick = grownNeighbors[random.Next(grownNeighbors.Count)];
            regions[cell.Row][cell.Column] = regions[pick.Row][pick.Column];
            unassigned--;
        }

        return Puzzle.Create(regions, solution);
    }

    // Backtracking: try columns in random order.
    // A plant can't share a column with an earlier plant or sit next to the plant in the row above.
    private static bool PlacePlants(int row, int[] plantColumns, Random random) {
        if (row == plantColumns.Length) {
            return true;
        }

        var columns = Enumerable.Range(0, plantColumns.Length).ToArray();
        random.Shuffle(columns);

        foreach (int column in columns) {
            bool columnUsed = plantColumns.Take(row).Contains(column);
            bool touchesRowAbove = row > 0 && Math.Abs(plantColumns[row - 1] - column) <= 1;
            if (columnUsed || touchesRowAbove) {
                continue;
            }

            plantColumns[row] = column;
            if (PlacePlants(row + 1, plantColumns, random)) {
                return true;
            }
        }

        return false;
    }
}