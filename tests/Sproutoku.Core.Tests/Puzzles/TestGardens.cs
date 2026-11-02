using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Tests.Puzzles;

/// <summary>
/// Small gardens shared by the puzzle tests.
/// </summary>
internal static class TestGardens {
    /// <summary>
    /// Builds a region grid from strings: one string per row, one digit (region id) per cell.
    /// </summary>
    public static int[][] Regions(params string[] rows) => rows.Select(row => row.Select(ch => ch - '0').ToArray()).ToArray();

    /// <summary>
    /// A valid 5×5 garden with exactly one solution.
    /// </summary>
    public static int[][] ValidRegions() => Regions(
        "00111",
        "11111",
        "33122",
        "33344",
        "33444"
    );

    /// <summary>
    /// The solution for ValidRegions. The index is the region id.
    /// </summary>
    public static Cell[] ValidSolution() => [
        new(0, 0), // region 0
        new(1, 2), // region 1
        new(2, 4), // region 2
        new(3, 1), // region 3
        new(4, 3), // region 4
    ];
}
