using Sproutoku.Core.Play;
using Sproutoku.Core.Puzzles;
using Sproutoku.Core.Tests.Puzzles;

namespace Sproutoku.Core.Tests.Play;

public class BoardTests {
    // TestGardens' 5×5. Its solution: (0,0) (1,2) (2,4) (3,1) (4,3).
    private static readonly Cell SolutionCell = new(1, 2);
    private static readonly Cell WrongCell = new(0, 1);

    private static Board NewBoard() =>
        Board.Start(Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution()));

    [Fact]
    public void Start_EveryCellIsEmpty() {
        var board = NewBoard();

        Assert.Equal(25, board.CountOf(CellState.Empty));
        Assert.False(board.IsSolved);
    }

    [Fact]
    public void ToggleMark_EmptyCell_BecomesMarked() {
        var board = NewBoard().ToggleMark(WrongCell);

        Assert.Equal(CellState.Marked, board[WrongCell]);
    }

    [Fact]
    public void ToggleMark_MarkedCell_BecomesEmpty() {
        var board = NewBoard().ToggleMark(WrongCell).ToggleMark(WrongCell);

        Assert.Equal(CellState.Empty, board[WrongCell]);
    }

    [Fact]
    public void ToggleMark_LeavesTheOriginalBoardUnchanged() {
        var original = NewBoard();

        original.ToggleMark(WrongCell);

        Assert.Equal(CellState.Empty, original[WrongCell]);
    }

    [Fact]
    public void TryPlant_SolutionCell_Grows() {
        var result = NewBoard().TryPlant(SolutionCell);

        Assert.Equal(PlantOutcome.Grew, result.Outcome);
        Assert.Equal(CellState.Planted, result.Board[SolutionCell]);
    }

    [Fact]
    public void TryPlant_WrongCell_Wilts() {
        var result = NewBoard().TryPlant(WrongCell);

        Assert.Equal(PlantOutcome.Wilted, result.Outcome);
        Assert.Equal(CellState.Wilted, result.Board[WrongCell]);
    }

    [Fact]
    public void TryPlant_MarkedCell_IsIgnored() {
        var board = NewBoard().ToggleMark(SolutionCell);

        var result = board.TryPlant(SolutionCell);

        Assert.Equal(PlantOutcome.Ignored, result.Outcome);
        Assert.Same(board, result.Board);
    }

    [Fact]
    public void TryPlant_WiltedCell_IsIgnored_SoAMistakeCantCostTwice() {
        var board = NewBoard().TryPlant(WrongCell).Board;

        var result = board.TryPlant(WrongCell);

        Assert.Equal(PlantOutcome.Ignored, result.Outcome);
    }

    [Fact]
    public void ToggleMark_LockedCells_DoNotChange() {
        var board = NewBoard().TryPlant(SolutionCell).Board.TryPlant(WrongCell).Board;

        var after = board.ToggleMark(SolutionCell).ToggleMark(WrongCell);

        Assert.Equal(CellState.Planted, after[SolutionCell]);
        Assert.Equal(CellState.Wilted, after[WrongCell]);
    }

    [Fact]
    public void PlantingEverySolutionCell_SolvesTheGarden_AndStopsInput() {
        var board = NewBoard();
        foreach (var cell in TestGardens.ValidSolution()) {
            board = board.TryPlant(cell).Board;
        }

        Assert.True(board.IsSolved);
        Assert.Same(board, board.ToggleMark(WrongCell));
        Assert.Equal(PlantOutcome.Ignored, board.TryPlant(WrongCell).Outcome);
    }
}
