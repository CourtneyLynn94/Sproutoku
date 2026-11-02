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
        Assert.Equal([new Area(AreaKind.Bed, 5)], step.Areas);
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
        Assert.Equal([new Area(AreaKind.Row, 4)], step.Areas);
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
        Assert.Equal([new Area(AreaKind.Column, 0)], step.Areas);
        Assert.Equal([new Cell(0, 0)], step.Plants);
    }
    #endregion

    #region Groups that fill rows or columns
    [Fact]
    public void ABedSqueezedIntoOneRow_RulesOutOtherBedsInThatRow() {
        // In the test garden, bed 0 is just (0,0) and (0,1), both in row 0, so row 0's plant is bed 0's.
        var step = LogicSolver.NextStep(GardenFacts.Blank(TestGarden()));

        Assert.NotNull(step);
        Assert.Equal(Technique.BedsFillLines, step.Technique);
        Assert.Equal([new Area(AreaKind.Bed, 0), new Area(AreaKind.Row, 0)], step.Areas);
        Assert.Equal([new Cell(0, 2), new Cell(0, 3), new Cell(0, 4)], step.RuledOut);
    }

    [Fact]
    public void ARowWithOnlyOneBedLeft_RulesOutThatBedElsewhere() {
        // Row 0 is all bed 0, so bed 0's plant is in row 0, and its cell in row 1 is ruled out.
        var regions = TestGardens.Regions(
            "0000",
            "0112",
            "3122",
            "3332");

        var step = LogicSolver.NextStep(GardenFacts.Blank(regions));

        Assert.NotNull(step);
        Assert.Equal(Technique.LinesFillBeds, step.Technique);
        Assert.Equal([new Area(AreaKind.Row, 0), new Area(AreaKind.Bed, 0)], step.Areas);
        Assert.Equal([new Cell(1, 0)], step.RuledOut);
    }

    [Fact]
    public void TwoBedsSqueezedIntoTwoSeparateRows_RuleOutOtherBedsInThoseRows() {
        // Mid-solve, beds 0 and 2 can only grow in rows 2 and 4 (not next to each other), and each needs both rows.
        // So rows 2 and 4 hold beds 0 and 2's plants, and every other bed's cells in those rows are ruled out.
        var regions = TestGardens.Regions(
            "44411",
            "44411",
            "00021",
            "03321",
            "00322");
        var facts = GardenFacts.Blank(regions)
            .WithNoPlant(new Cell(0, 4))
            .WithNoPlant(new Cell(1, 1))
            .WithNoPlant(new Cell(3, 0))
            .WithNoPlant(new Cell(3, 3))
            .WithNoPlant(new Cell(4, 3));

        var step = LogicSolver.NextStep(facts);

        Assert.NotNull(step);
        Assert.Equal(Technique.BedsFillLines, step.Technique);
        Assert.Equal(
            [new Area(AreaKind.Bed, 0), new Area(AreaKind.Bed, 2), new Area(AreaKind.Row, 2), new Area(AreaKind.Row, 4)],
            step.Areas);
        Assert.Equal([new Cell(2, 4), new Cell(4, 2)], step.RuledOut);
    }
    #endregion

    #region Cells that would leave an area with nowhere to grow
    [Fact]
    public void CellsTouchingAllOfATwoCellBed_AreRuledOut() {
        // Your example: the test garden's bed 0 is just (0,0) and (0,1).
        // After bed 0 fills row 0, a plant at (1,0) or (1,1) would touch both of its cells, leaving it nowhere to grow.
        var result = LogicSolver.Solve(GardenFacts.Blank(TestGarden()));
        var step = result.Steps.First(step => step.Technique == Technique.WouldStarve);

        Assert.Equal([new Area(AreaKind.Bed, 0)], step.Areas);
        Assert.Equal([new Cell(1, 0), new Cell(1, 1)], step.RuledOut);
        Assert.Equal([new Cell(0, 0), new Cell(0, 1)], step.Reasons);
    }

    [Fact]
    public void ACellThatWouldEmptyARow_IsRuledOut() {
        // Every column is its own bed. Row 1 can only grow at (1,1) or (1,2);
        // the four cells above and below that touch both would leave row 1 nowhere to grow.
        var regions = TestGardens.Regions(
            "0123",
            "0123",
            "0123",
            "0123");
        var facts = GardenFacts.Blank(regions)
            .WithNoPlant(new Cell(1, 0))
            .WithNoPlant(new Cell(1, 3));

        var step = LogicSolver.NextStep(facts);

        Assert.NotNull(step);
        Assert.Equal(Technique.WouldStarve, step.Technique);
        Assert.Equal([new Area(AreaKind.Row, 1)], step.Areas);
        Assert.Equal([new Cell(0, 1), new Cell(0, 2), new Cell(2, 1), new Cell(2, 2)], step.RuledOut);
    }
    #endregion

    #region Solving
    [Fact]
    public void EveryStarterGarden_AndTheTestGarden_SolveByLogicAlone() {
        Puzzle[] gardens = [.. StarterGardens.All, TestGarden()];

        Assert.All(gardens, garden => {
            var result = LogicSolver.Solve(GardenFacts.Blank(garden));

            Assert.True(result.IsSolved);
            Assert.Equal(garden.Solution.ToHashSet(), result.Plants.ToHashSet());
        });
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
