using Sproutoku.Core.Gardens.Solving;

namespace Sproutoku.Core.Tests.Game.PowerUps;

public class WormToolTests {
    // Rows 00111 / 11111 / 33122 / 33344 / 33444; its logic steps were worked out with a reference copy of the solver.
    private static readonly Puzzle Garden = Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution());

    private static Board MarkedAt(params Cell[] cells) =>
        cells.Aggregate(Board.Start(Garden), (board, cell) => board.ToggleMark(cell));

    [Fact]
    public void OnAFreshGarden_TheWormGivesTheFirstStep() {
        var hint = WormTool.HintFor(Board.Start(Garden));

        // Bed 0 (Soil) only has cells in row 1, so the rest of row 1 can't hold a plant.
        Assert.NotNull(hint);
        Assert.Equal($"{BedNames.For(0)} only has open cells in row 1. Rule out the other beds there.", hint.Message);
        Assert.Equal([0], hint.NamedRows);
        Assert.Empty(hint.NamedColumns);
        Assert.Equal([new Cell(0, 2), new Cell(0, 3), new Cell(0, 4)], hint.RuleOuts);
        Assert.Empty(hint.Plants);
        Assert.Equal([new Cell(0, 0), new Cell(0, 1), new Cell(0, 2), new Cell(0, 3), new Cell(0, 4)], hint.Focus);
    }

    [Fact]
    public void StepsAlreadyOnTheBoard_AreSkipped() {
        var hint = WormTool.HintFor(MarkedAt(new(0, 2), new(0, 3), new(0, 4)));

        Assert.NotNull(hint);
        Assert.Equal($"{BedNames.For(2)} only has open cells in row 3. Rule out the other beds there.", hint.Message);
        Assert.Equal([new Cell(2, 0), new Cell(2, 1), new Cell(2, 2)], hint.RuleOuts);
    }

    [Fact]
    public void OnlyCellsNotYetMarked_GetPreviewMarks() {
        var hint = WormTool.HintFor(MarkedAt(new Cell(0, 2)));

        Assert.NotNull(hint);
        Assert.Equal([new Cell(0, 3), new Cell(0, 4)], hint.RuleOuts);
    }

    [Fact]
    public void AWouldStarveHint_NamesTheRulesThatApply() {
        var hint = WormTool.HintFor(MarkedAt(new(0, 2), new(0, 3), new(0, 4), new(2, 0), new(2, 1), new(2, 2)));

        // A plant at (1,0) or (1,1) would block Soil's open cells (0,0) and (0,1): same column, or touching diagonally.
        Assert.NotNull(hint);
        Assert.Equal(Technique.WouldStarve, hint.Step.Technique);
        Assert.Equal($"A plant on A2 or B2 would block all of {BedNames.For(0)}'s open cells (same row or column, or touching). Rule them out.", hint.Message);
        Assert.Equal([1], hint.NamedRows);
        Assert.Equal([0, 1], hint.NamedColumns);
        Assert.Equal([new Cell(1, 0), new Cell(1, 1)], hint.RuleOuts);
        Assert.Equal([new Cell(0, 0), new Cell(0, 1), new Cell(1, 0), new Cell(1, 1)], hint.Focus);
    }

    [Fact]
    public void ACorrectMark_CountsAsAFact() {
        // (0,1) is Soil but not its plant, so the ✕ is right: Soil now has only (0,0) left.
        // Without trusting the ✕, the worm would have said "Soil only has open cells in row 1" instead.
        var hint = WormTool.HintFor(MarkedAt(new Cell(0, 1)));

        Assert.NotNull(hint);
        Assert.Equal($"{BedNames.For(0)} has only one cell left for a plant: A1.", hint.Message);
        Assert.Equal([new Cell(0, 0)], hint.Plants);
        Assert.Contains(new Cell(0, 1), hint.Focus); // The whole bed is lit, including the ✕.
    }

    [Fact]
    public void AWrongMark_TheStepDoesntTouch_IsLeftAlone() {
        var hint = WormTool.HintFor(MarkedAt(new Cell(0, 0)));

        Assert.NotNull(hint);
        Assert.Equal([new Cell(0, 2), new Cell(0, 3), new Cell(0, 4)], hint.RuleOuts);
        Assert.Empty(hint.WrongMarks);
    }

    [Fact]
    public void AWrongMark_TheStepProvesWrong_IsCalledOut() {
        var board = MarkedAt(
            new(0, 2), new(0, 3), new(0, 4),
            new(2, 0), new(2, 1), new(2, 2),
            new(1, 0), new(1, 1),
            new(2, 3),
            new(2, 4));

        var hint = WormTool.HintFor(board);

        Assert.NotNull(hint);
        Assert.Equal($"{BedNames.For(2)} has only one cell left for a plant: E3. Your ✕ there is a mistake.", hint.Message);
        Assert.Equal([2], hint.NamedRows);
        Assert.Equal([4], hint.NamedColumns);
        Assert.Equal([new Cell(2, 4)], hint.Plants);
        Assert.Equal([new Cell(2, 4)], hint.WrongMarks);
    }

    [Fact]
    public void APlant_RulesOutItsRowColumnBedAndNeighbors() {
        var hint = WormTool.HintFor(Board.Start(Garden).TryPlant(new Cell(0, 0)).Board);

        Assert.NotNull(hint);
        Assert.Equal("No other plant can share a row, column or bed with the plant on A1, or touch it. Rule those cells out.", hint.Message);
        Assert.Equal([0], hint.NamedRows);
        Assert.Equal([0], hint.NamedColumns);
        Assert.Equal(
            [new(0, 1), new(0, 2), new(0, 3), new(0, 4), new(1, 0), new(1, 1), new(2, 0), new(3, 0), new(4, 0)],
            hint.RuleOuts);
        Assert.Equal(new Cell(0, 0), hint.Focus[0]);
    }

    [Fact]
    public void AFinishedGarden_GetsNoHint() {
        var board = Garden.Solution.Aggregate(Board.Start(Garden), (current, cell) => current.TryPlant(cell).Board);

        Assert.Null(WormTool.HintFor(board));
    }
}
