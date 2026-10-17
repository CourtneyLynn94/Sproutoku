namespace Sproutoku.Core.Puzzles;

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

    public override string ToString() => $"(row {Row}, column {Column})";
}
