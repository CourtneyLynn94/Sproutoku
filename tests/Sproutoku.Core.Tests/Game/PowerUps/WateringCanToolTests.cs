using Sproutoku.Core.Game.Levels;

namespace Sproutoku.Core.Tests.Game.PowerUps;

public class WateringCanToolTests {
    // TestGardens' 5×5. The first thing logic proves: bed 0 fits only in row 1, so (0,2), (0,3) and (0,4) are empty.
    private static Board NewBoard() =>
        Board.Start(Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution()));

    [Fact]
    public void OnAFreshGarden_ItRevealsTheFirstCellsLogicRulesOut() {
        Assert.Equal([new Cell(0, 2), new Cell(0, 3), new Cell(0, 4)], WateringCanTool.CellsFor(NewBoard()));
    }

    [Fact]
    public void ACellThePlayerAlreadyMarked_IsSkipped() {
        var cells = WateringCanTool.CellsFor(NewBoard().ToggleMark(new Cell(0, 2)));

        Assert.DoesNotContain(new Cell(0, 2), cells);
        Assert.Equal(new Cell(0, 3), cells[0]);
        Assert.Equal(WateringCanTool.MaxCells, cells.Count);
    }

    [Fact]
    public void WateringUntilNothingIsLeft_NeverRevealsAPlant() {
        for (int level = 1; level <= 20; level++) {
            var board = GardenLevels.ForLevel(level).Start();

            while (WateringCanTool.CellsFor(board) is { Count: > 0 } cells) {
                foreach (var cell in cells) {
                    Assert.False(board.Puzzle.IsSolutionCell(cell), $"Level {level} would reveal its plant at {cell}.");
                    board = board.RevealEmpty(cell);
                }
            }

            Assert.All(board.Puzzle.Solution, plant => Assert.Equal(CellState.Empty, board[plant]));
        }
    }
}
