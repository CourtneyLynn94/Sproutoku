namespace Sproutoku.Core.Puzzles;

/// <summary>
/// A garden: an N×N grid split into N regions, and the one correct plant for each region.
/// The only way to make one is <see cref="Create"/>, which runs the validator, so every Puzzle in the game is known to be valid.
/// </summary>
public sealed class Puzzle
{
    private readonly int[,] _regions;

    private Puzzle(int[,] regions, IReadOnlyList<Cell> solution)
    {
        _regions = regions;
        Solution = solution;
    }

    /// <summary>
    /// N: the number of rows, columns and regions.
    /// </summary>
    public int Size => Solution.Count;

    /// <summary>
    /// The correct plant for each region: Solution[r] is region r's plant.
    /// </summary>
    public IReadOnlyList<Cell> Solution { get; }

    /// <summary>
    /// The region id (0 to N−1) of a cell.
    /// </summary>
    public int RegionAt(Cell cell) => _regions[cell.Row, cell.Column];

    /// <summary>
    /// True if the cell holds its region's plant in the solution.
    /// </summary>
    public bool IsSolutionCell(Cell cell) => Solution[RegionAt(cell)] == cell;

    /// <summary>
    /// Validates the data and builds a puzzle, or throws <see cref="InvalidPuzzleException"/>.
    /// </summary>
    public static Puzzle Create(int[][] regions, IReadOnlyList<Cell> solution)
    {
        var errors = PuzzleValidator.Validate(regions, solution);
        if (errors.Count > 0)
        {
            throw new InvalidPuzzleException(errors);
        }

        // Copy the input so later changes to the caller's arrays can't change this puzzle.
        int size = regions.Length;
        var regionCopy = new int[size, size];
        for (int row = 0; row < size; row++)
        {
            for (int column = 0; column < size; column++)
            {
                regionCopy[row, column] = regions[row][column];
            }
        }

        return new Puzzle(regionCopy, Array.AsReadOnly(solution.ToArray()));
    }
}
