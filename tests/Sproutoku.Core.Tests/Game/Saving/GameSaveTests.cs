using Sproutoku.Core.Game.Levels;
using Sproutoku.Core.Game.Saving;
using Sproutoku.Core.Gardens.Generation;

namespace Sproutoku.Core.Tests.Game.Saving;

public class GameSaveTests {
    private static readonly Puzzle Level1 = GardenLevels.ForLevel(1).Puzzle;
    private static readonly Puzzle Level2 = GardenLevels.ForLevel(2).Puzzle;

    // The first cell (reading row by row) that isn't part of the solution.
    private static Cell WrongCell(Puzzle puzzle) {
        for (int row = 0; row < puzzle.Size; row++) {
            for (int column = 0; column < puzzle.Size; column++) {
                var cell = new Cell(row, column);
                if (!puzzle.IsSolutionCell(cell)) {
                    return cell;
                }
            }
        }

        throw new InvalidOperationException("Every cell is a solution cell.");
    }

    // Cells for a garden one size bigger than level 2, so they can't fit it (as if an update had changed the garden).
    private static string CellsThatDontFitLevel2() => new('.', (Level2.Size + 1) * (Level2.Size + 1));

    [Fact]
    public void Json_RoundTrip_KeepsEverything() {
        var board = Board.Start(Level2).TryPlant(Level2.Solution[0]).Board.TryPlant(WrongCell(Level2)).Board;
        var save = GameSave.From(2, board);

        var loaded = GameSave.FromJson(save.ToJson());

        Assert.Equal(save, loaded);
        Assert.Equal(GardenLikeness.Fingerprint(Level2), loaded?.GardenFingerprint);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("""{"Version":99,"Level":1,"StartingSuns":3,"SunsLeft":3,"Cells":""}""")]
    public void FromJson_MissingBrokenOrOtherVersion_IsNull(string? json) {
        Assert.Null(GameSave.FromJson(json));
    }

    [Fact]
    public void FromJson_Version1Save_KeepsItsLevel_ButDropsItsBoard() {
        string json = $$"""{"Version":1,"Level":3,"StartingSuns":3,"SunsLeft":2,"Cells":"{{new string('x', 25)}}"}""";

        var save = GameSave.FromJson(json);

        Assert.NotNull(save);
        Assert.Equal(3, save.Level);
        Assert.Equal("", save.Cells);

        var (garden, board) = GameSave.Resume(save);
        Assert.Equal(3, garden.Level);
        Assert.Equal(board.Size * board.Size, board.CountOf(CellState.Empty));
        Assert.Equal(garden.Recipe.Suns, board.StartingSuns);
    }

    [Fact]
    public void Resume_WithNoSave_StartsAtLevel1() {
        var (garden, board) = GameSave.Resume(null);

        Assert.Equal(1, garden.Level);
        Assert.Equal(TestGardens.BedRows(Level1), TestGardens.BedRows(board.Puzzle));
        Assert.Equal(Level1.Size * Level1.Size, board.CountOf(CellState.Empty));
    }

    [Fact]
    public void Resume_MidGarden_PicksUpExactlyWhereItWasLeft() {
        var board = Board.Start(Level2).ToggleMark(WrongCell(Level2)).TryPlant(Level2.Solution[0]).Board;

        var (garden, resumed) = GameSave.Resume(GameSave.From(2, board));

        Assert.Equal(2, garden.Level);
        Assert.Equal(board.CellsToText(), resumed.CellsToText());
    }

    [Fact]
    public void Resume_OutOfSun_StartsTheSameGardenFresh() {
        var lost = Board.Start(Level2, suns: 1).TryPlant(WrongCell(Level2)).Board;

        var (garden, resumed) = GameSave.Resume(GameSave.From(2, lost));

        Assert.Equal(2, garden.Level);
        Assert.Equal(1, resumed.SunsLeft);
        Assert.Equal(0, resumed.Mistakes);
    }

