using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Play;

/// <summary>
/// The player's progress on one garden, kept separate from the puzzle's answer key.
/// Immutable: every change returns a new Board and leaves the old one untouched.
/// </summary>
public sealed class Board {
    private readonly CellState[,] _cells;

    private Board(Puzzle puzzle, CellState[,] cells) {
        Puzzle = puzzle;
        _cells = cells;
    }

    public Puzzle Puzzle { get; }

    public int Size => Puzzle.Size;

    public CellState this[Cell cell] => _cells[cell.Row, cell.Column];

    /// <summary>
    /// True once every region has its plant.
    /// </summary>
    public bool IsSolved => CountOf(CellState.Planted) == Size;

    /// <summary>
    /// A fresh board for a puzzle: every cell empty.
    /// </summary>
    public static Board Start(Puzzle puzzle) {
        ArgumentNullException.ThrowIfNull(puzzle);
        return new Board(puzzle, new CellState[puzzle.Size, puzzle.Size]);
    }

    public int CountOf(CellState state) {
        int count = 0;
        foreach (var cellState in _cells) {
            if (cellState == state) {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Empty becomes ✕ and ✕ becomes empty.
    /// Locked cells don't change, and nothing changes once the garden is solved.
    /// </summary>
    public Board ToggleMark(Cell cell) {
        if (IsSolved) {
            return this;
        }

        return this[cell] switch {
            CellState.Empty => With(cell, CellState.Marked),
            CellState.Marked => With(cell, CellState.Empty),
            _ => this,
        };
    }

    /// <summary>
    /// Tries to grow a plant.
    /// Only empty cells can be planted: ✕ marks, locked cells and solved gardens are ignored.
    /// A correct cell grows; a wrong cell wilts and locks.
    /// </summary>
    public PlantResult TryPlant(Cell cell) {
        if (IsSolved || this[cell] != CellState.Empty) {
            return new PlantResult(this, PlantOutcome.Ignored);
        }

        return Puzzle.IsSolutionCell(cell)
            ? new PlantResult(With(cell, CellState.Planted), PlantOutcome.Grew)
            : new PlantResult(With(cell, CellState.Wilted), PlantOutcome.Wilted);
    }

    // A copy of this board with one cell changed.
    private Board With(Cell cell, CellState state) {
        var cells = (CellState[,])_cells.Clone();
        cells[cell.Row, cell.Column] = state;
        return new Board(Puzzle, cells);
    }
}
