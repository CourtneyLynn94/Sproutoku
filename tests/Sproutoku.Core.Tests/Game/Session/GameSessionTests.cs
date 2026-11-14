using Sproutoku.Core.Game.Levels;

namespace Sproutoku.Core.Tests.Game.Session;

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
        var plant = session.Board.Puzzle.Solution[0];

        // A row has one plant, so every other cell in the plant's row is wrong.
        var others = RowOf(session, plant.Row).Where(cell => cell != plant).ToList();
        DoubleTap(session, plant);      // planted
        DoubleTap(session, others[0]);  // wilted
        SingleTap(session, others[1]);  // ✕

        // Starts on an empty cell, so it marks; then crosses the plant, the wilted cell, the ✕ and the rest of the row.
        Drag(session, [others[2], plant, others[0], others[1], .. others.Skip(3)]);

        Assert.Equal(CellState.Planted, session.Board[plant]);
        Assert.Equal(CellState.Wilted, session.Board[others[0]]);
        Assert.Equal(CellState.Marked, session.Board[others[1]]);
        Assert.All(others.Skip(2), cell => Assert.Equal(CellState.Marked, session.Board[cell]));
    }

    [Fact]
    public void Drag_StartingOnALockedCell_TakesItsModeFromTheFirstEmptyOrMarkedCell() {
        var session = NewGame();
        var plant = session.Board.Puzzle.Solution[0];
        var others = RowOf(session, plant.Row).Where(cell => cell != plant).ToList();
        DoubleTap(session, plant);      // planted
        SingleTap(session, others[0]);  // ✕

        // Starts on the plant, then reaches the ✕ first, so it clears.
        Drag(session, plant, others[0], others[1]);

        Assert.Equal(CellState.Planted, session.Board[plant]);
        Assert.Equal(CellState.Empty, session.Board[others[0]]);
        Assert.Equal(CellState.Empty, session.Board[others[1]]);
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

    #region Keyboard
    [Fact]
    public void KeyboardToggleMark_TogglesStraightAway_EvenTwiceInARow() {
        var session = NewGame();
        var cell = new Cell(0, 0);

        Assert.Equal(TapOutcome.MarkToggled, session.ToggleMark(cell).Outcome);
        Assert.Equal(CellState.Marked, session.Board[cell]);

        Assert.Equal(TapOutcome.MarkToggled, session.ToggleMark(cell).Outcome);
        Assert.Equal(CellState.Empty, session.Board[cell]);
    }

    [Fact]
    public void KeyboardPlant_OnTheSolution_Grows() {
        var session = NewGame();
        var cell = session.Board.Puzzle.Solution[0];

        Assert.Equal(TapOutcome.Grew, session.Plant(cell).Outcome);
        Assert.Equal(CellState.Planted, session.Board[cell]);
    }

    [Fact]
    public void KeyboardPlant_OnAWrongCell_Wilts_AndSpendsASun() {
        var session = NewGame();
        var cell = WrongCells(session.Board.Puzzle).First();

        Assert.Equal(TapOutcome.Wilted, session.Plant(cell).Outcome);
        Assert.Equal(Board.DefaultSuns - 1, session.Board.SunsLeft);
    }

    [Fact]
    public void KeyboardPlant_OnAMark_Plants() {
        var session = NewGame();
        var cell = session.Board.Puzzle.Solution[0];
        session.ToggleMark(cell);

        Assert.Equal(TapOutcome.Grew, session.Plant(cell).Outcome);
    }

    [Fact]
    public void KeyboardPlanting_EveryBed_EndsTheGarden() {
        var session = NewGame();
        TapResult? last = null;
        foreach (var cell in session.Board.Puzzle.Solution) {
            last = session.Plant(cell);
        }

        Assert.NotNull(last);
        Assert.True(last.EndedGarden);
        Assert.True(session.Board.IsSolved);
    }

    [Fact]
    public void KeyboardActions_OnAFinishedGarden_AreIgnored() {
        var session = NewGame();
        Lose(session);
        var cell = session.Board.Puzzle.Solution[0];

        Assert.Equal(TapOutcome.Ignored, session.ToggleMark(cell).Outcome);
        Assert.Equal(TapOutcome.Ignored, session.Plant(cell).Outcome);
    }

    [Fact]
    public void KeyboardAction_EndsAHalfFinishedDoubleTap() {
        var session = NewGame();
        var cell = new Cell(0, 0);
        session.Tap(cell, 1000);

        session.ToggleMark(new Cell(4, 4));
        var result = session.Tap(cell, 1050);

        // Without the keypress this would have been a double tap; now it just clears the ✕.
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
        Assert.Equal(TestGardens.BedRows(GardenLevels.ForLevel(2).Puzzle), TestGardens.BedRows(session.Board.Puzzle));
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
    public void GoToLevel_BelowOne_DoesNothing() {
        var session = NewGame();

        Assert.False(session.GoToLevel(0));
        Assert.Equal(1, session.Level);
    }

    [Fact]
    public void GoToLevel_WithAGrownGarden_StartsItFresh_AndEndsAnExtraGarden() {
        var session = NewGame();
        session.PlayExtraGarden(ExtraGarden());
        var garden = GardenLevels.ForLevel(5);

        session.GoToLevel(garden);

        Assert.Equal(5, session.Level);
        Assert.False(session.IsExtraGarden);
        Assert.Same(garden.Puzzle, session.Board.Puzzle);
        Assert.Equal(garden.Recipe.Suns, session.Board.SunsLeft);
    }

    [Fact]
    public void TryAdvance_ToAPreparedGarden_MovesOnOnce() {
        var session = NewGame();
        var next = GardenLevels.ForLevel(2);
        Solve(session);

        Assert.True(session.TryAdvance(next));
        Assert.Equal(2, session.Level);
        Assert.Same(next.Puzzle, session.Board.Puzzle);
        Assert.False(session.TryAdvance(next)); // Level 2 is unsolved, and it isn't the next level any more.
    }

    [Fact]
    public void TryAdvance_ToAPreparedGarden_RefusesAnUnsolvedGarden_OrTheWrongLevel() {
        var session = NewGame();

        Assert.False(session.TryAdvance(GardenLevels.ForLevel(2))); // Mid-garden.

        Solve(session);
        Assert.False(session.TryAdvance(GardenLevels.ForLevel(3))); // Not the next level.
        Assert.Equal(1, session.Level);
    }

    [Fact]
    public void TryAdvance_ToAPreparedGarden_DoesNothing_DuringAnExtraGarden() {
        var session = NewGame();
        session.PlayExtraGarden(ExtraGarden());
        Solve(session);

        Assert.False(session.TryAdvance(GardenLevels.ForLevel(2)));
        Assert.True(session.IsExtraGarden);
    }
    #endregion

    #region Endless levels
    [Fact]
    public void SolvingAGarden_AlwaysMovesOn() {
        var session = NewGame();
        session.GoToLevel(30);
        Solve(session);

        Assert.False(session.CanRestart);
        Assert.True(session.TryAdvance());
        Assert.Equal(31, session.Level);
        Assert.Equal(session.LevelGarden.Recipe.Size, session.Board.Size);
    }

    [Fact]
    public void AOneSunLevel_StartsWithOneSun() {
        var session = NewGame();
        session.GoToLevel(18);

        Assert.Equal(1, session.Board.StartingSuns);
    }

    [Fact]
    public void ALevel_ResumesAsTheSameGarden() {
        var session = NewGame();
        session.GoToLevel(12);
        SingleTap(session, WrongCells(session.Board.Puzzle).First());

        var resumed = GameSession.Resume(session.ToSave());

        Assert.Equal(12, resumed.Level);
        Assert.Equal(TestGardens.BedRows(session.Board.Puzzle), TestGardens.BedRows(resumed.Board.Puzzle));
        Assert.Equal(session.Board.CellsToText(), resumed.Board.CellsToText());
    }
    #endregion

    #region Extra gardens
    private static Puzzle ExtraGarden() => Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution());

    [Fact]
    public void AnExtraGarden_IsPlayed_WhileTheLevelWaits() {
        var session = NewGame();
        SingleTap(session, new Cell(0, 0));
        var saveBefore = session.ToSave();
        var extra = ExtraGarden();

        session.PlayExtraGarden(extra);

        Assert.True(session.IsExtraGarden);
        Assert.Same(extra, session.Board.Puzzle);
        Assert.Equal(1, session.Level);
        Assert.Equal(saveBefore, session.ToSave()); // Saving keeps the level's board, not the extra garden.
    }

    [Fact]
    public void SolvingAnExtraGarden_ReturnsToTheLevel_AsItWas() {
        var session = NewGame();
        SingleTap(session, new Cell(0, 0));
        var levelBoard = session.Board;

        session.PlayExtraGarden(ExtraGarden());
        Solve(session);

        Assert.True(session.TryAdvance());
        Assert.False(session.IsExtraGarden);
        Assert.Equal(1, session.Level);
        Assert.Same(levelBoard, session.Board);
    }

    [Fact]
    public void ASecondExtraGarden_StillReturnsToTheLevel() {
        var session = NewGame();
        var levelBoard = session.Board;

        session.PlayExtraGarden(ExtraGarden());
        session.PlayExtraGarden(ExtraGarden());
        Solve(session);
        session.TryAdvance();

        Assert.Same(levelBoard, session.Board);
    }

    [Fact]
    public void AnExtraGarden_CanBeTriedAgain_AfterRunningOutOfSun() {
        var session = NewGame();
        var extra = ExtraGarden();
        session.PlayExtraGarden(extra);

        Lose(session);

        Assert.True(session.TryRestart());
        Assert.True(session.IsExtraGarden);
        Assert.Same(extra, session.Board.Puzzle);
        Assert.Equal(0, session.Board.Mistakes);
    }

    [Fact]
    public void GoingToALevel_EndsTheExtraGarden() {
        var session = NewGame();
        session.PlayExtraGarden(ExtraGarden());

        Assert.True(session.GoToLevel(2));
        Assert.False(session.IsExtraGarden);
        Assert.Equal(2, session.Level);
    }
    #endregion

    #region Power-ups
    // A game whose level has only one Worm left, as if one had been used.
    private static GameSession GameWithOneWormLeft() =>
        GameSession.Resume(NewGame().ToSave() with { PowerUps = new PowerUpSupply(0, 1, 0) });

    [Fact]
    public void ANewGame_HasTheStartingPowerUps() {
        Assert.Equal(PowerUpSupply.Starting, NewGame().PowerUps);
    }

    [Fact]
    public void APowerUp_CanBeUsed_OnlyWithOneLeft_AndBeforeTheGardenEnds() {
        var session = GameWithOneWormLeft();

        Assert.True(session.CanUse(PowerUp.Worm));
        Assert.False(session.CanUse(PowerUp.Seedling));

        Solve(session);
        Assert.False(session.CanUse(PowerUp.Worm));
    }

    [Fact]
    public void PowerUps_AreSaved_AndResumed() {
        var session = GameWithOneWormLeft();

        Assert.Equal(1, session.PowerUps.Worm);
        Assert.Equal(1, GameSession.Resume(session.ToSave()).PowerUps.Worm);
    }

    [Fact]
    public void ANewGarden_RefillsThePowerUps() {
        var session = GameWithOneWormLeft();

        session.GoToLevel(2);

        Assert.Equal(PowerUpSupply.Starting, session.PowerUps);
    }

    [Fact]
    public void TryAgain_RefillsThePowerUps() {
        var session = GameWithOneWormLeft();
        Lose(session);

        session.TryRestart();

        Assert.Equal(PowerUpSupply.Starting, session.PowerUps);
    }

    [Fact]
    public void AnExtraGarden_HasItsOwnPowerUps_AndTheLevelsWait() {
        var session = GameWithOneWormLeft();

        session.PlayExtraGarden(ExtraGarden());
        Assert.Equal(PowerUpSupply.Starting, session.PowerUps);
        Assert.Equal(1, session.ToSave().PowerUps?.Worm); // Saving keeps the level's supply.

        Solve(session);
        session.TryAdvance();
        Assert.Equal(1, session.PowerUps.Worm);
    }

    #region Worm helper
    [Fact]
    public void UseWorm_GivesAHint_AndSpendsOneWorm() {
        var session = NewGame();

        Assert.NotNull(session.UseWorm());
        Assert.Equal(PowerUpSupply.Starting.Worm - 1, session.PowerUps.Worm);
    }

    [Fact]
    public void UseWorm_WithNoneLeft_GivesNothing() {
        var session = NewGame();
        session.UseWorm();
        session.ApplyHint();
        session.UseWorm();
        session.ApplyHint();

        Assert.Null(session.UseWorm());
        Assert.Equal(0, session.PowerUps.Worm);
    }

    [Fact]
    public void UseWorm_OnAFinishedGarden_GivesNothing_AndSpendsNothing() {
        var session = NewGame();
        Solve(session);

        Assert.Null(session.UseWorm());
        Assert.Equal(PowerUpSupply.Starting.Worm, session.PowerUps.Worm);
    }

    [Fact]
    public void UseWorm_LeavesTheBoardAsItWas() {
        var session = NewGame();
        var before = session.Board;

        session.UseWorm();

        Assert.Same(before, session.Board);
    }

    [Fact]
    public void UnlimitedWorms_NeverRunOut_OrSpend() {
        var session = NewGame();
        session.UnlimitedTools = true;

        for (int i = 0; i < 5; i++) {
            Assert.NotNull(session.UseWorm());
            session.ApplyHint();
        }

        Assert.Equal(PowerUpSupply.Starting.Worm, session.PowerUps.Worm);
        Assert.Equal(PowerUpSupply.Starting.Worm, session.ToSave().PowerUps?.Worm);
    }

    [Fact]
    public void WhileAHintIsFocused_TheBoardIgnoresTapsDragsAndKeys() {
        var session = NewGame();
        var cell = WrongCells(session.Board.Puzzle).First();
        Assert.NotNull(session.UseWorm());

        Assert.Equal(TapOutcome.Ignored, SingleTap(session, cell).Outcome);
        Assert.Equal(TapOutcome.Ignored, session.ToggleMark(cell).Outcome);
        Assert.Equal(TapOutcome.Ignored, session.Plant(cell).Outcome);
        session.BeginDrag();
        Assert.False(session.DragOver(cell));
        session.EndDrag();
        Assert.Equal(CellState.Empty, session.Board[cell]);
    }

    [Fact]
    public void ApplyHint_MarksTheRuleOuts_AndClosesTheHint() {
        var session = NewGame();
        session.PlayExtraGarden(ExtraGarden());
        var hint = session.UseWorm();
        Assert.NotNull(hint);
        Assert.NotEmpty(hint.RuleOuts);

        var result = session.ApplyHint();

        Assert.Equal(TapOutcome.MarkToggled, result.Outcome);
        Assert.All(hint.RuleOuts, cell => Assert.Equal(CellState.Marked, session.Board[cell]));
        Assert.Null(session.OpenHint);
        Assert.False(session.IsHintFocused);
        Assert.Equal(TapOutcome.Ignored, session.ApplyHint().Outcome); // Nothing left to apply.
    }

    [Fact]
    public void ApplyHint_GrowsAPlant_RightOverAWrongMark() {
        var session = NewGame();
        session.PlayExtraGarden(ExtraGarden());

        // The first four steps marked, plus a wrong ✕ on (2,4), where Sand's plant grows.
        Cell[] marks = [new(0, 2), new(0, 3), new(0, 4), new(2, 0), new(2, 1), new(2, 2), new(1, 0), new(1, 1), new(2, 3), new(2, 4)];
        foreach (var cell in marks) {
            session.ToggleMark(cell);
        }

        var hint = session.UseWorm();
        Assert.NotNull(hint);
        Assert.Equal([new Cell(2, 4)], hint.WrongMarks);

        var result = session.ApplyHint();

        Assert.Equal(TapOutcome.Grew, result.Outcome);
        Assert.Equal(CellState.Planted, session.Board[new Cell(2, 4)]);
        Assert.Equal(0, session.Board.Mistakes);
    }

    [Fact]
    public void AskingAgain_WithAHintOpen_GivesTheSameHint_ForFree() {
        var session = NewGame();
        var first = session.UseWorm();

        Assert.Same(first, session.UseWorm());
        Assert.Equal(PowerUpSupply.Starting.Worm - 1, session.PowerUps.Worm);
    }

    [Fact]
    public void ANewGarden_ClosesTheOpenHint() {
        var session = NewGame();
        session.UseWorm();

        session.GoToLevel(2);

        Assert.Null(session.OpenHint);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(40)]
    public void ApplyingEveryHint_GrowsTheWholeGarden_WithoutAMistake(int level) {
        var session = NewGame();
        session.GoToLevel(level);
        session.UnlimitedTools = true;
        TapResult? last = null;

        for (int hints = 0; !session.Board.IsSolved; hints++) {
            Assert.True(hints < 500, "The worm went round in circles.");
            Assert.NotNull(session.UseWorm());
            last = session.ApplyHint();
        }

        Assert.NotNull(last);
        Assert.True(last.EndedGarden);
        Assert.Equal(0, session.Board.Mistakes);
    }

    [Fact]
    public void AnUnfocusedHint_StaysOpen_WhileThePlayerPlays() {
        var session = NewGame();
        session.PlayExtraGarden(ExtraGarden());
        Assert.NotNull(session.UseWorm()); // Rule out A1's row: (0,2), (0,3) and (0,4).
        Assert.True(session.IsHintFocused);

        session.UnfocusHint();

        Assert.False(session.IsHintFocused);
        Assert.Equal(TapOutcome.MarkToggled, session.ToggleMark(new Cell(4, 0)).Outcome);
        Assert.NotNull(session.OpenHint);
    }

    [Fact]
    public void TheOpenHint_ShowsOnlyWhatsLeft_AndClosesOnceItsDone() {
        var session = NewGame();
        session.PlayExtraGarden(ExtraGarden());
        session.UseWorm();
        session.UnfocusHint();

        session.ToggleMark(new Cell(0, 2));

        Assert.NotNull(session.OpenHint);
        Assert.Equal([new Cell(0, 3), new Cell(0, 4)], session.OpenHint.RuleOuts);

        session.ToggleMark(new Cell(0, 3));
        session.ToggleMark(new Cell(0, 4));

        Assert.Null(session.OpenHint);
        Assert.False(session.IsHintFocused);
    }

    [Fact]
    public void ApplyHint_AfterSomeOfTheWork_OnlyDoesWhatsLeft() {
        var session = NewGame();
        session.PlayExtraGarden(ExtraGarden());
        session.UseWorm();
        session.UnfocusHint();
        session.ToggleMark(new Cell(0, 2));
        session.FocusHint();

        var result = session.ApplyHint();

        // (0,2) keeps the player's ✕ instead of being toggled back off.
        Cell[] ruleOuts = [new(0, 2), new(0, 3), new(0, 4)];
        Assert.Equal(TapOutcome.MarkToggled, result.Outcome);
        Assert.All(ruleOuts, cell => Assert.Equal(CellState.Marked, session.Board[cell]));
    }

    [Fact]
    public void FocusingTheHintAgain_IsFree() {
        var session = NewGame();
        var hint = session.UseWorm();
        session.UnfocusHint();

        Assert.Same(hint, session.UseWorm());
        Assert.True(session.IsHintFocused);
        Assert.Equal(PowerUpSupply.Starting.Worm - 1, session.PowerUps.Worm);

        session.UnfocusHint();
        session.FocusHint();
        Assert.True(session.IsHintFocused);
    }

    [Fact]
    public void CloseHint_PutsTheHintAway() {
        var session = NewGame();
        session.UseWorm();

        session.CloseHint();
        session.FocusHint(); // Nothing left to focus.

        Assert.Null(session.OpenHint);
        Assert.False(session.IsHintFocused);
    }

    [Fact]
    public void ARuleOutMarkedThenCleared_ComesBackToTheHint() {
        var session = NewGame();
        session.PlayExtraGarden(ExtraGarden());
        session.UseWorm(); // Rule out (0,2), (0,3) and (0,4).
        session.UnfocusHint();

        session.ToggleMark(new Cell(0, 2));
        session.ToggleMark(new Cell(0, 2));

        Assert.NotNull(session.OpenHint);
        Assert.Equal([new Cell(0, 2), new Cell(0, 3), new Cell(0, 4)], session.OpenHint.RuleOuts);

        session.ToggleMark(new Cell(0, 3));
        session.ToggleMark(new Cell(0, 4));
        Assert.NotNull(session.OpenHint); // (0,2) is still left, so the hint stays open.
    }

    [Fact]
    public void ClearingTheWrongMark_TakesTheMistakeOutOfTheNote() {
        var session = NewGame();
        session.PlayExtraGarden(ExtraGarden());

        // The first four steps marked, plus a wrong ✕ on (2,4) (E3), where Sand's plant grows.
        Cell[] marks = [new(0, 2), new(0, 3), new(0, 4), new(2, 0), new(2, 1), new(2, 2), new(1, 0), new(1, 1), new(2, 3), new(2, 4)];
        foreach (var cell in marks) {
            session.ToggleMark(cell);
        }

        var hint = session.UseWorm();
        Assert.NotNull(hint);
        Assert.EndsWith("Your ✕ there is a mistake.", hint.Message);

        session.UnfocusHint();
        session.ToggleMark(new Cell(2, 4)); // The player clears the wrong ✕.

        Assert.NotNull(session.OpenHint);
        Assert.Empty(session.OpenHint.WrongMarks);
        Assert.Equal($"{BedNames.For(2)} has only one cell left for a plant: E3.", session.OpenHint.Message);
        Assert.Equal([new Cell(2, 4)], session.OpenHint.Plants); // The plant is still left to grow.
    }

    [Fact]
    public void ApplyHint_EndsAHalfFinishedDoubleTap() {
        var session = NewGame();
        session.PlayExtraGarden(ExtraGarden());
        session.UseWorm(); // Rule out (0,2), (0,3) and (0,4).
        session.UnfocusHint();

        var cell = new Cell(4, 0);
        session.Tap(cell, 1000);  // First tap: a ✕.
        session.ApplyHint();      // Not possible from the app (Apply only shows while focused), but GameSession allows it.
        session.Tap(cell, 1100);  // Soon enough to be a double tap.

        // The second tap is a fresh tap, not a double tap rebuilt from the board before the hint.
        Cell[] ruleOuts = [new(0, 2), new(0, 3), new(0, 4)];
        Assert.All(ruleOuts, ruleOut => Assert.Equal(CellState.Marked, session.Board[ruleOut]));
    }
    #endregion

    #region Seedling
    [Fact]
    public void Seedlings_GrowACorrectPlant_WithoutSpendingASun() {
        var session = NewGame();
        session.PlayExtraGarden(ExtraGarden());

        var result = session.UseSeedling();

        Assert.Equal(TapOutcome.Grew, result.Outcome);
        Assert.Equal(CellState.Planted, session.Board[new Cell(0, 0)]);
        Assert.Equal(session.Board.StartingSuns, session.Board.SunsLeft);
        Assert.Equal(PowerUpSupply.Starting.Seedling - 1, session.PowerUps.Seedling);
    }

    [Fact]
    public void Seedlings_WithNoneLeft_DoNothing() {
        var session = GameWithOneWormLeft(); // No Seeds.

        Assert.Equal(TapOutcome.Ignored, session.UseSeedling().Outcome);
        Assert.Equal(0, session.Board.CountOf(CellState.Planted));
    }
    #endregion

    #endregion
}
