namespace Sproutoku.Core.Gardens.Puzzles;

/// <summary>
/// A position in the grid. Row 0 is the top row and column 0 the left column.
/// </summary>
public readonly record struct Cell(int Row, int Column) {

    /// <summary>
    /// True if the cell lies on an N×N grid of the given size.
    /// </summary>
    public bool IsInside(int size) => Row >= 0 && Row < size && Column >= 0 && Column < size;

    /// <summary>
    /// True if the two cells are different and share an edge or a corner.
    /// </summary>
    public bool Touches(Cell other) => this != other
                                       && Math.Abs(Row - other.Row) <= 1
                                       && Math.Abs(Column - other.Column) <= 1;

    /// <summary>
    /// The four cells that share an edge with this one. Some may lie outside the grid.
    /// </summary>
    public IReadOnlyList<Cell> EdgeNeighbors() => [
        new(Row - 1, Column),
        new(Row + 1, Column),
        new(Row, Column - 1),
        new(Row, Column + 1),
    ];

    /// <summary>
    /// The cell a number of rows and columns away, kept on an N×N grid of the given size, so arrow keys stop at the garden's edge.
    /// </summary>
    public Cell MovedBy(int rows, int columns, int size) =>
        new(Math.Clamp(Row + rows, 0, size - 1), Math.Clamp(Column + columns, 0, size - 1));

    /// <summary>
    /// The cell's name on the board's labels, column letter then row number: (row 4, column 1) is "B5".
    /// </summary>
    public string Label => $"{ColumnLabel(Column)}{RowLabel(Row)}";

    /// <summary>
    /// Columns are lettered A, B, C… from the left.
    /// </summary>
    public static string ColumnLabel(int column) => ((char)('A' + column)).ToString();

    /// <summary>
    /// Rows are numbered 1, 2, 3… from the top. Players count from 1.
    /// </summary>
    public static string RowLabel(int row) => (row + 1).ToString();

    public override string ToString() => $"(row {Row}, column {Column})";
}
