using Sproutoku.Core.Gardens.Puzzles;

namespace Sproutoku.Core.Game.Input;

/// <summary>
/// Which line a drag is held to, if any.
/// </summary>
public enum DragLock {
    /// <summary>
    /// The drag follows the finger anywhere.
    /// </summary>
    None,

    /// <summary>
    /// The drag stays in the row it started in.
    /// </summary>
    Row,

    /// <summary>
    /// The drag stays in the column it started in.
    /// </summary>
    Column,
}

/// <summary>
/// Turns one press's finger path into the cells a drag passes.
/// Positions are in cell units, like <see cref="DragGridLine"/>: x is the column and y is the row.
/// A press becomes a drag once it moves a third of a cell, and the start cell comes first so it's marked at once.
/// A drag that starts clearly along a row or column stays in that line, so a wobbly finger can't slip into the next one;
/// it lets go once the finger is past the middle of the neighboring line. A diagonal start follows the finger freely.
/// A press that leaves the board without crossing another cell stays a press, like a finger sliding off the garden.
/// </summary>
public sealed class DragTracker {
    /// <summary>
    /// How far a press moves, in cells, before it becomes a drag. Less than this is still a tap.
    /// </summary>
    public const double StartDistance = 1.0 / 3;

    // A start this many times longer along one direction than the other locks the drag to its row or column.
    private const double LockRatio = 2;

    // How far from its own line's middle the finger goes before a lock lets go: the middle of the neighboring line.
    private const double UnlockDistance = 1;

    private readonly double _startX;
    private readonly double _startY;
    private double _lastX;
    private double _lastY;

    private readonly int _boardSize;

    /// <summary>
    /// A press at this position, on a board of this size.
    /// </summary>
    public DragTracker(double x, double y, int boardSize) {
        _startX = x;
        _startY = y;
        _lastX = x;
        _lastY = y;
        _boardSize = boardSize;
    }

    /// <summary>
    /// Whether the press has moved far enough to be a drag.
    /// </summary>
    public bool IsDragging { get; private set; }

    /// <summary>
    /// The line the drag is held to, chosen when it starts.
    /// </summary>
    public DragLock Lock { get; private set; }

    /// <summary>
    /// The finger moved here. Returns the cells to visit, in order: none while it's still a press;
    /// once it's a drag, every cell the path crosses, starting with the start cell on the first one.
    /// Cells off the board are included; callers filter them.
    /// </summary>
    public IReadOnlyList<Cell> MoveTo(double x, double y) {
        if (!IsDragging) {
            double dx = x - _startX;
            double dy = y - _startY;
            if (Math.Sqrt((dx * dx) + (dy * dy)) < StartDistance) return [];

            // Off the board without crossing another cell: still a press. It may come back, or lift off as nothing.
            if (!IsOnBoard(x, y) && !CrossesAnotherCell(x, y)) return [];

            IsDragging = true;
            Lock = Math.Abs(dx) >= LockRatio * Math.Abs(dy) ? DragLock.Row
                : Math.Abs(dy) >= LockRatio * Math.Abs(dx) ? DragLock.Column
                : DragLock.None;
        }

        double rowMiddle = Math.Floor(_startY) + 0.5;
        double columnMiddle = Math.Floor(_startX) + 0.5;

        if (Lock == DragLock.Row && Math.Abs(y - rowMiddle) > UnlockDistance) {
            Lock = DragLock.None;
        } else if (Lock == DragLock.Column && Math.Abs(x - columnMiddle) > UnlockDistance) {
            Lock = DragLock.None;
        }

        // Held to its line: the wobble across the line is ignored.
        double toX = Lock == DragLock.Column ? columnMiddle : x;
        double toY = Lock == DragLock.Row ? rowMiddle : y;

        var cells = DragGridLine.CellsAlong(_lastX, _lastY, toX, toY);
        _lastX = toX;
        _lastY = toY;
        return cells;
    }

    private bool IsOnBoard(double x, double y) => x >= 0 && x < _boardSize && y >= 0 && y < _boardSize;

    // Whether the straight path from the press to here passes through a board cell other than the one it started on.
    private bool CrossesAnotherCell(double x, double y) {
        var start = new Cell((int)Math.Floor(_startY), (int)Math.Floor(_startX));
        return DragGridLine.CellsAlong(_startX, _startY, x, y)
            .Any(cell => cell != start && cell.IsInside(_boardSize));
    }
}