    [Fact]
    public void Resume_Solved_MovesOnToTheNextLevel() {
        var board = Board.Start(Level1);
        foreach (var cell in Level1.Solution) {
            board = board.TryPlant(cell).Board;
        }

        var (garden, resumed) = GameSave.Resume(GameSave.From(1, board));

        Assert.Equal(2, garden.Level);
        Assert.Equal(TestGardens.BedRows(Level2), TestGardens.BedRows(resumed.Puzzle));
        Assert.False(resumed.IsSolved);
    }

    [Fact]
    public void Resume_LevelBelowOne_StartsAtLevel1() {
        var save = new GameSave(GameSave.CurrentVersion, 0, 3, 3, "");

        Assert.Equal(1, GameSave.Resume(save).Garden.Level);
    }

    [Fact]
    public void Resume_SaveThatDoesntFitItsGarden_KeepsTheLevel_AndStartsItFresh_WithItsOwnSuns() {
        var save = new GameSave(GameSave.CurrentVersion, 2, 1, 1, CellsThatDontFitLevel2());

        var (garden, board) = GameSave.Resume(save);

        Assert.Equal(2, garden.Level);
        Assert.Equal(TestGardens.BedRows(Level2), TestGardens.BedRows(board.Puzzle));
        Assert.Equal(Level2.Size * Level2.Size, board.CountOf(CellState.Empty));
        Assert.Equal(garden.Recipe.Suns, board.StartingSuns);
    }

    [Fact]
    public void Resume_SaveWithTooManySuns_StartsItsGardenFresh() {
        // A corrupted save claiming a million suns must not build a million sun slots.
        var save = new GameSave(GameSave.CurrentVersion, 2, 1_000_000, 1_000_000, new string('.', Level2.Size * Level2.Size));

        var (garden, board) = GameSave.Resume(save);

        Assert.Equal(2, garden.Level);
        Assert.Equal(garden.Recipe.Suns, board.StartingSuns);
    }

    [Fact]
    public void Resume_SaveFromAChangedGarden_StartsTheLevelFresh() {
        var board = Board.Start(Level2).ToggleMark(WrongCell(Level2));
        var save = GameSave.From(2, board) with { GardenFingerprint = "a garden from before an update" };

        var (garden, resumed) = GameSave.Resume(save);

        Assert.Equal(2, garden.Level);
        Assert.Equal(0, resumed.CountOf(CellState.Marked));
    }

    [Fact]
    public void FromJson_Version2Save_KeepsItsLevel_ButDropsItsBoard() {
        string json = $$"""{"Version":2,"Level":3,"StartingSuns":3,"SunsLeft":3,"Cells":"{{new string('x', 25)}}"}""";

        var save = GameSave.FromJson(json);

        Assert.NotNull(save);
        Assert.Equal(3, save.Level);
        Assert.Equal("", save.Cells);
        Assert.Null(save.GardenFingerprint);
    }

    [Fact]
    public void Resume_KeepsTheSavedPowerUps_ForARestoredBoard() {
        var save = GameSave.From(2, Board.Start(Level2), new PowerUpSupply(0, 1, 0));

        Assert.Equal(new PowerUpSupply(0, 1, 0), GameSave.Resume(save).PowerUps);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resume_GivesTheStartingPowerUps_ForAMissingOrBrokenSupply(bool broken) {
        var save = GameSave.From(2, Board.Start(Level2), broken ? new PowerUpSupply(0, -5, 0) : null);

        Assert.Equal(PowerUpSupply.Starting, GameSave.Resume(save).PowerUps);
    }

    [Fact]
    public void Resume_AFreshGarden_GetsTheStartingPowerUps() {
        // The board doesn't fit, so the level starts fresh, and a fresh garden has a full supply.
        var save = new GameSave(GameSave.CurrentVersion, 2, 3, 3, CellsThatDontFitLevel2(), PowerUps: new PowerUpSupply(0, 0, 0));

        Assert.Equal(PowerUpSupply.Starting, GameSave.Resume(save).PowerUps);
    }

    [Fact]
    public void Json_KeepsThePowerUps() {
        var save = GameSave.From(2, Board.Start(Level2), new PowerUpSupply(0, 1, 0));

        Assert.Equal(save, GameSave.FromJson(save.ToJson()));
    }
}
