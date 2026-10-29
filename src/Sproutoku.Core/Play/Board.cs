using Sproutoku.Core.Puzzles;
using System.Text;

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
    /// Tries to grow a plant on an empty or ✕ cell; the plant replaces the ✕.
    /// Locked cells and finished gardens are ignored.
    /// A correct cell grows; a wrong cell wilts, locks and spends a sun.
    /// </summary>
    public PlantResult TryPlant(Cell cell) {
        if (IsFinished || this[cell] is not (CellState.Empty or CellState.Marked)) {
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

    #region Board to text

    // One letter per CellState, in the enum's order: Empty, Marked, Planted, Wilted, RevealedEmpty.
    private const string StateLetters = ".xPWr";

    /// <summary>
    /// The cells as text for saving: one letter per cell, row by row (. empty, x marked, P planted, W wilted, r revealed empty).
    /// </summary>
    public string CellsToText() {
        var text = new StringBuilder(Size * Size);
        foreach (var state in _cells) {
            text.Append(StateLetters[(int)state]);
        }

        return text.ToString();
    }

    /// <summary>
    /// Rebuilds a saved board, or returns null if the save doesn't fit this puzzle:
    /// the wrong number of cells, an unknown letter, a plant off the solution, a wilt on the solution, or impossible suns.
    /// </summary>
    public static Board? TryRestore(Puzzle puzzle, string cellText, int startingSuns, int sunsLeft) {
        ArgumentNullException.ThrowIfNull(puzzle);

        if (cellText is null || cellText.Length != puzzle.Size * puzzle.Size) {
            return null;
        }

        if (startingSuns < 1 || sunsLeft < 0 || sunsLeft > startingSuns) {
            return null;
        }

        var cells = new CellState[puzzle.Size, puzzle.Size];
        for (int i = 0; i < cellText.Length; i++) {
            int letter = StateLetters.IndexOf(cellText[i]);
            if (letter < 0) {
                return null;
            }

            var cell = new Cell(i / puzzle.Size, i % puzzle.Size);
            var state = (CellState)letter;
            bool isSolution = puzzle.IsSolutionCell(cell);

            if ((state == CellState.Planted && !isSolution)
                || (state is CellState.Wilted or CellState.RevealedEmpty && isSolution)) {
                return null;
            }

            cells[cell.Row, cell.Column] = state;
        }

        return new Board(puzzle, cells, startingSuns, sunsLeft);
    }
    #endregion
}
