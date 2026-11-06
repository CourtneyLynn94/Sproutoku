using Sproutoku.Core.Game.Levels;
using Sproutoku.Core.Gardens.Solving;

namespace Sproutoku.Core.Tests.Game.PowerUps;

public class WormHelperTests {
    private static readonly Puzzle Garden = Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution());

    private static Board MarkedAt(params Cell[] cells) =>
        cells.Aggregate(Board.Start(Garden), (board, cell) => board.ToggleMark(cell));

    [Fact]
    public void OnAFreshGarden_TheWormPointsToTheFirstStep() {
        var hint = WormHelper.HintFor(Board.Start(Garden));

        // Bed 0 only has cells in row 1, so the rest of row 1 can't hold a plant.
        Assert.NotNull(hint);
        Assert.Equal(WormHintKind.NextStep, hint.Kind);
        Assert.Equal(Technique.BedsFillLines, hint.Step.Technique);
        Assert.Equal([new Cell(0, 0), new Cell(0, 1), new Cell(0, 2), new Cell(0, 3), new Cell(0, 4)], hint.Area);
        Assert.Equal([new Cell(0, 2), new Cell(0, 3), new Cell(0, 4)], hint.Targets);
        Assert.Contains("row 1", hint.LookHere);
        Assert.Contains("row 1", hint.Explanation);
    }

    [Fact]
    public void StepsAlreadyOnTheBoard_AreSkipped() {
        var hint = WormHelper.HintFor(MarkedAt(new(0, 2), new(0, 3), new(0, 4)));

        // The next step: bed 2 only has cells in row 3.
        Assert.NotNull(hint);
        Assert.Equal([new Cell(2, 0), new Cell(2, 1), new Cell(2, 2)], hint.Targets);
        Assert.Contains("row 3", hint.LookHere);
    }

    [Fact]
    public void OnlyCellsNotYetMarked_AreRinged() {
        var hint = WormHelper.HintFor(MarkedAt(new Cell(0, 2)));

        Assert.NotNull(hint);
        Assert.Equal([new Cell(0, 3), new Cell(0, 4)], hint.Targets);
    }

    [Fact]
    public void AWrongMark_IsPointedOutFirst() {
        // (0,0) is where bed 0's plant grows, and there's a newer step waiting too.
        var hint = WormHelper.HintFor(MarkedAt(new Cell(0, 0)));

        Assert.NotNull(hint);
        Assert.Equal(WormHintKind.WrongMark, hint.Kind);
        Assert.Equal([new Cell(0, 0)], hint.Targets);
        Assert.Contains("row 1, column 1", hint.LookHere);
    }

    [Fact]
    public void APlant_RulesOutItsRowColumnBedAndNeighbors() {
        var board = Board.Start(Garden).TryPlant(new Cell(0, 0)).Board;

        var hint = WormHelper.HintFor(board);

        Assert.NotNull(hint);
        Assert.Equal(Technique.PlantRulesOut, hint.Step.Technique);
        Assert.Equal([new Cell(0, 0)], hint.Area);
        Assert.Equal(
            [new(0, 1), new(0, 2), new(0, 3), new(0, 4), new(1, 0), new(1, 1), new(2, 0), new(3, 0), new(4, 0)],
            hint.Targets);
    }

    [Fact]
    public void AFinishedGarden_GetsNoHint() {
        var board = Garden.Solution.Aggregate(Board.Start(Garden), (current, cell) => current.TryPlant(cell).Board);

        Assert.Null(WormHelper.HintFor(board));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(40)]
    public void FollowingEveryHint_GrowsTheWholeGarden(int level) {
        // A player who only ever does what the worm says always finishes, and never runs out of hints.
        var puzzle = GardenLevels.ForLevel(level).Puzzle;
        var board = Board.Start(puzzle);

        for (int hints = 0; !board.IsSolved; hints++) {
            Assert.True(hints < 500, "The worm went round in circles.");

            var hint = WormHelper.HintFor(board);
            Assert.NotNull(hint);

            foreach (var cell in hint.Targets) {
                board = hint.Step.Plants.Contains(cell) && hint.Kind == WormHintKind.NextStep
                    ? board.TryPlant(cell).Board
                    : board.ToggleMark(cell);
            }
        }

        Assert.Equal(0, board.Mistakes);
    }
}
