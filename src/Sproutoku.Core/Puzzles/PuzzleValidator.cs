namespace Sproutoku.Core.Puzzles;

/// <summary>
/// Checks raw puzzle data against Sproutoku's rules. 
/// Returns every problem found, or an empty list when the puzzle is valid.
/// </summary>
public static class PuzzleValidator {

    /// <summary>
    /// Smallest allowed garden. 4×4 is for the tutorial only.
    /// </summary>
    public const int MinSize = 4;

    /// <summary>
    /// Largest allowed garden.
    /// </summary>
    public const int MaxSize = 10;

    /// <param name="regions">Region id for every cell, as regions[row][column]. Ids run 0 to N−1.</param>
    /// <param name="solution">The correct plant for each region: solution[r] is region r's plant.</param>
    public static IReadOnlyList<PuzzleError> Validate(int[][] regions, IReadOnlyList<Cell> solution) {
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentNullException.ThrowIfNull(solution);

        var errors = new List<PuzzleError>();
        int size = regions.Length;

        // Shape first: every later check assumes a square grid of an allowed size.
        if (regions.Any(row => row.Length != size)) {
            errors.Add(new(PuzzleErrorCode.NotSquare, $"The grid has {size} rows, so every row needs {size} cells."));
            return errors;
        }

        if (size < MinSize || size > MaxSize) {
            errors.Add(new(PuzzleErrorCode.SizeOutOfRange, $"Size {size} is outside the allowed range of {MinSize} to {MaxSize}."));
            return errors;
        }

        CheckRegions(regions, errors);
        if (errors.Count > 0) {
            return errors;
        }

        CheckSolution(regions, solution, errors);
        return errors;
    }

    private static void CheckRegions(int[][] regions, List<PuzzleError> errors) {
        int size = regions.Length;
        var cellsByRegion = new Dictionary<int, List<Cell>>();

        for (int row = 0; row < size; row++) {
            for (int column = 0; column < size; column++) {
                var cell = new Cell(row, column);
                int id = regions[row][column];

                if (id < 0 || id >= size) {
                    errors.Add(new(PuzzleErrorCode.InvalidRegionId, $"Cell {cell} has region id {id}; ids must be 0 to {size - 1}."));
                    continue;
                }

                if (!cellsByRegion.TryGetValue(id, out var cells)) {
                    cells = [];
                    cellsByRegion[id] = cells;
                }

                cells.Add(cell);
            }
        }

        if (errors.Count > 0) {
            return;
        }

        // Every id is 0 to N−1 here, so fewer than N distinct ids means a region is missing.
        if (cellsByRegion.Count != size) {
            errors.Add(new(PuzzleErrorCode.WrongRegionCount, $"Found {cellsByRegion.Count} regions, but a {size}×{size} garden needs exactly {size}."));
            return;
        }

        foreach (var (regionId, regionCells) in cellsByRegion) {
            if (!IsConnected(regionCells)) {
                errors.Add(new(PuzzleErrorCode.DisconnectedRegion, $"Region {regionId} is split into separate pieces."));
            }
        }
    }

    // Flood fill across shared edges. The region is connected if the fill reaches every cell.
    private static bool IsConnected(List<Cell> cells) {
        var unvisited = new HashSet<Cell>(cells);
        var toVisit = new Stack<Cell>();

        toVisit.Push(cells[0]);
        unvisited.Remove(cells[0]);

        while (toVisit.Count > 0) {
            var cell = toVisit.Pop();
            foreach (var neighbor in cell.EdgeNeighbors()) {
                if (unvisited.Remove(neighbor)) {
                    toVisit.Push(neighbor);
                }
            }
        }

        return unvisited.Count == 0;
    }

    private static void CheckSolution(int[][] regions, IReadOnlyList<Cell> solution, List<PuzzleError> errors) {
        int size = regions.Length;

        if (solution.Count != size) {
            errors.Add(new(PuzzleErrorCode.WrongSolutionCount, $"The solution has {solution.Count} plants, but there are {size} regions."));
            return;
        }

        for (int region = 0; region < size; region++) {
            var plant = solution[region];
            if (!plant.IsInside(size) || regions[plant.Row][plant.Column] != region) {
                errors.Add(new(PuzzleErrorCode.SolutionOutsideRegion, $"The plant for region {region} at {plant} is not inside region {region}."));
            }
        }

        if (errors.Count > 0) {
            return;
        }

        // Compare every pair of plants once.
        for (int a = 0; a < size; a++) {
            for (int b = a + 1; b < size; b++) {
                Cell first = solution[a];
                Cell second = solution[b];

                if (first.Row == second.Row) {
                    errors.Add(new(PuzzleErrorCode.SolutionRowConflict, $"Regions {a} and {b} both have their plant in row {first.Row}."));
                }

                if (first.Column == second.Column) {
                    errors.Add(new(PuzzleErrorCode.SolutionColumnConflict, $"Regions {a} and {b} both have their plant in column {first.Column}."));
                }

                if (first.Touches(second)) {
                    errors.Add(new(PuzzleErrorCode.SolutionPlantsTouch, $"The plants for regions {a} and {b} touch at {first} and {second}."));
                }
            }
        }
    }
}
