using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Tests.Puzzles;

public class StarterGardensTests {
    [Fact]
    public void AllStarterGardens_PassTheValidator() {
        // Loading the list runs every garden through Puzzle.Create, which validates it.
        Assert.NotEmpty(StarterGardens.All);
    }

    [Fact]
    public void Includes5x5And6x6And7x7() {
        var sizes = StarterGardens.All.Select(p => p.Size).ToList();

        Assert.Contains(5, sizes);
        Assert.Contains(6, sizes);
        Assert.Contains(7, sizes);
    }

    [Fact]
    public void AtLeastOneGardenHasA1x1Region() {
        Assert.Contains(StarterGardens.All, HasSingleCellRegion);
    }

    private static bool HasSingleCellRegion(Puzzle puzzle) =>
        AllCells(puzzle)
            .GroupBy(puzzle.RegionAt)
            .Any(region => region.Count() == 1);

    private static IEnumerable<Cell> AllCells(Puzzle puzzle) =>
        from row in Enumerable.Range(0, puzzle.Size)
        from column in Enumerable.Range(0, puzzle.Size)
        select new Cell(row, column);
}
