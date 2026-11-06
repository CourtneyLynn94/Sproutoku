using Sproutoku.Core.Game.Levels;
using Sproutoku.Core.Gardens.Solving;

namespace Sproutoku.Core.Tests.Game.PowerUps;

public class WormHelperTests {
    // Rows 00111 / 11111 / 33122 / 33344 / 33444; its logic steps were worked out with a reference copy of the solver.
    private static readonly Puzzle Garden = Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution());

    private static Board MarkedAt(params Cell[] cells) =>
        cells.Aggregate(Board.Start(Garden), (board, cell) => board.ToggleMark(cell));

    [Fact]
    public void OnAFreshGarden_TheWormGivesTheFirstStep() {
        var hint = WormHelper.HintFor(Board.Start(Garden));

        // Bed 0 only has cells in row 1, so the rest of row 1 can't hold a plant.
        Assert.NotNull(hint);
        Assert.Equal(Technique.BedsFillLines, hint.Step.Technique);
        Assert.Equal([new Cell(0, 0), new Cell(0, 1), new Cell(0, 2), new Cell(0, 3), new Cell(0, 4)], hint.Area);
        Assert.Equal([new Cell(0, 2), new Cell(0, 3), new Cell(0, 4)], hint.Targets);
        Assert.Empty(hint.WrongMarks);
        Assert.StartsWith("Look at this bed and row 1.", hint.Message);
    }

    [Fact]
    public void StepsAlreadyOnTheBoard_AreSkipped() {
        var hint = WormHelper.HintFor(MarkedAt(new(0, 2), new(0, 3), new(0, 4)));

        // The next step: bed 2 only has cells in row 3.
        Assert.NotNull(hint);
        Assert.Equal([new Cell(2, 0), new Cell(2, 1), new Cell(2, 2)], hint.Targets);
        Assert.Contains("row 3", hint.Message);
    }

    [Fact]
    public void OnlyCellsNotYetMarked_AreRinged() {
        var hint = WormHelper.HintFor(MarkedAt(new Cell(0, 2)));

        Assert.NotNull(hint);
        Assert.Equal([new Cell(0, 3), new Cell(0, 4)], hint.Targets);
    }

    [Fact]
    public void AWrongMark_TheStepDoesntTouch_IsLeftAlone() {
        // (0,0) holds bed 0's plant, but the first step only rules out the rest of row 1.
        var hint = WormHelper.HintFor(MarkedAt(new Cell(0, 0)));

        Assert.NotNull(hint);
        Assert.Equal([new Cell(0, 2), new Cell(0, 3), new Cell(0, 4)], hint.Targets);
        Assert.Empty(hint.WrongMarks);
    }

    [Fact]
    public void AWrongMark_TheStepProvesWrong_IsCalledOutInTheHint() {
        // The first four steps are all marked, and there's a ✕ on (2,4): the fifth step proves bed 2's plant grows there.
        var board = MarkedAt(
            new(0, 2), new(0, 3), new(0, 4),
            new(2, 0), new(2, 1), new(2, 2),
            new(1, 0), new(1, 1),
            new(2, 3),
            new(2, 4));

        var hint = WormHelper.HintFor(board);

        Assert.NotNull(hint);
        Assert.Equal(Technique.OnlyCellLeft, hint.Step.Technique);
        Assert.Equal([new Cell(2, 4)], hint.Targets);
        Assert.Equal([new Cell(2, 4)], hint.WrongMarks);
        Assert.Contains("Your ✕ there is a mistake", hint.Message);
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
        // A player who only ever does what the worm says always finishes, never runs out of hints, and never makes a mistake.
        var puzzle = GardenLevels.ForLevel(level).Puzzle;
        var board = Board.Start(puzzle);

        for (int hints = 0; !board.IsSolved; hints++) {
            Assert.True(hints < 500, "The worm went round in circles.");

            var hint = WormHelper.HintFor(board);
            Assert.NotNull(hint);

            foreach (var cell in hint.Targets) {
                board = hint.Step.Plants.Contains(cell) ? board.TryPlant(cell).Board : board.ToggleMark(cell);
            }
        }

        Assert.Equal(0, board.Mistakes);
    }

    [Fact]
    public void AWouldStarveHint_SaysWhichRulesDoIt() {
        // With the first two steps marked, the next one: a plant at (1,0) or (1,1) would starve bed 0, whose open cells are (0,0) and (0,1).
        // Each is in the same column as one of them and touches the other diagonally; none shares their bed.
        var hint = WormHelper.HintFor(MarkedAt(new(0, 2), new(0, 3), new(0, 4), new(2, 0), new(2, 1), new(2, 2)));

        Assert.NotNull(hint);
        Assert.Equal(Technique.WouldStarve, hint.Step.Technique);
        Assert.Equal([new Cell(1, 0), new Cell(1, 1)], hint.Targets);
        Assert.Contains("because each of them is in that cell's row or column or touches it", hint.Message);
        Assert.Contains("This bed would have nowhere left to grow", hint.Message);
    }
}
