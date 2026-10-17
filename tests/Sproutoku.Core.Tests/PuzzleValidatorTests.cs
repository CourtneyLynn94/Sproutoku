using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Tests.Puzzles;

public class PuzzleValidatorTests
{
    [Fact]
    public void ValidGarden_HasNoErrors()
    {
        var errors = PuzzleValidator.Validate(TestGardens.ValidRegions(), TestGardens.ValidSolution());

        Assert.Empty(errors);
    }

    [Fact]
    public void TutorialSize4_IsAllowed()
    {
        var regions = TestGardens.Regions(
            "0011",
            "0011",
            "2231",
            "2333"
        );
        Cell[] solution = [new(0, 1), new(1, 3), new(2, 0), new(3, 2)];

        Assert.Empty(PuzzleValidator.Validate(regions, solution));
    }

    [Fact]
    public void RejectsMoreColumnsThanRows()
    {
        var regions = TestGardens.Regions(
            "00111",
            "11111",
            "33122",
            "33344"
        );

        AssertRejects(regions, TestGardens.ValidSolution(), PuzzleErrorCode.NotSquare);
    }

    [Fact]
    public void RejectsRaggedRows()
    {
        var regions = TestGardens.Regions(
            "0011",
            "11111",
            "33122",
            "33344",
            "33444"
        );

        AssertRejects(regions, TestGardens.ValidSolution(), PuzzleErrorCode.NotSquare);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void RejectsSizeOutsideAllowedRange(int size)
    {
        // Each row is its own region.
        // The validator stops at the size check, so the empty solution doesn't add other errors.
        int[][] regions = Enumerable.Range(0, size)
                          .Select(row => Enumerable.Repeat(row, size).ToArray())
                          .ToArray();

        AssertRejects(regions, [], PuzzleErrorCode.SizeOutOfRange);
    }

    [Fact]
    public void RejectsRegionIdOutsideRange()
    {
        var regions = TestGardens.Regions(
            "00111",
            "11111",
            "33122",
            "33344",
            "33447"
        );

        AssertRejects(regions, TestGardens.ValidSolution(), PuzzleErrorCode.InvalidRegionId);
    }

    [Fact]
    public void RejectsTooFewRegions()
    {
        // Region 2 merged into region 1, leaving four regions.
        var regions = TestGardens.Regions(
            "00111",
            "11111",
            "33111",
            "33344",
            "33444"
        );

        AssertRejects(regions, TestGardens.ValidSolution(), PuzzleErrorCode.WrongRegionCount);
    }

    [Fact]
    public void RejectsDisconnectedRegion()
    {
        // The bottom-left cell belongs to region 0, cut off from the rest of it.
        var regions = TestGardens.Regions(
            "00111",
            "11111",
            "33122",
            "33344",
            "03444"
        );

        AssertRejects(regions, TestGardens.ValidSolution(), PuzzleErrorCode.DisconnectedRegion);
    }

    [Fact]
    public void RejectsRegionJoinedOnlyAtACorner()
    {
        // Region 2's two cells, (2,4) and (3,3), touch only diagonally.
        var regions = TestGardens.Regions(
            "00111",
            "11111",
            "33112",
            "33324",
            "33444"
        );

        AssertRejects(regions, TestGardens.ValidSolution(), PuzzleErrorCode.DisconnectedRegion);
    }

    [Fact]
    public void RejectsMissingPlant()
    {
        var solution = TestGardens.ValidSolution()[..4];

        AssertRejects(TestGardens.ValidRegions(), solution, PuzzleErrorCode.WrongSolutionCount);
    }

    [Fact]
    public void RejectsPlantOutsideItsRegion()
    {
        var solution = TestGardens.ValidSolution();
        solution[0] = new Cell(2, 2); // a region 1 cell

        AssertRejects(TestGardens.ValidRegions(), solution, PuzzleErrorCode.SolutionOutsideRegion);
    }

    [Fact]
    public void RejectsPlantOffTheGrid()
    {
        var solution = TestGardens.ValidSolution();
        solution[0] = new Cell(-1, 0);

        AssertRejects(TestGardens.ValidRegions(), solution, PuzzleErrorCode.SolutionOutsideRegion);
    }

    [Fact]
    public void RejectsTwoPlantsInOneRow()
    {
        var solution = TestGardens.ValidSolution();
        solution[1] = new Cell(0, 2); // row 0, same as region 0's plant

        AssertRejects(TestGardens.ValidRegions(), solution, PuzzleErrorCode.SolutionRowConflict);
    }

    [Fact]
    public void RejectsTwoPlantsInOneColumn()
    {
        var solution = TestGardens.ValidSolution();
        solution[3] = new Cell(3, 0); // column 0, same as region 0's plant

        AssertRejects(TestGardens.ValidRegions(), solution, PuzzleErrorCode.SolutionColumnConflict);
    }

    [Fact]
    public void RejectsTouchingPlants()
    {
        var solution = TestGardens.ValidSolution();
        solution[2] = new Cell(2, 3); // touches region 1's plant at (1,2) diagonally
        solution[4] = new Cell(4, 4); // keeps every row and column different

        AssertRejects(TestGardens.ValidRegions(), solution, PuzzleErrorCode.SolutionPlantsTouch);
    }

    // Checks the puzzle fails, and that it fails only for the expected reason.
    private static void AssertRejects(int[][] regions, IReadOnlyList<Cell> solution, PuzzleErrorCode expected)
    {
        var errors = PuzzleValidator.Validate(regions, solution);

        Assert.NotEmpty(errors);
        Assert.All(errors, error => Assert.Equal(expected, error.Code));
    }
}
