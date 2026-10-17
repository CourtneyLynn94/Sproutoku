using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Tests.Puzzles;

public class PuzzleTests
{
    [Fact]
    public void Create_ValidGarden_ExposesRegionsAndSolution()
    {
        var puzzle = Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution());

        Assert.Equal(5, puzzle.Size);
        Assert.Equal(1, puzzle.RegionAt(new Cell(2, 2)));
        Assert.Equal(new Cell(2, 4), puzzle.Solution[2]);
        Assert.True(puzzle.IsSolutionCell(new Cell(3, 1)));
        Assert.False(puzzle.IsSolutionCell(new Cell(3, 2)));
    }

    [Fact]
    public void Create_InvalidGarden_ThrowsWithErrors()
    {
        var solution = TestGardens.ValidSolution()[..4];

        var exception = Assert.Throws<InvalidPuzzleException>(
            () => Puzzle.Create(TestGardens.ValidRegions(), solution)
        );

        Assert.Contains(exception.Errors, e => e.Code == PuzzleErrorCode.WrongSolutionCount);
    }

    [Fact]
    public void Create_CopiesInput_SoLaterChangesDontAffectThePuzzle()
    {
        var regions = TestGardens.ValidRegions();
        var solution = TestGardens.ValidSolution();
        var puzzle = Puzzle.Create(regions, solution);

        regions[0][0] = 4;
        solution[0] = new Cell(0, 1);

        Assert.Equal(0, puzzle.RegionAt(new Cell(0, 0)));
        Assert.Equal(new Cell(0, 0), puzzle.Solution[0]);
    }
}
