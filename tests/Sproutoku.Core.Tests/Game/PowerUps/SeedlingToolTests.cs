namespace Sproutoku.Core.Tests.Game.PowerUps;

public class SeedlingToolTests {
    // TestGardens' 5×5. Its solution: (0,0) (1,2) (2,4) (3,1) (4,3).
    private static Board NewBoard() =>
        Board.Start(Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution()));

    private static IEnumerable<Random> ManyRandoms() => Enumerable.Range(0, 50).Select(seed => new Random(seed));

    [Fact]
    public void ItAlwaysPicksACorrectPlant() {
        var board = NewBoard();

        Assert.All(ManyRandoms(), random => Assert.True(board.Puzzle.IsSolutionCell(SeedlingTool.CellFor(board, random)!.Value)));
    }

    [Fact]
    public void ABedThatHasItsPlant_IsNeverPicked() {
        var board = NewBoard().TryPlant(new Cell(0, 0)).Board;

        Assert.All(ManyRandoms(), random => Assert.NotEqual(new Cell(0, 0), SeedlingTool.CellFor(board, random)));
    }

    [Fact]
    public void ItPicksDifferentBeds() {
        var picks = ManyRandoms().Select(random => SeedlingTool.CellFor(NewBoard(), random)).Distinct();

        Assert.True(picks.Count() > 1);
    }

    [Fact]
    public void ASolvedGarden_HasNothingToGrow() {
        var board = NewBoard();
        foreach (var cell in board.Puzzle.Solution) {
            board = board.TryPlant(cell).Board;
        }

        Assert.Null(SeedlingTool.CellFor(board, new Random(1)));
    }
}
