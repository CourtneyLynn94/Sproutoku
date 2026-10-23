using Sproutoku.Core.Play;
using Sproutoku.Core.Puzzles;
using Sproutoku.Core.Tests.Puzzles;

namespace Sproutoku.Core.Tests.Play;

public class BoardTests {
    // TestGardens' 5×5. Its solution: (0,0) (1,2) (2,4) (3,1) (4,3).
    private static readonly Cell SolutionCell = new(1, 2);
    private static readonly Cell WrongCell = new(0, 1);
    private static readonly Cell AnotherWrongCell = new(0, 2);

    private static Board NewBoard() =>
        Board.Start(Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution()));

    #region Toggle Marks
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
    public void ToggleMark_LockedCells_DoNotChange() {
        var board = NewBoard().TryPlant(SolutionCell).Board.TryPlant(WrongCell).Board;

        var after = board.ToggleMark(SolutionCell).ToggleMark(WrongCell);

        Assert.Equal(CellState.Planted, after[SolutionCell]);
        Assert.Equal(CellState.Wilted, after[WrongCell]);
    }
    #endregion

    #region Planting
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
    public void TryPlant_MarkedSolutionCell_Grows() {
        var result = NewBoard().ToggleMark(SolutionCell).TryPlant(SolutionCell);

        Assert.Equal(PlantOutcome.Grew, result.Outcome);
        Assert.Equal(CellState.Planted, result.Board[SolutionCell]);
    }

    [Fact]
    public void TryPlant_MarkedWrongCell_Wilts_AndSpendsASun() {
        var result = NewBoard().ToggleMark(WrongCell).TryPlant(WrongCell);

        Assert.Equal(PlantOutcome.Wilted, result.Outcome);
        Assert.Equal(CellState.Wilted, result.Board[WrongCell]);
        Assert.Equal(Board.DefaultSuns - 1, result.Board.SunsLeft);
    }

    [Fact]
    public void TryPlant_WiltedCell_IsIgnored_SoAMistakeCantCostTwice() {
        var board = NewBoard().TryPlant(WrongCell).Board;

        var result = board.TryPlant(WrongCell);

        Assert.Equal(PlantOutcome.Ignored, result.Outcome);
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
    #endregion

    #region Sunlight
    [Fact]
    public void Start_HasTheDefaultSuns() {
        var board = NewBoard();

        Assert.Equal(Board.DefaultSuns, board.StartingSuns);
        Assert.Equal(Board.DefaultSuns, board.SunsLeft);
    }

    [Fact]
    public void Start_UsesTheSunsItIsGiven() {
        var puzzle = Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution());

        var board = Board.Start(puzzle, suns: 1);

        Assert.Equal(1, board.StartingSuns);
        Assert.Equal(1, board.SunsLeft);
    }

    [Fact]
    public void Start_WithNoSuns_Throws() {
        var puzzle = Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution());

        Assert.Throws<ArgumentOutOfRangeException>(() => Board.Start(puzzle, suns: 0));
    }

    [Fact]
    public void WrongPlant_SpendsOneSun() {
        var board = NewBoard().TryPlant(WrongCell).Board;

        Assert.Equal(Board.DefaultSuns - 1, board.SunsLeft);
        Assert.Equal(Board.DefaultSuns, board.StartingSuns);
    }

    [Fact]
    public void CorrectPlantsAndMarks_KeepEverySun() {
        var board = NewBoard().TryPlant(SolutionCell).Board.ToggleMark(WrongCell);

        Assert.Equal(Board.DefaultSuns, board.SunsLeft);
    }

    [Fact]
    public void IgnoredPlant_KeepsEverySun() {
        var board = NewBoard().TryPlant(WrongCell).Board;

        var result = board.TryPlant(WrongCell);

        Assert.Equal(PlantOutcome.Ignored, result.Outcome);
        Assert.Equal(Board.DefaultSuns - 1, result.Board.SunsLeft);
    }

    [Fact]
    public void Suns_NeverGoBelowZero() {
        var puzzle = Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution());

        var board = Board.Start(puzzle, suns: 1).TryPlant(WrongCell).Board.TryPlant(AnotherWrongCell).Board;

        Assert.Equal(0, board.SunsLeft);
    }
    #endregion

    #region Level
    [Fact]
    public void SpendingTheLastSun_EndsTheGarden_AndLocksIt() {
        var puzzle = Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution());
        var board = Board.Start(puzzle, suns: 1).TryPlant(WrongCell).Board;

        Assert.True(board.IsOutOfSun);
        Assert.True(board.IsFinished);
        Assert.False(board.IsSolved);
        Assert.Same(board, board.ToggleMark(AnotherWrongCell));
        Assert.Equal(PlantOutcome.Ignored, board.TryPlant(SolutionCell).Outcome);
    }

    [Fact]
    public void Mistakes_CountsWrongPlants() {
        var board = NewBoard()
            .TryPlant(WrongCell).Board
            .TryPlant(AnotherWrongCell).Board
            .TryPlant(SolutionCell).Board;

        Assert.Equal(2, board.Mistakes);
    }

    [Fact]
    public void Restart_GivesAFreshBoardForTheSameGarden() {
        var puzzle = Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution());
        var played = Board.Start(puzzle, suns: 1).ToggleMark(SolutionCell).TryPlant(WrongCell).Board;

        var fresh = played.Restart();

        Assert.Same(puzzle, fresh.Puzzle);
        Assert.Equal(1, fresh.StartingSuns);
        Assert.Equal(1, fresh.SunsLeft);
        Assert.Equal(25, fresh.CountOf(CellState.Empty));
    }
    #endregion
}
