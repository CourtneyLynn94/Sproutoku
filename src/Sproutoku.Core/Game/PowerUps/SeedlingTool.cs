using Sproutoku.Core.Game.Boards;
using Sproutoku.Core.Gardens.Puzzles;

namespace Sproutoku.Core.Game.PowerUps;

/// <summary>
/// Which plant a seedling grows: the correct plant of a random bed that doesn't have its plant yet.
/// </summary>
public static class SeedlingTool {
    /// <summary>
    /// The cell a seedling would grow, or null if every bed already has its plant.
    /// </summary>
    public static Cell? CellFor(Board board, Random random) {
        var notGrown = board.Puzzle.Solution.Where(cell => board[cell] != CellState.Planted).ToList();
        return notGrown.Count == 0 ? null : notGrown[random.Next(notGrown.Count)];
    }
}
