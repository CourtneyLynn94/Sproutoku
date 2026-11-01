using Sproutoku.Core.Play;
using Sproutoku.Core.Puzzles;
using Sproutoku.Core.Solving;
using Sproutoku.Core.Tests.Puzzles;

namespace Sproutoku.Core.Tests.Solving;

public class LogicSolverTests {
    private static Puzzle TestGarden() => Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution());

    private static IEnumerable<Cell> AllCells(int size) =>
        from row in Enumerable.Range(0, size)
        from column in Enumerable.Range(0, size)
        select new Cell(row, column);

    #region A plant rules cells out
    [Fact]
    public void APlantedCell_RulesOutItsRowColumnBedAndTouchingCells() {
        var puzzle = TestGarden();
        var plant = new Cell(1, 2);
        var facts = GardenFacts.Blank(puzzle).WithPlant(plant);

        var step = LogicSolver.NextStep(facts);

        Assert.NotNull(step);
        Assert.Equal(Technique.PlantRulesOut, step.Technique);
        Assert.Equal([plant], step.Reasons);

        var expected = AllCells(5).Where(cell => cell != plant
            && (cell.Row == 1 || cell.Column == 2
                || puzzle.RegionAt(cell) == puzzle.RegionAt(plant)
                || cell.Touches(plant)));
        Assert.Equal(expected.ToHashSet(), step.RuledOut.ToHashSet());
    }

    [Fact]
    public void WithNoPlants_NothingIsRuledOutAroundAPlant() {
        // Rule-outs only start from plants on the board (or ones the solver has proved).
        var step = LogicSolver.NextStep(GardenFacts.Blank(StarterGardens.All[1]));

        Assert.NotNull(step);
        Assert.NotEqual(Technique.PlantRulesOut, step.Technique);
    }
    #endregion

    #region Only one cell left
    [Fact]
    public void ASingleCellBed_MustHoldItsPlant() {
        // Starter garden 2's bed f is a single cell in the bottom-left corner.
        var step = LogicSolver.NextStep(GardenFacts.Blank(StarterGardens.All[1]));

        Assert.NotNull(step);
        Assert.Equal(Technique.OnlyCellLeft, step.Technique);
        Assert.Equal(new Area(AreaKind.Bed, 5), step.Area);
        Assert.Equal([new Cell(5, 0)], step.Plants);
    }

    [Fact]
    public void ARowWithOneCellLeft_MustHoldItsPlant() {
        var facts = GardenFacts.Blank(TestGarden())
            .WithNoPlant(new Cell(4, 0))
            .WithNoPlant(new Cell(4, 1))
            .WithNoPlant(new Cell(4, 2))
            .WithNoPlant(new Cell(4, 4));

        var step = LogicSolver.NextStep(facts);

        Assert.NotNull(step);
        Assert.Equal(Technique.OnlyCellLeft, step.Technique);
        Assert.Equal(new Area(AreaKind.Row, 4), step.Area);
        Assert.Equal([new Cell(4, 3)], step.Plants);
        Assert.Equal(4, step.Reasons.Count);
    }

    [Fact]
    public void AColumnWithOneCellLeft_MustHoldItsPlant() {
        var facts = GardenFacts.Blank(TestGarden())
            .WithNoPlant(new Cell(1, 0))
            .WithNoPlant(new Cell(2, 0))
            .WithNoPlant(new Cell(3, 0))
            .WithNoPlant(new Cell(4, 0));

        var step = LogicSolver.NextStep(facts);

        Assert.NotNull(step);
        Assert.Equal(new Area(AreaKind.Column, 0), step.Area);
        Assert.Equal([new Cell(0, 0)], step.Plants);
    }
    #endregion

    #region Solving
    [Fact]
    public void StarterGarden2_SolvesFromScratch_AndMatchesItsAnswer() {
        var garden = StarterGardens.All[1];

        var result = LogicSolver.Solve(GardenFacts.Blank(garden));

        Assert.True(result.IsSolved);
        Assert.Equal(garden.Solution.ToHashSet(), result.Plants.ToHashSet());
    }

    [Fact]
    public void AWrongMark_DoesntMisleadTheSolver() {
        // The player marks ✕ on a real plant cell. The solver ignores ✕ marks, so it still finds the right answer.
        var garden = StarterGardens.All[1];
        var board = Board.Start(garden).ToggleMark(garden.Solution[0]);

        var result = LogicSolver.Solve(GardenFacts.From(board));

        Assert.True(result.IsSolved);
        Assert.Contains(garden.Solution[0], result.Plants);
    }

    [Fact]
    public void EveryStep_ChangesSomething() {
        var result = LogicSolver.Solve(GardenFacts.Blank(StarterGardens.All[1]));

        Assert.All(result.Steps, step => Assert.True(step.Plants.Count + step.RuledOut.Count > 0));
    }
    #endregion
}
