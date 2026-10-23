using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Play;

/// <summary>
/// The player's progress on one garden, kept separate from the puzzle's answer key.
/// Immutable: every change returns a new Board and leaves the old one untouched.
/// </summary>
public sealed class Board {

    /// <summary>
    /// Suns a garden starts with unless its level settings say otherwise.
    /// </summary>
    public const int DefaultSuns = 3;

    /// <summary>
    /// Suns the garden started with, from the level's settings.
    /// </summary>
    public int StartingSuns { get; }

    /// <summary>
    /// Suns not spent yet. Each wrong plant spends one. Never below 0.
    /// </summary>
    public int SunsLeft { get; }

    private readonly CellState[,] _cells;

    public Puzzle Puzzle { get; }

    public int Size => Puzzle.Size;

    public CellState this[Cell cell] => _cells[cell.Row, cell.Column];

    private Board(Puzzle puzzle, CellState[,] cells, int startingSuns, int sunsLeft) {
        Puzzle = puzzle;
        _cells = cells;
        StartingSuns = startingSuns;
        SunsLeft = sunsLeft;
    }

    /// <summary>
    /// True once every region has its plant.
    /// </summary>
    public bool IsSolved => CountOf(CellState.Planted) == Size;

    /// <summary>
    /// True once every sun is spent.
    /// </summary>
    public bool IsOutOfSun => SunsLeft == 0;

    /// <summary>
    /// True when the garden is solved or out of sun. A finished garden ignores all input.
    /// </summary>
    public bool IsFinished => IsSolved || IsOutOfSun;

    /// <summary>
    /// Wrong plants so far.
    /// </summary>
    public int Mistakes => CountOf(CellState.Wilted);

    /// <summary>
    /// The same garden from the start: every cell empty, every sun back.
    /// </summary>
    public Board Restart() => Start(Puzzle, StartingSuns);

    /// <summary>
    /// A fresh board for a puzzle: every cell empty, every sun unspent.
    /// </summary>
    public static Board Start(Puzzle puzzle, int suns = DefaultSuns) {
        ArgumentNullException.ThrowIfNull(puzzle);
        ArgumentOutOfRangeException.ThrowIfLessThan(suns, 1);
        return new Board(puzzle, new CellState[puzzle.Size, puzzle.Size], suns, suns);
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
    /// Locked cells don't change, and nothing changes once the garden is finished.
    /// </summary>
    public Board ToggleMark(Cell cell) {
        if (IsFinished) {
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
    /// Only empty cells can be planted: ✕ marks, locked cells and finished gardens are ignored.
    /// A correct cell grows; a wrong cell wilts and locks.
    /// </summary>
    public PlantResult TryPlant(Cell cell) {
        if (IsFinished || this[cell] != CellState.Empty) {
            return new PlantResult(this, PlantOutcome.Ignored);
        }

        return Puzzle.IsSolutionCell(cell)
            ? new PlantResult(With(cell, CellState.Planted), PlantOutcome.Grew)
            : new PlantResult(With(cell, CellState.Wilted, sunsLeft: Math.Max(0, SunsLeft - 1)), PlantOutcome.Wilted);
    }

    // A copy of this board with one cell changed, and optionally a new sun count.
    private Board With(Cell cell, CellState state, int? sunsLeft = null) {
        var cells = (CellState[,])_cells.Clone();
        cells[cell.Row, cell.Column] = state;
        return new Board(Puzzle, cells, StartingSuns, sunsLeft ?? SunsLeft);
    }
}
