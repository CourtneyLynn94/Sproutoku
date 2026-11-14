namespace Sproutoku.Core.Tests.Game.PowerUps;

public class WateringCanToolTests {
    // TestGardens' 5×5. Its solution: (0,0) (1,2) (2,4) (3,1) (4,3).
    private static Board NewBoard() =>
        Board.Start(Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution()));

    private static Board AmbiguousBoard() =>
        Board.Start(Puzzle.Create(
            TestGardens.Regions("00000", "11111", "22222", "33333", "44444"),
            TestGardens.ValidSolution()));

    [Fact]
    public void WithoutAnyProof_ItReturnsNoCells_EvenWithPlayerMarks() {
        var board = AmbiguousBoard().ToggleMark(new Cell(0, 1));

        Assert.All(ManyRandoms(), random => Assert.Empty(WateringCanTool.CellsFor(board, random)));
    }

    [Fact]
    public void WithPartialProof_ItOnlyRevealsProvenCells() {
        var board = AmbiguousBoard().TryPlant(new Cell(0, 0)).Board;
        // With A1 planted, the only legal column sequences are A C E B D and A D B E C.
        // Every other cell is empty in both solutions.
        var provenEmpty = new[] {
            new Cell(0, 1), new Cell(0, 2), new Cell(0, 3), new Cell(0, 4),
            new Cell(1, 0), new Cell(1, 1), new Cell(1, 4),
            new Cell(2, 0), new Cell(2, 2), new Cell(2, 3),
            new Cell(3, 0), new Cell(3, 2), new Cell(3, 3),
            new Cell(4, 0), new Cell(4, 1), new Cell(4, 4),
        };

        Assert.All(ManyRandoms(), random => {
            var cells = WateringCanTool.CellsFor(board, random);

            Assert.Equal(WateringCanTool.MaxCells, cells.Count);
            Assert.All(cells, cell => Assert.Contains(cell, provenEmpty));
        });
    }

    private static IEnumerable<Random> ManyRandoms() => Enumerable.Range(0, 50).Select(seed => new Random(seed));

    [Fact]
    public void ItRevealsThreeDifferentEmptyCells_NeverAPlant() {
        var board = NewBoard();

        Assert.All(ManyRandoms(), random => {
            var cells = WateringCanTool.CellsFor(board, random);

            Assert.Equal(WateringCanTool.MaxCells, cells.Distinct().Count());
            Assert.All(cells, cell => Assert.False(board.Puzzle.IsSolutionCell(cell)));
        });
    }

    [Fact]
    public void ItNeverPicksThePlayersMarks_OrAWilt() {
        var board = NewBoard().ToggleMark(new Cell(0, 2)).TryPlant(new Cell(0, 1)).Board;

        Assert.All(ManyRandoms(), random => {
            var cells = WateringCanTool.CellsFor(board, random);

            Assert.DoesNotContain(new Cell(0, 2), cells);
            Assert.DoesNotContain(new Cell(0, 1), cells);
        });
    }

    [Fact]
    public void ItDoesntGoInOrder() {
        var picks = ManyRandoms().Select(random => string.Join(",", WateringCanTool.CellsFor(NewBoard(), random))).Distinct();

        Assert.True(picks.Count() > 1);
    }

    [Fact]
    public void WateringUntilNothingIsLeft_RevealsEveryEmptyCell_AndNoPlant() {
        var board = NewBoard();
        var random = new Random(7);

        while (WateringCanTool.CellsFor(board, random) is { Count: > 0 } cells) {
            foreach (var cell in cells) {
                board = board.RevealEmpty(cell);
            }
        }

        Assert.Equal(board.Size * board.Size - board.Size, board.CountOf(CellState.RevealedEmpty));
        Assert.All(board.Puzzle.Solution, plant => Assert.Equal(CellState.Empty, board[plant]));
    }
}
