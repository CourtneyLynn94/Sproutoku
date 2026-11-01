using Sproutoku.Core.Play;
using Sproutoku.Core.Puzzles;
using Sproutoku.Core.Saving;

namespace Sproutoku.Core.Tests.Saving;

public class GameSaveTests {
    private static readonly Puzzle Level1 = GardenLevels.ForLevel(1)!;
    private static readonly Puzzle Level2 = GardenLevels.ForLevel(2)!;

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

    [Fact]
    public void Json_RoundTrip_KeepsEverything() {
        var board = Board.Start(Level2).TryPlant(Level2.Solution[0]).Board.TryPlant(WrongCell(Level2)).Board;
        var save = GameSave.From(2, board);

        var loaded = GameSave.FromJson(save.ToJson());

        Assert.Equal(save, loaded);
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
    public void Resume_WithNoSave_StartsAtLevel1() {
        var (level, board) = GameSave.Resume(null);

        Assert.Equal(1, level);
        Assert.Same(Level1, board.Puzzle);
        Assert.Equal(Level1.Size * Level1.Size, board.CountOf(CellState.Empty));
    }

    [Fact]
    public void Resume_MidGarden_PicksUpExactlyWhereItWasLeft() {
        var board = Board.Start(Level2).ToggleMark(WrongCell(Level2)).TryPlant(Level2.Solution[0]).Board;

        var (level, resumed) = GameSave.Resume(GameSave.From(2, board));

        Assert.Equal(2, level);
        Assert.Equal(board.CellsToText(), resumed.CellsToText());
    }

    [Fact]
    public void Resume_OutOfSun_StartsTheSameGardenFresh() {
        var lost = Board.Start(Level2, suns: 1).TryPlant(WrongCell(Level2)).Board;

        var (level, resumed) = GameSave.Resume(GameSave.From(2, lost));

        Assert.Equal(2, level);
        Assert.Equal(1, resumed.SunsLeft);
        Assert.Equal(0, resumed.Mistakes);
    }

    [Fact]
    public void Resume_Solved_MovesOnToTheNextGarden() {
        var board = Board.Start(Level1);
        foreach (var cell in Level1.Solution) {
            board = board.TryPlant(cell).Board;
        }

        var (level, resumed) = GameSave.Resume(GameSave.From(1, board));

        Assert.Equal(2, level);
        Assert.Same(Level2, resumed.Puzzle);
    }

    [Fact]
    public void Resume_LevelThatDoesntExist_StartsAtLevel1() {
        var save = new GameSave(GameSave.CurrentVersion, 99, 3, 3, "");

        Assert.Equal(1, GameSave.Resume(save).Level);
    }

    [Fact]
    public void Resume_SaveThatDoesntFitItsGarden_KeepsTheLevel_AndStartsItFresh() {
        // Level 2 is 6×6, so a 5×5 board's cells don't fit it (as if an update had changed the garden).
        var save = new GameSave(GameSave.CurrentVersion, 2, 3, 3, new string('.', 25));

        var (level, board) = GameSave.Resume(save);

        Assert.Equal(2, level);
        Assert.Same(Level2, board.Puzzle);
        Assert.Equal(Level2.Size * Level2.Size, board.CountOf(CellState.Empty));
    }

    [Fact]
    public void Resume_LastGardenSolved_StartsItAgain() {
        int lastLevel = StarterGardens.All.Count;
        var puzzle = GardenLevels.ForLevel(lastLevel)!;
        var board = Board.Start(puzzle);
        foreach (var cell in puzzle.Solution) {
            board = board.TryPlant(cell).Board;
        }

        var (level, resumed) = GameSave.Resume(GameSave.From(lastLevel, board));

        Assert.Equal(lastLevel, level);
        Assert.False(resumed.IsSolved);
        Assert.Equal(puzzle.Size * puzzle.Size, resumed.CountOf(CellState.Empty));
    }
}
