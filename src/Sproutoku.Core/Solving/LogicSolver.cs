using Sproutoku.Core.Puzzles;
using System.Numerics;

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
        ?? OnlyCellLeft(grid)
        ?? GroupsFillLines(grid)
        ?? WouldStarve(grid);

    // A plant rules out its row, column, bed and the touching cells: the first plant that still has something to rule out.
    private static LogicStep? PlantRulesOut(SolverGrid grid) {
        foreach (var plant in grid.Plants()) {
            var ruledOut = grid.AllCells()
                .Where(cell => grid[cell] == Known.Unknown && Blocks(grid, plant, cell))
                .ToList();

            if (ruledOut.Count > 0) {
                return new LogicStep(Technique.PlantRulesOut, Areas: [], Plants: [], ruledOut, Reasons: [plant]);
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
                return new LogicStep(Technique.OnlyCellLeft, Areas: [area], Plants: candidates, RuledOut: [], reasons);
            }
        }

        return null;
    }

    private static readonly AreaKind[] LineKinds = [AreaKind.Row, AreaKind.Column];

    // Smallest groups first, so the simplest reasoning is found first.
    // Only up to half the open beds: a bigger group is the same deduction as the smaller group
    // left over, seen the other way round (beds fill lines versus lines fill beds).
    private static LogicStep? GroupsFillLines(SolverGrid grid) {
        int openBeds = grid.AllCells()
            .Where(cell => grid[cell] == Known.Unknown)
            .Select(grid.RegionAt)
            .Distinct()
            .Count();

        for (int k = 1; k <= openBeds / 2; k++) {
            foreach (var lineKind in LineKinds) {
                var step = BedsFillLines(grid, lineKind, k) ?? LinesFillBeds(grid, lineKind, k);
                if (step is not null) return step;
            }
        }

        return null;
    }

    // k beds whose open cells all lie in the same k lines: those lines' plants must be these beds',
    // so every other bed's open cells in those lines are ruled out.
    private static LogicStep? BedsFillLines(SolverGrid grid, AreaKind lineKind, int k) {
        var open = grid.AllCells().Where(cell => grid[cell] == Known.Unknown).ToList();

        // For each bed, a bit set of the lines its open cells are in.
        var linesOfBed = new int[grid.Size];
        foreach (var cell in open) {
            linesOfBed[grid.RegionAt(cell)] |= 1 << LineOf(cell, lineKind);
        }

        var openBeds = Enumerable.Range(0, grid.Size).Where(bed => linesOfBed[bed] != 0).ToList();

        foreach (var beds in Combinations(openBeds, k)) {
            int lines = beds.Aggregate(0, (set, bed) => set | linesOfBed[bed]);
            if (BitOperations.PopCount((uint)lines) != k) continue;

            var ruledOut = open
                .Where(cell => IsIn(lines, LineOf(cell, lineKind)) && !beds.Contains(grid.RegionAt(cell)))
                .ToList();
            if (ruledOut.Count == 0) continue;

            var areas = beds.Select(bed => new Area(AreaKind.Bed, bed))
                .Concat(AreasIn(lines, lineKind, grid.Size))
                .ToList();
            var reasons = open.Where(cell => beds.Contains(grid.RegionAt(cell))).ToList();
            return new LogicStep(Technique.BedsFillLines, areas, Plants: [], ruledOut, reasons);
        }

        return null;
    }

    // k lines whose open cells all belong to the same k beds: those beds' plants must be in these lines,
    // so the beds' open cells outside them are ruled out.
    private static LogicStep? LinesFillBeds(SolverGrid grid, AreaKind lineKind, int k) {
        var open = grid.AllCells().Where(cell => grid[cell] == Known.Unknown).ToList();

        // For each line, a bit set of the beds its open cells belong to.
        var bedsOfLine = new int[grid.Size];
        foreach (var cell in open) {
            bedsOfLine[LineOf(cell, lineKind)] |= 1 << grid.RegionAt(cell);
        }

        var openLines = Enumerable.Range(0, grid.Size).Where(line => bedsOfLine[line] != 0).ToList();

        foreach (var lines in Combinations(openLines, k)) {
            int beds = lines.Aggregate(0, (set, line) => set | bedsOfLine[line]);
            if (BitOperations.PopCount((uint)beds) != k) continue;

            var ruledOut = open
                .Where(cell => IsIn(beds, grid.RegionAt(cell)) && !lines.Contains(LineOf(cell, lineKind)))
                .ToList();
            if (ruledOut.Count == 0) continue;

            var areas = lines.Select(line => new Area(lineKind, line))
                .Concat(AreasIn(beds, AreaKind.Bed, grid.Size))
                .ToList();
            var reasons = open.Where(cell => lines.Contains(LineOf(cell, lineKind))).ToList();
            return new LogicStep(Technique.LinesFillBeds, areas, Plants: [], ruledOut, reasons);
        }

        return null;
    }

    private static int LineOf(Cell cell, AreaKind lineKind) => lineKind == AreaKind.Row ? cell.Row : cell.Column;

    private static bool IsIn(int set, int index) => (set & (1 << index)) != 0;

    // The areas whose bits are on in a bit set, lowest first.
    private static IEnumerable<Area> AreasIn(int set, AreaKind kind, int size) =>
        Enumerable.Range(0, size).Where(index => IsIn(set, index)).Select(index => new Area(kind, index));

    // Every way to choose k of the items, in order: (0,1), (0,2) ... (1,2) ..., so lower numbers come first.
    private static IEnumerable<int[]> Combinations(IReadOnlyList<int> items, int k) {
        if (k < 1 || k > items.Count) yield break;

        var picks = new int[k];
        for (int i = 0; i < k; i++) {
            picks[i] = i;
        }

        while (true) {
            yield return picks.Select(i => items[i]).ToArray();

            // Find the rightmost pick that can still move right, move it, and reset the picks after it.
            int position = k - 1;
            while (position >= 0 && picks[position] == items.Count - k + position) {
                position--;
            }

            if (position < 0) yield break;

            picks[position]++;
            for (int i = position + 1; i < k; i++) {
                picks[i] = picks[i - 1] + 1;
            }
        }
    }

    // A cell whose plant would rule out every remaining cell of another bed, row or column can't hold a plant.
    // One area at a time, with all the cells that would starve it, so a hint can explain them together.
    private static LogicStep? WouldStarve(SolverGrid grid) {
        var open = grid.AllCells().Where(cell => grid[cell] == Known.Unknown).ToList();

        foreach (var area in grid.AllAreas()) {
            var cells = grid.CellsOf(area).ToList();
            if (cells.Any(cell => grid[cell] == Known.Plant)) continue;

            var candidates = cells.Where(cell => grid[cell] == Known.Unknown).ToList();
            if (candidates.Count == 0) continue;

            // A cell inside the area would fill it, not starve it, so only cells outside count.
            var ruledOut = open
                .Where(cell => !candidates.Contains(cell) && candidates.All(candidate => Blocks(grid, cell, candidate)))
                .ToList();

            if (ruledOut.Count > 0) {
                return new LogicStep(Technique.WouldStarve, Areas: [area], Plants: [], ruledOut, Reasons: candidates);
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
