namespace Sproutoku.Core.Tests.Game.PowerUps;

public class SeedlingToolTests {
    // TestGardens' 5×5. Beds 0 and 2 have two cells each; bed 0 comes first in reading order.
    // Its solution: (0,0) (1,2) (2,4) (3,1) (4,3).
    private static Board NewBoard() =>
        Board.Start(Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution()));

    [Fact]
    public void OnAFreshGarden_ItGrowsTheSmallestBed_FirstInReadingOrder() {
        Assert.Equal(new Cell(0, 0), SeedlingTool.CellFor(NewBoard()));
    }

    [Fact]
    public void ABedWithFewerOpenCells_ComesFirst() {
        var board = NewBoard().TryPlant(new Cell(2, 3)).Board; // A wilt leaves bed 2 one open cell.

        Assert.Equal(new Cell(2, 4), SeedlingTool.CellFor(board));
    }

    [Fact]
    public void AMark_StillCountsAsOpen() {
        var board = NewBoard().ToggleMark(new Cell(0, 1)); // A ✕ might be wrong, so bed 0 still has two open cells.

        Assert.Equal(new Cell(0, 0), SeedlingTool.CellFor(board));
    }

    [Fact]
    public void ABedThatHasItsPlant_IsSkipped() {
        var board = NewBoard().TryPlant(new Cell(0, 0)).Board;

        Assert.Equal(new Cell(2, 4), SeedlingTool.CellFor(board));
    }

    [Fact]
    public void ASolvedGarden_HasNothingToGrow() {
        var board = NewBoard();
        foreach (var cell in board.Puzzle.Solution) {
            board = board.TryPlant(cell).Board;
        }

        Assert.Null(SeedlingTool.CellFor(board));
    }
}
