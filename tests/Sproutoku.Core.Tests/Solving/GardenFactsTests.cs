using Sproutoku.Core.Play;
using Sproutoku.Core.Puzzles;
using Sproutoku.Core.Solving;
using Sproutoku.Core.Tests.Puzzles;

namespace Sproutoku.Core.Tests.Solving;

public class GardenFactsTests {
    [Fact]
    public void From_KeepsPlantsAndLockedCells_AndTreatsMarksAsUnknown() {
        var puzzle = Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution());
        var board = Board.Start(puzzle)
            .TryPlant(new Cell(1, 2)).Board   // planted (a solution cell)
            .TryPlant(new Cell(0, 1)).Board   // wilted
            .ToggleMark(new Cell(0, 0));      // a wrong ✕: (0,0) is a solution cell

        var facts = GardenFacts.From(board);

        Assert.Equal(Known.Plant, facts[new Cell(1, 2)]);
        Assert.Equal(Known.NoPlant, facts[new Cell(0, 1)]);
        Assert.Equal(Known.Unknown, facts[new Cell(0, 0)]);
        Assert.Equal(Known.Unknown, facts[new Cell(4, 4)]);
    }

    [Fact]
    public void Blank_KnowsOnlyTheBeds() {
        var puzzle = Puzzle.Create(TestGardens.ValidRegions(), TestGardens.ValidSolution());

        var facts = GardenFacts.Blank(puzzle);

        Assert.Equal(5, facts.Size);
        Assert.Equal(1, facts.RegionAt(new Cell(2, 2)));
        Assert.Equal(Known.Unknown, facts[puzzle.Solution[0]]);
    }
}
