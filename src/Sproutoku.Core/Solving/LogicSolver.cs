using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Solving;

/// <summary>
/// Solves a garden the way a player would: one deduction at a time, using only the facts it's given.
/// Techniques are tried cheapest first, so the steps read like a natural line of reasoning.
/// </summary>
public static class LogicSolver {
    /// <summary>
    /// Works forward from the facts until every bed has its plant, or no technique can find another step.
    /// </summary>
    public static SolveResult Solve(GardenFacts facts) {
        ArgumentNullException.ThrowIfNull(facts);

        var grid = new SolverGrid(facts);
        var steps = new List<LogicStep>();

        while (!grid.IsSolved && NextStep(grid) is { } step) {
            grid.Apply(step);
            steps.Add(step);
        }

        return new SolveResult(grid.IsSolved, steps, grid.Plants());
    }

    /// <summary>
    /// The first deduction these facts allow, or null if there's none.
    /// </summary>
    public static LogicStep? NextStep(GardenFacts facts) {
        ArgumentNullException.ThrowIfNull(facts);
        return NextStep(new SolverGrid(facts));
    }

    // Cheapest first.
    private static LogicStep? NextStep(SolverGrid grid) =>
        PlantRulesOut(grid)
        ?? OnlyCellLeft(grid);

    // A plant rules out its row, column, bed and the touching cells: the first plant that still has something to rule out.
    private static LogicStep? PlantRulesOut(SolverGrid grid) {
        foreach (var plant in grid.Plants()) {
            var ruledOut = grid.AllCells()
                .Where(cell => grid[cell] == Known.Unknown && Blocks(grid, plant, cell))
                .ToList();

            if (ruledOut.Count > 0) {
                return new LogicStep(Technique.PlantRulesOut, Area: null, Plants: [], ruledOut, Reasons: [plant]);
            }
        }

        return null;
    }

    // A bed, row or column with no plant yet and exactly one possible cell must hold its plant there.
    private static LogicStep? OnlyCellLeft(SolverGrid grid) {
        foreach (var area in grid.AllAreas()) {
            var cells = grid.CellsOf(area).ToList();
            if (cells.Any(cell => grid[cell] == Known.Plant)) continue;

            var candidates = cells.Where(cell => grid[cell] == Known.Unknown).ToList();
            if (candidates.Count == 1) {
                var reasons = cells.Where(cell => grid[cell] == Known.NoPlant).ToList();
                return new LogicStep(Technique.OnlyCellLeft, area, Plants: candidates, RuledOut: [], reasons);
            }
        }

        return null;
    }

    /// <summary>
    /// True if a plant on one cell means the other can't hold a plant:
    /// same row, same column, same bed, or touching (including diagonally).
    /// </summary>
    internal static bool Blocks(SolverGrid grid, Cell plant, Cell other) =>
        other != plant
        && (other.Row == plant.Row
            || other.Column == plant.Column
            || grid.RegionAt(other) == grid.RegionAt(plant)
            || other.Touches(plant));
}
