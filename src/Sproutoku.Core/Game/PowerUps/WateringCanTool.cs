using Sproutoku.Core.Game.Boards;
using Sproutoku.Core.Gardens.Puzzles;
using Sproutoku.Core.Gardens.Solving;

namespace Sproutoku.Core.Game.PowerUps;

/// <summary>
/// Which cells the watering can reveals: up to <see cref="MaxCells"/> empty cells that logic can prove hold no plant,
/// in the order the logic solver rules them out, from the board's facts and the player's correct ✕ marks (the Worm's view).
/// It never peeks at the answer, so every revealed cell can be explained.
/// </summary>
public static class WateringCanTool {
    /// <summary>
    /// The most cells one watering reveals.
    /// </summary>
    public const int MaxCells = 3;

    /// <summary>
    /// The cells to reveal, or none if logic can't prove any more empty cells (or the garden is over).
    /// </summary>
    public static IReadOnlyList<Cell> CellsFor(Board board) {
        if (board.IsFinished) return [];

        var cells = new List<Cell>();
        foreach (var step in LogicSolver.Solve(WormHelperTool.FactsWithCorrectMarks(board)).Steps) {
            foreach (var cell in step.RuledOut) {
                if (board[cell] != CellState.Empty || cells.Contains(cell)) continue;

                cells.Add(cell);
                if (cells.Count == MaxCells) return cells;
            }
        }

        return cells;
    }
}
