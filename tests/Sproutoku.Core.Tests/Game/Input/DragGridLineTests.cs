using Sproutoku.Core.Input;
using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Tests.Input;

public class DragGridLineTests {
    [Fact]
    public void ReviewExample_IncludesTheBarelyCrossedCell() {
        // From CodeRabbit's review of PR #94: quarter-cell sampling skipped (1, 1).
        var cells = DragGridLine.CellsAlong(0.3702369584, 3.3436398757, 3.8126092381, 0.1544213297);

        Cell[] expected = [new(3, 0), new(2, 0), new(2, 1), new(1, 1), new(1, 2), new(0, 2), new(0, 3)];
        Assert.Equal(expected, cells.ToArray());
    }

    [Fact]
    public void AlongARow_ReturnsEveryCellInOrder() {
        var cells = DragGridLine.CellsAlong(0.5, 0.5, 4.5, 0.5);

        Cell[] expected = [new(0, 0), new(0, 1), new(0, 2), new(0, 3), new(0, 4)];
        Assert.Equal(expected, cells.ToArray());
    }

    [Fact]
    public void Backwards_ReturnsCellsInTheOrderCrossed() {
        var cells = DragGridLine.CellsAlong(2.5, 0.5, 0.5, 0.5);

        Cell[] expected = [new(0, 2), new(0, 1), new(0, 0)];
        Assert.Equal(expected, cells.ToArray());
    }

    [Fact]
    public void ThroughExactCorners_SkipsCellsOnlyTouchedAtAPoint() {
        var cells = DragGridLine.CellsAlong(0.5, 0.5, 2.5, 2.5);

        Cell[] expected = [new(0, 0), new(1, 1), new(2, 2)];
        Assert.Equal(expected, cells.ToArray());
    }

    [Fact]
    public void EndingExactlyOnAnEdge_IncludesTheCellPastIt() {
        var cells = DragGridLine.CellsAlong(0.5, 0.5, 2.0, 0.5);

        Cell[] expected = [new(0, 0), new(0, 1), new(0, 2)];
        Assert.Equal(expected, cells.ToArray());
    }

    [Fact]
    public void WithinOneCell_ReturnsJustThatCell() {
        var cells = DragGridLine.CellsAlong(1.5, 1.5, 1.7, 1.2);

        Assert.Single(cells);
        Assert.Equal(new Cell(1, 1), cells[0]);
    }
}
