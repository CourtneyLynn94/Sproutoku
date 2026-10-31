using Sproutoku.Core.Play;
using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Tests.Play;

public class GameSessionTests {
    // Taps on one cell this far apart are two single taps; this close together, a double tap.
    private const long SlowGap = 1000;
    private const long QuickGap = 100;

    // A pretend clock. xUnit creates a new instance for every test, so each test starts fresh.
    private long _now = 10_000;

    private static GameSession NewGame() => GameSession.Resume(null);

    private TapResult SingleTap(GameSession session, Cell cell) {
        _now += SlowGap;
        return session.Tap(cell, _now);
    }

    private TapResult DoubleTap(GameSession session, Cell cell) {
        _now += SlowGap;
        session.Tap(cell, _now);
        _now += QuickGap;
        return session.Tap(cell, _now);
    }

    // Double-taps every solution cell and returns what the last tap did.
    private TapResult Solve(GameSession session) {
        TapResult? last = null;
        foreach (var cell in session.Board.Puzzle.Solution) {
            last = DoubleTap(session, cell);
        }

        Assert.NotNull(last);
        return last;
    }

    // Wilts wrong cells until the suns run out and returns what the last tap did.
    private TapResult Lose(GameSession session) {
        TapResult? last = null;
        foreach (var cell in WrongCells(session.Board.Puzzle).Take(session.Board.StartingSuns)) {
            last = DoubleTap(session, cell);
        }

        Assert.NotNull(last);
        return last;
    }

    private static IEnumerable<Cell> WrongCells(Puzzle puzzle) =>
        from row in Enumerable.Range(0, puzzle.Size)
        from column in Enumerable.Range(0, puzzle.Size)
        let cell = new Cell(row, column)
        where !puzzle.IsSolutionCell(cell)
        select cell;

    #region Starting
    [Fact]
    public void NewGame_StartsAtGarden1_Fresh() {
        var session = NewGame();

        Assert.Equal(1, session.Level);
        Assert.Equal(session.Board.Size * session.Board.Size, session.Board.CountOf(CellState.Empty));
        Assert.False(session.Board.IsFinished);
    }

    [Fact]
    public void SaveAndResume_PicksUpTheSameGame() {
        var session = NewGame();
        session.GoToLevel(2);
        SingleTap(session, WrongCells(session.Board.Puzzle).First());
        DoubleTap(session, session.Board.Puzzle.Solution[0]);

        var resumed = GameSession.Resume(session.ToSave());

        Assert.Equal(2, resumed.Level);
        Assert.Equal(session.Board.CellsToText(), resumed.Board.CellsToText());
    }
    #endregion

    #region Tapping
    [Fact]
    public void SingleTap_PlacesAMark() {
        var session = NewGame();
        var cell = WrongCells(session.Board.Puzzle).First();

        var result = SingleTap(session, cell);

        Assert.Equal(TapOutcome.MarkToggled, result.Outcome);
        Assert.Equal(CellState.Marked, session.Board[cell]);
    }

    [Fact]
    public void TwoSlowTaps_PlaceAndClearAMark() {
        var session = NewGame();
        var cell = WrongCells(session.Board.Puzzle).First();

        SingleTap(session, cell);
        var result = SingleTap(session, cell);

        Assert.Equal(TapOutcome.MarkToggled, result.Outcome);
        Assert.Equal(CellState.Empty, session.Board[cell]);
    }

    [Fact]
    public void DoubleTap_OnTheSolution_Grows_AndLeavesNoMark() {
        var session = NewGame();
        var cell = session.Board.Puzzle.Solution[0];

        var result = DoubleTap(session, cell);

        Assert.Equal(TapOutcome.Grew, result.Outcome);
        Assert.Equal(CellState.Planted, session.Board[cell]);
        Assert.Equal(0, session.Board.CountOf(CellState.Marked));
    }

    [Fact]
    public void DoubleTap_OnAWrongCell_Wilts_AndSpendsASun() {
        var session = NewGame();
        var cell = WrongCells(session.Board.Puzzle).First();

        var result = DoubleTap(session, cell);

        Assert.Equal(TapOutcome.Wilted, result.Outcome);
        Assert.Equal(CellState.Wilted, session.Board[cell]);
        Assert.Equal(Board.DefaultSuns - 1, session.Board.SunsLeft);
    }

