using Sproutoku.Core.Puzzles;
using Sproutoku.Core.Solving;

namespace Sproutoku.Core.Generation;

/// <summary>
/// The third step of making a garden: reshapes beds until the planned plants are the only answer.
/// </summary>
public static class BedReshaper {
    /// <summary>
    /// Changes <paramref name="beds"/> in place, one cell at a time, until <paramref name="plants"/> is the only solution.
    /// Returns false if it gives up: after <paramref name="maxMoves"/> moves, or when no cell can move.
    /// </summary>
    public static bool TryMakeUnique(int[][] beds, IReadOnlyList<Cell> plants, SeededRandom random, int maxMoves) {
        ArgumentNullException.ThrowIfNull(beds);
        ArgumentNullException.ThrowIfNull(plants);
        ArgumentNullException.ThrowIfNull(random);

        for (int moves = 0; ; moves++) {
            var other = SolutionCounter.FindOther(beds, plants);
            if (other is null) return true;
            if (moves == maxMoves) return false;

            var choices = MovesThatBreak(beds, plants, other);
            if (choices.Count == 0) return false;

            var (cell, newBed) = random.Pick(choices);
            beds[cell.Row][cell.Column] = newBed;
        }
    }

    // Each choice moves one of the other answer's cells into a neighboring bed.
    // That bed then holds two of the other answer's plants, so that answer is broken.
    // The cell is never one of the real plants, so the real answer still works.
    private static List<(Cell Cell, int NewBed)> MovesThatBreak(int[][] beds, IReadOnlyList<Cell> plants, IReadOnlyList<Cell> other) {
        int size = beds.Length;
        var realPlants = plants.ToHashSet();
        var choices = new List<(Cell Cell, int NewBed)>();

        foreach (var cell in other) {
            if (realPlants.Contains(cell)) continue;

            int bed = beds[cell.Row][cell.Column];
            if (!StaysConnectedWithout(beds, cell)) continue;

            foreach (var neighbor in cell.EdgeNeighbors()) {
                if (!neighbor.IsInside(size)) continue;

                int newBed = beds[neighbor.Row][neighbor.Column];
                if (newBed != bed && !choices.Contains((cell, newBed))) {
                    choices.Add((cell, newBed));
                }
            }
        }

        return choices;
    }

    // True if the cell's bed is still one piece without it. Real plants never move, so the bed always keeps at least its plant.
    private static bool StaysConnectedWithout(int[][] beds, Cell removed) {
        int size = beds.Length;
        int bed = beds[removed.Row][removed.Column];

        var rest = new HashSet<Cell>();
        for (int row = 0; row < size; row++) {
            for (int column = 0; column < size; column++) {
                var cell = new Cell(row, column);
                if (beds[row][column] == bed && cell != removed) {
                    rest.Add(cell);
                }
            }
        }

        // Flood fill from any one cell; the bed is connected if the fill reaches all of them.
        var start = rest.First();
        var reached = new HashSet<Cell> { start };
        var toVisit = new Stack<Cell>();
        toVisit.Push(start);

        while (toVisit.Count > 0) {
            foreach (var neighbor in toVisit.Pop().EdgeNeighbors()) {
                if (rest.Contains(neighbor) && reached.Add(neighbor)) {
                    toVisit.Push(neighbor);
                }
            }
        }

        return reached.Count == rest.Count;
    }
}
