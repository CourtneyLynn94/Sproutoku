using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Input;

/// <summary>
/// Finds the cells a straight line passes through, for drags (S27).
/// Positions are in cell units: x is the column and y is the row, so (2.5, 0.5) is the middle of cell (0, 2).
/// </summary>
public static class DragGridLine {
    /// <summary>
    /// Every cell the segment passes through with a non-zero length, in order from start to end.
    /// A line through an exact corner steps diagonally, skipping the two cells it only touches at that point.
    /// The end point's own cell is always included, even when it lies exactly on a cell edge.
    /// Cells off the board are included too; callers filter them.
    /// </summary>
    public static IReadOnlyList<Cell> CellsAlong(double x0, double y0, double x1, double y1) {
        int column = (int)Math.Floor(x0);
        int row = (int)Math.Floor(y0);
        int endColumn = (int)Math.Floor(x1);
        int endRow = (int)Math.Floor(y1);

        double dx = x1 - x0;
        double dy = y1 - y0;
        int stepColumn = Math.Sign(dx);
        int stepRow = Math.Sign(dy);

        // How far along the segment (0 = start, 1 = end) the next column and row lines are crossed, and how far it is from one line to the next.
        double nextX = stepColumn > 0 ? (column + 1 - x0) / dx
            : stepColumn < 0 ? (column - x0) / dx
            : double.PositiveInfinity;
        double nextY = stepRow > 0 ? (row + 1 - y0) / dy
            : stepRow < 0 ? (row - y0) / dy
            : double.PositiveInfinity;
        double gapX = stepColumn != 0 ? stepColumn / dx : double.PositiveInfinity;
        double gapY = stepRow != 0 ? stepRow / dy : double.PositiveInfinity;

        var cells = new List<Cell> { new(row, column) };
        int stepsLeft = Math.Abs(endColumn - column) + Math.Abs(endRow - row) + 2;

        while ((row != endRow || column != endColumn) && stepsLeft-- > 0) {
            if (Math.Min(nextX, nextY) > 1) {
                break;
            }

            if (nextX == nextY) {
                // Exactly through a corner.
                column += stepColumn;
                row += stepRow;
                nextX += gapX;
                nextY += gapY;
            } else if (nextX < nextY) {
                column += stepColumn;
                nextX += gapX;
            } else {
                row += stepRow;
                nextY += gapY;
            }

            cells.Add(new Cell(row, column));
        }

        var endCell = new Cell(endRow, endColumn);
        if (cells[^1] != endCell) {
            cells.Add(endCell);
        }

        return cells;
    }
}
