using Sproutoku.Core.Play;
using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Solving;

/// <summary>
/// What the solver knows about one cell.
/// </summary>
public enum Known {
    Unknown,
    Plant,
    NoPlant,
}

/// <summary>
/// Everything the logic solver may use: the bed shapes and the facts on the board.
/// It never holds the answer, so the solver (and the Worm's hints) can't peek.
/// Player ✕ marks aren't facts: they might be wrong, so they count as Unknown.
/// </summary>
public sealed class GardenFacts {
    private readonly int[,] _regions;
    private readonly Known[,] _known;

    private GardenFacts(int[,] regions, Known[,] known) {
        _regions = regions;
        _known = known;
    }

    public int Size => _regions.GetLength(0);

    public int RegionAt(Cell cell) => _regions[cell.Row, cell.Column];

    public Known this[Cell cell] => _known[cell.Row, cell.Column];

    /// <summary>
    /// The facts on a board: planted cells hold plants; wilted and revealed cells don't;
    /// everything else, including the player's ✕ marks, is unknown.
    /// </summary>
    public static GardenFacts From(Board board) {
        ArgumentNullException.ThrowIfNull(board);

        int size = board.Size;
        var regions = new int[size, size];
        var known = new Known[size, size];

        for (int row = 0; row < size; row++) {
            for (int column = 0; column < size; column++) {
                var cell = new Cell(row, column);
                regions[row, column] = board.Puzzle.RegionAt(cell);
                known[row, column] = board[cell] switch {
                    CellState.Planted => Known.Plant,
                    CellState.Wilted or CellState.RevealedEmpty => Known.NoPlant,
                    _ => Known.Unknown,
                };
            }
        }

        return new GardenFacts(regions, known);
    }

    /// <summary>
    /// Only the bed shapes, nothing known yet: how the generator checks a garden from scratch.
    /// </summary>
    public static GardenFacts Blank(Puzzle puzzle) {
        ArgumentNullException.ThrowIfNull(puzzle);

        var regions = new int[puzzle.Size, puzzle.Size];
        for (int row = 0; row < puzzle.Size; row++) {
            for (int column = 0; column < puzzle.Size; column++) {
                regions[row, column] = puzzle.RegionAt(new Cell(row, column));
            }
        }

        return new GardenFacts(regions, new Known[puzzle.Size, puzzle.Size]);
    }

    /// <summary>
    /// Only the bed shapes from a grid of bed ids (regions[row][column], ids 0 to N−1), nothing known yet.
    /// For bed layouts that aren't puzzles yet, like the generator's.
    /// </summary>
    public static GardenFacts Blank(int[][] regions) {
        ArgumentNullException.ThrowIfNull(regions);

        int size = regions.Length;
        if (size < 1 || size > PuzzleValidator.MaxSize) {
            throw new ArgumentOutOfRangeException(nameof(regions), size, $"A garden is 1 to {PuzzleValidator.MaxSize} rows.");
        }

        var grid = new int[size, size];
        for (int row = 0; row < size; row++) {
            if (regions[row].Length != size) {
                throw new ArgumentException("The bed grid must be square.", nameof(regions));
            }

            for (int column = 0; column < size; column++) {
                grid[row, column] = regions[row][column];
            }
        }

        return new GardenFacts(grid, new Known[size, size]);
    }

    /// <summary>
    /// These facts plus one more plant.
    /// </summary>
    public GardenFacts WithPlant(Cell cell) => With(cell, Known.Plant);

    /// <summary>
    /// These facts plus one more cell known to hold no plant.
    /// </summary>
    public GardenFacts WithNoPlant(Cell cell) => With(cell, Known.NoPlant);

    private GardenFacts With(Cell cell, Known value) {
        var known = (Known[,])_known.Clone();
        known[cell.Row, cell.Column] = value;
        return new GardenFacts(_regions, known);
    }
}