    [Fact]
    public void DoubleTap_OnAMark_Plants() {
        var session = NewGame();
        var cell = session.Board.Puzzle.Solution[0];
        SingleTap(session, cell);

        var result = DoubleTap(session, cell);

        Assert.Equal(TapOutcome.Grew, result.Outcome);
        Assert.Equal(CellState.Planted, session.Board[cell]);
    }

    [Fact]
    public void ChangingGarden_ForgetsTheLastTap() {
        var session = NewGame();
        var cell = new Cell(0, 0);
        session.Tap(cell, 1000);
        session.GoToLevel(2);

        var result = session.Tap(cell, 1050);

        Assert.Equal(TapOutcome.MarkToggled, result.Outcome);
        Assert.Equal(CellState.Marked, session.Board[cell]);
    }
    #endregion

    #region Dragging
    private static IEnumerable<Cell> RowOf(GameSession session, int row) =>
        Enumerable.Range(0, session.Board.Size).Select(column => new Cell(row, column));

    private static void Drag(GameSession session, params Cell[] cells) {
        session.BeginDrag();
        foreach (var cell in cells) {
            session.DragOver(cell);
        }

        session.EndDrag();
    }

    [Fact]
    public void Drag_FromAnEmptyCell_MarksEveryEmptyCellItPasses_WithoutPlantingOrSpendingSuns() {
        var session = NewGame();

        Drag(session, [.. RowOf(session, 0)]);

        Assert.All(RowOf(session, 0), cell => Assert.Equal(CellState.Marked, session.Board[cell]));
        Assert.Equal(0, session.Board.CountOf(CellState.Planted));
        Assert.Equal(session.Board.StartingSuns, session.Board.SunsLeft);
    }

    [Fact]
    public void Drag_FromAMark_ClearsMarks_AndLeavesEmptyCellsEmpty() {
        var session = NewGame();
        SingleTap(session, new Cell(0, 0));
        SingleTap(session, new Cell(0, 2));

        Drag(session, new Cell(0, 0), new Cell(0, 1), new Cell(0, 2));

        Assert.Equal(CellState.Empty, session.Board[new Cell(0, 0)]);
        Assert.Equal(CellState.Empty, session.Board[new Cell(0, 1)]);
        Assert.Equal(CellState.Empty, session.Board[new Cell(0, 2)]);
    }

    [Fact]
    public void MarkingDrag_LeavesMarksAndLockedCellsAlone() {
        var session = NewGame();
        DoubleTap(session, new Cell(1, 1)); // planted (a solution cell)
        DoubleTap(session, new Cell(1, 0)); // wilted
        SingleTap(session, new Cell(1, 3)); // ✕

        Drag(session, new Cell(1, 2), new Cell(1, 1), new Cell(1, 0), new Cell(1, 3), new Cell(1, 4));

        Assert.Equal(CellState.Marked, session.Board[new Cell(1, 2)]);
        Assert.Equal(CellState.Planted, session.Board[new Cell(1, 1)]);
        Assert.Equal(CellState.Wilted, session.Board[new Cell(1, 0)]);
        Assert.Equal(CellState.Marked, session.Board[new Cell(1, 3)]);
        Assert.Equal(CellState.Marked, session.Board[new Cell(1, 4)]);
    }

    [Fact]
    public void Drag_StartingOnALockedCell_TakesItsModeFromTheFirstEmptyOrMarkedCell() {
        var session = NewGame();
        DoubleTap(session, new Cell(0, 3)); // planted
        SingleTap(session, new Cell(0, 4)); // ✕

        // Starts on the plant, then reaches the ✕ first, so it clears.
        Drag(session, new Cell(0, 3), new Cell(0, 4), new Cell(0, 2));

        Assert.Equal(CellState.Planted, session.Board[new Cell(0, 3)]);
        Assert.Equal(CellState.Empty, session.Board[new Cell(0, 4)]);
        Assert.Equal(CellState.Empty, session.Board[new Cell(0, 2)]);
    }

    [Fact]
    public void Drag_PassingACellTwice_DoesNotFlipItBack() {
        var session = NewGame();

        Drag(session, new Cell(2, 0), new Cell(2, 1), new Cell(2, 0));

        Assert.Equal(CellState.Marked, session.Board[new Cell(2, 0)]);
        Assert.Equal(CellState.Marked, session.Board[new Cell(2, 1)]);
    }

