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
}
