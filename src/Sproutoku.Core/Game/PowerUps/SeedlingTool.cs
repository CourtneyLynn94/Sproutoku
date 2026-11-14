using Sproutoku.Core.Game.Boards;
using Sproutoku.Core.Gardens.Puzzles;

namespace Sproutoku.Core.Game.PowerUps;

/// <summary>
/// Which plant a seedling grows: the correct plant of the bed with the fewest cells still open (empty or ✕; a ✕ might be wrong),
/// ties going to the bed that comes first in reading order. Predictable: the same board always gets the same plant.
/// </summary>
public static class SeedlingTool {
    /// <summary>
    /// The cell a seedling would grow, or null if every bed already has its plant.
    /// </summary>
    public static Cell? CellFor(Board board) {
        var puzzle = board.Puzzle;
        var open = new Dictionary<int, int>();
        var planted = new HashSet<int>();
        var readingOrder = new List<int>(); // Beds in the order their first cell appears, row by row.

        for (int row = 0; row < board.Size; row++) {
            for (int column = 0; column < board.Size; column++) {
                var cell = new Cell(row, column);
                int bed = puzzle.RegionAt(cell);

                if (!open.ContainsKey(bed)) {
                    open[bed] = 0;
                    readingOrder.Add(bed);
                }

                switch (board[cell]) {
                    case CellState.Planted:
                        planted.Add(bed);
                        break;
                    case CellState.Empty or CellState.Marked:
                        open[bed]++;
                        break;
                }
            }
        }

        int? chosen = null;
        foreach (int bed in readingOrder) {
            if (planted.Contains(bed)) continue;

            if (chosen is null || open[bed] < open[chosen.Value]) {
                chosen = bed;
            }
        }

        return chosen is { } bedToGrow ? puzzle.Solution[bedToGrow] : null;
    }
}