    [Fact]
    public void DragOver_WithoutADragInProgress_DoesNothing() {
        var session = NewGame();
        var cell = new Cell(2, 0);

        Assert.False(session.DragOver(cell));

        session.BeginDrag();
        session.EndDrag();

        Assert.False(session.DragOver(cell));
        Assert.Equal(CellState.Empty, session.Board[cell]);
    }

    [Fact]
    public void Drag_OnAFinishedGarden_DoesNothing() {
        var session = NewGame();
        Lose(session);

        session.BeginDrag();

        Assert.False(session.DragOver(session.Board.Puzzle.Solution[0]));
    }

    [Fact]
    public void Drag_EndsAHalfFinishedDoubleTap() {
        var session = NewGame();
        var cell = new Cell(0, 0);
        session.Tap(cell, 1000);

        session.BeginDrag();
        session.EndDrag();
        var result = session.Tap(cell, 1050);

        // Without the drag this would have been a double tap and a plant attempt; now it just clears the ✕.
        Assert.Equal(TapOutcome.MarkToggled, result.Outcome);
        Assert.Equal(CellState.Empty, session.Board[cell]);
    }
    #endregion

    #region Ending a garden
    [Fact]
    public void PlantingTheLastPlant_EndsTheGarden() {
        var session = NewGame();

        var result = Solve(session);

        Assert.True(result.EndedGarden);
        Assert.True(session.Board.IsSolved);
    }

    [Fact]
    public void SpendingTheLastSun_EndsTheGarden_AndLaterTapsAreIgnored() {
        var session = NewGame();

        var result = Lose(session);

        Assert.True(result.EndedGarden);
        Assert.True(session.Board.IsOutOfSun);
        Assert.Equal(TapOutcome.Ignored, SingleTap(session, session.Board.Puzzle.Solution[0]).Outcome);
    }
    #endregion

    #region Moving between gardens
    [Fact]
    public void TryAdvance_MidGarden_DoesNothing() {
        var session = NewGame();

        Assert.False(session.TryAdvance());
        Assert.Equal(1, session.Level);
    }

    [Fact]
    public void TryAdvance_AfterSolving_MovesOnExactlyOnce() {
        var session = NewGame();
        Solve(session);

        Assert.True(session.TryAdvance());
        Assert.False(session.TryAdvance());
        Assert.Equal(2, session.Level);
        Assert.Same(GardenLevels.ForLevel(2), session.Board.Puzzle);
        Assert.False(session.Board.IsSolved);
    }

    [Fact]
    public void TryRestart_MidGarden_DoesNothing() {
        var session = NewGame();
        var cell = WrongCells(session.Board.Puzzle).First();
        SingleTap(session, cell);

        Assert.False(session.CanRestart);
        Assert.False(session.TryRestart());
        Assert.Equal(CellState.Marked, session.Board[cell]);
    }

    [Fact]
    public void TryRestart_AfterLosing_StartsTheSameGardenFresh() {
        var session = NewGame();
        Lose(session);

        Assert.True(session.TryRestart());
        Assert.Equal(1, session.Level);
        Assert.Equal(session.Board.StartingSuns, session.Board.SunsLeft);
        Assert.Equal(0, session.Board.Mistakes);
    }

    [Fact]
    public void TryRestart_AfterSolving_WithAnotherGardenWaiting_DoesNothing() {
        var session = NewGame();
        Solve(session);

        Assert.False(session.TryRestart());
        Assert.True(session.Board.IsSolved);
    }

    [Fact]
    public void TryRestart_AfterSolvingTheLastGarden_PlaysItAgain() {
        int lastLevel = StarterGardens.All.Count;
        var session = NewGame();
        session.GoToLevel(lastLevel);
        Solve(session);

        Assert.False(session.HasNextLevel);
        Assert.False(session.TryAdvance());
        Assert.True(session.TryRestart());
        Assert.Equal(lastLevel, session.Level);
        Assert.False(session.Board.IsSolved);
    }

    [Fact]
    public void GoToLevel_WithoutAGarden_DoesNothing() {
        var session = NewGame();

        Assert.False(session.GoToLevel(99));
        Assert.Equal(1, session.Level);
    }
    #endregion
}
