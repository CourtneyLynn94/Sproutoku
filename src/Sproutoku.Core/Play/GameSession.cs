using Sproutoku.Core.Input;
using Sproutoku.Core.Puzzles;
using Sproutoku.Core.Saving;

namespace Sproutoku.Core.Play;

/// <summary>
/// One player's game: the level being played, its board, and the rules for moving between gardens.
/// The page passes input in and shows what comes back. Every rule lives here, so the whole game flow can be unit tested without a phone or a window.
/// </summary>
public sealed class GameSession {
    private readonly DoubleTapDetector _doubleTap;

    // The board from just before the last single tap, so a double tap can take that tap back.
    private Board? _boardBeforeTap;

    // The drag in progress. Null when there is none; Undecided until the drag reaches an empty or ✕ cell.
    private enum DragMode { Undecided, Mark, Clear }
    private DragMode? _dragMode;

    private GameSession(int level, Board board, DoubleTapDetector doubleTap) {
        Level = level;
        Board = board;
        _doubleTap = doubleTap;
    }

    /// <summary>
    /// The level being played; 1 is the first garden.
    /// </summary>
    public int Level { get; private set; }

    /// <summary>
    /// The current garden as the player sees it.
    /// </summary>
    public Board Board { get; private set; }

    /// <summary>
    /// True if there is a garden after this one.
    /// </summary>
    public bool HasNextLevel => GardenLevels.ForLevel(Level + 1) is not null;

    /// <summary>
    /// True when this garden may start over:
    /// after running out of sun (Try again), or after solving the last garden there is (Play it again). Never mid-garden.
    /// </summary>
    public bool CanRestart => Board.IsOutOfSun || (Board.IsSolved && !HasNextLevel);

    /// <summary>
    /// Picks up from a save, or starts at Garden 1 when there's no usable save.
    /// </summary>
    public static GameSession Resume(GameSave? save, DoubleTapDetector? doubleTap = null) {
        var (level, board) = GameSave.Resume(save);
        return new GameSession(level, board, doubleTap ?? new DoubleTapDetector());
    }

    /// <summary>
    /// What to store so this game can be resumed later.
    /// </summary>
    public GameSave ToSave() => GameSave.From(Level, Board);

    /// <summary>
    /// A tap on a cell at a time in milliseconds.
    /// A single tap toggles ✕.
    /// A second tap on the same cell soon after takes the first tap back and tries to plant on the cell as it was before.
    /// A finished garden ignores taps.
    /// </summary>
    public TapResult Tap(Cell cell, long timeMs) {
        if (Board.IsFinished) {
            return new TapResult(TapOutcome.Ignored, EndedGarden: false);
        }

        if (_doubleTap.RegisterTap(cell, timeMs) && _boardBeforeTap is not null) {
            var result = _boardBeforeTap.TryPlant(cell);
            Board = result.Board;
            _boardBeforeTap = null;

            var outcome = result.Outcome switch {
                PlantOutcome.Grew => TapOutcome.Grew,
                PlantOutcome.Wilted => TapOutcome.Wilted,
                _ => TapOutcome.Ignored,
            };
            return new TapResult(outcome, Board.IsFinished);
        }

        var before = Board;
        _boardBeforeTap = before;
        Board = before.ToggleMark(cell);

        var markOutcome = ReferenceEquals(Board, before) ? TapOutcome.Ignored : TapOutcome.MarkToggled;
        return new TapResult(markOutcome, EndedGarden: false);
    }

    /// <summary>
    /// Starts a drag.
    /// The first empty or ✕ cell the drag passes decides whether it marks or clears.
    /// Also ends any half-finished double tap.
    /// </summary>
    public void BeginDrag() {
        _dragMode = DragMode.Undecided;
        _doubleTap.Reset();
        _boardBeforeTap = null;
    }

    /// <summary>
    /// The drag passing over a cell.
    /// Marking only fills empty cells and clearing only clears ✕ cells;
    /// planted, wilted and revealed cells are never touched, and a drag never plants.
    /// Returns true if the cell changed.
    /// </summary>
    public bool DragOver(Cell cell) {
        if (_dragMode is null || Board.IsFinished) {
            return false;
        }

        var state = Board[cell];

        if (_dragMode == DragMode.Undecided) {
            if (state == CellState.Empty) {
                _dragMode = DragMode.Mark;
            } else if (state == CellState.Marked) {
                _dragMode = DragMode.Clear;
            } else {
                return false; // A locked cell: keep looking for the cell that decides.
            }
        }

        bool changes = _dragMode == DragMode.Mark
            ? state == CellState.Empty
            : state == CellState.Marked;
        if (!changes) {
            return false;
        }

        Board = Board.ToggleMark(cell);
        return true;
    }

    /// <summary>
    /// Ends the drag in progress, if any.
    /// </summary>
    public void EndDrag() {
        _dragMode = null;
    }

    /// <summary>
    /// Moves on to the next garden.
    /// Only works once this one is solved and another exists, so a garden can never be advanced past twice.
    /// Returns false when it can't.
    /// </summary>
    public bool TryAdvance() {
        if (!Board.IsSolved || GardenLevels.ForLevel(Level + 1) is not { } next) {
            return false;
        }

        SetGarden(Level + 1, Board.Start(next));
        return true;
    }

    /// <summary>
    /// Starts this garden again (Try again, Play it again).
    /// Returns false mid-garden.
    /// </summary>
    public bool TryRestart() {
        if (!CanRestart) {
            return false;
        }

        SetGarden(Level, Board.Restart());
        return true;
    }

    /// <summary>
    /// Jumps to a level's garden, fresh: Garden 1 after a progress reset, or the dev skip.
    /// Returns false when there's no garden for that level.
    /// </summary>
    public bool GoToLevel(int level) {
        if (GardenLevels.ForLevel(level) is not { } puzzle) {
            return false;
        }

        SetGarden(level, Board.Start(puzzle));
        return true;
    }

    // A new garden forgets the last tap, so a tap on the old garden can't pair up with one on the new one.
    private void SetGarden(int level, Board board) {
        Level = level;
        Board = board;
        _doubleTap.Reset();
        _boardBeforeTap = null;
        _dragMode = null;
    }
}
