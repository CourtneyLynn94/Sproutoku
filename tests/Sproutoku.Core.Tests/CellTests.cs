using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Tests.Puzzles;

public class CellTests {
    [Theory]
    [InlineData(1, 2, true)]  // shares an edge
    [InlineData(3, 3, true)]  // shares a corner
    [InlineData(2, 2, false)] // same cell
    [InlineData(4, 2, false)] // two rows away
    [InlineData(2, 0, false)] // two columns away
    public void Touches_ComparedToCellAtRow2Column2(int row, int column, bool expected) {
        var center = new Cell(2, 2);

        Assert.Equal(expected, center.Touches(new Cell(row, column)));
    }

    [Theory]
    [InlineData(2, 2, -1, 0, 1, 2)]  // up
    [InlineData(2, 2, 0, 1, 2, 3)]   // right
    [InlineData(0, 0, -1, 0, 0, 0)]  // up at the top edge stays put
    [InlineData(4, 4, 0, 1, 4, 4)]   // right at the right edge stays put
    [InlineData(4, 0, 1, -1, 4, 0)]  // bottom-left corner stays put
    public void MovedBy_StaysOnTheGrid(int row, int column, int rows, int columns, int expectedRow, int expectedColumn) {
        var moved = new Cell(row, column).MovedBy(rows, columns, size: 5);

        Assert.Equal(new Cell(expectedRow, expectedColumn), moved);
    }
}
