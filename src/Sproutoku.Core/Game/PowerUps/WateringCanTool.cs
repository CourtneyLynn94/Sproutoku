using Sproutoku.Core.Game.Boards;
using Sproutoku.Core.Gardens.Puzzles;
using Sproutoku.Core.Gardens.Solving;

namespace Sproutoku.Core.Game.PowerUps;

/// <summary>
/// Which cells the watering can reveals: up to <see cref="MaxCells"/> random cells that hold no plant.
/// The solver proves which cells hold no plant, and the can only picks cells that are still empty:
/// never the player's ✕, a wilt, or a cell already revealed. It doesn't follow the order logic would find them in,
/// so watering again and again doesn't just cross off rows and beds the way the Worm does.
/// </summary>
public static class WateringCanTool {
    /// <summary>
    /// The most cells one watering reveals.
    /// </summary>
    public const int MaxCells = 3;

    /// <summary>
    /// The cells to reveal, in no particular order, or none if no empty cell is proven to hold no plant (or the garden is over).
    /// </summary>
    public static IReadOnlyList<Cell> CellsFor(Board board, Random random) {
        if (board.IsFinished) return [];

        var proven = LogicSolver.Solve(GardenFacts.From(board));
        var plants = proven.Plants.ToHashSet();
        var ruledOut = proven.Steps.SelectMany(step => step.RuledOut).ToHashSet();

        var candidates = (
            from row in Enumerable.Range(0, board.Size)
            from column in Enumerable.Range(0, board.Size)
            let cell = new Cell(row, column)
            where board[cell] == CellState.Empty && (proven.IsSolved ? !plants.Contains(cell) : ruledOut.Contains(cell))
            select cell).ToList();

        var picked = new List<Cell>();
        while (picked.Count < MaxCells && candidates.Count > 0) {
            int index = random.Next(candidates.Count);
            picked.Add(candidates[index]);
            candidates.RemoveAt(index);
        }

        return picked;
    }
}
