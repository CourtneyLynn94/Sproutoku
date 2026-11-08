using Sproutoku.Core.Game.Boards;
using Sproutoku.Core.Game.Input;
using Sproutoku.Core.Game.Levels;
using Sproutoku.Core.Game.PowerUps;
using Sproutoku.Core.Game.Saving;
using Sproutoku.Core.Gardens.Puzzles;

namespace Sproutoku.Core.Game.Session;

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

    // The level's board, set aside while an extra garden is played. Null when playing the level itself.
    private Board? _levelBoard;

    // The level's power-ups, set aside with its board while an extra garden is played.
    private PowerUpSupply? _levelPowerUps;

    private GameSession(LevelGarden levelGarden, Board board, PowerUpSupply powerUps, DoubleTapDetector doubleTap) {
        LevelGarden = levelGarden;
        Board = board;
        PowerUps = powerUps;
        _doubleTap = doubleTap;
    }

    /// <summary>
    /// The level being played: its number, seed, recipe and garden. An extra garden doesn't change it.
    /// </summary>
    public LevelGarden LevelGarden { get; private set; }

    /// <summary>
    /// The level being played; 1 is the first garden.
    /// </summary>
    public int Level => LevelGarden.Level;

    /// <summary>
    /// The level's seed, shown in debug builds.
    /// </summary>
    public ulong Seed => LevelGarden.Seed;

    /// <summary>
    /// The current garden as the player sees it.
    /// </summary>
    public Board Board { get; private set; }

    /// <summary>
    /// The power-ups left for this garden. A new garden and a fresh start (Try again) get the starting supply.
    /// </summary>
    public PowerUpSupply PowerUps { get; private set; }

    // The open hint as the worm gave it. OpenHint is what's still left of it, worked out from this every time,
    // so a rule-out the player marks and then clears again comes back.
    private WormHint? _givenHint;

    /// <summary>
    /// The worm's hint, or null. It stays open while the player keeps playing and shows only what's still left to do.
    /// It closes when it's applied or closed, once the player has done all of it, or on a new garden.
    /// </summary>
    public WormHint? OpenHint { get; private set; }

    /// <summary>
    /// Whether the board is focused on the open hint. While it is, the board ignores taps, drags and keys.
    /// The player can Apply, or unfocus and keep playing.
    /// </summary>
    public bool IsHintFocused { get; private set; }

    /// <summary>
    /// For testing (a debug Journal option): Tools are never spent. Not saved.
    /// </summary>
    public bool UnlimitedTools { get; set; }

    /// <summary>
    /// True if a power-up can be used now: there's one left (or Tools are unlimited) and the garden isn't over.
    /// </summary>
    public bool CanUse(PowerUp powerUp) => !Board.IsFinished && (PowerUps.Has(powerUp) || UnlimitedTools);

    /// <summary>
    /// True while playing a garden outside the levels. Finishing it returns to the level.
    /// </summary>
    public bool IsExtraGarden => _levelBoard is not null;

    /// <summary>
    /// True when this garden may start over: only after running out of sun (Try again). There's always a next garden, so a solved one moves on.
    /// </summary>
    public bool CanRestart => Board.IsOutOfSun;

    /// <summary>
    /// Picks up from a save, or starts at Garden 1 when there's no usable save.
    /// </summary>
    public static GameSession Resume(GameSave? save, DoubleTapDetector? doubleTap = null) {
        var point = GameSave.Resume(save);
        return new GameSession(point.Garden, point.Board, point.PowerUps, doubleTap ?? new DoubleTapDetector());
    }

    /// <summary>
    /// What to store so this game can be resumed later. An extra garden is never saved; the level's board and power-ups are.
    /// </summary>
    public GameSave ToSave() => GameSave.From(Level, _levelBoard ?? Board, _levelPowerUps ?? PowerUps);

    /// <summary>
    /// Moves on once this garden is solved: back to the level after an extra garden, otherwise to the next level.
    /// A garden can never be advanced past twice. Returns false mid-garden.
    /// </summary>
    public bool TryAdvance() {
        if (!Board.IsSolved) {
            return false;
        }

        if (_levelBoard is { } levelBoard) {
            var levelPowerUps = _levelPowerUps ?? PowerUpSupply.Starting;
            _levelBoard = null;
            _levelPowerUps = null;
            SetGarden(LevelGarden, levelBoard, levelPowerUps);
            return true;
        }

        StartLevel(Level + 1);
        return true;
    }

    /// <summary>
    /// Moves on to the next level using a garden grown ahead of time.
    /// Same rule as <see cref="TryAdvance()"/>: only once this garden is solved, and only to the very next level.
    /// Returns false otherwise, including during an extra garden and for a garden that arrives late, after a reset or skip changed the level.
    /// </summary>
    public bool TryAdvance(LevelGarden next) {
        ArgumentNullException.ThrowIfNull(next);

        if (!Board.IsSolved || IsExtraGarden || next.Level != Level + 1) {
            return false;
        }

        GoToLevel(next);
        return true;
    }

    /// <summary>
    /// Starts this garden again after running out of sun. Returns false otherwise.
    /// </summary>
    public bool TryRestart() {
        if (!CanRestart) {
            return false;
        }

        SetGarden(LevelGarden, Board.Restart(), PowerUpSupply.Starting);
        return true;
    }

    /// <summary>
    /// Jumps to a level's garden, fresh: Garden 1 after a progress reset, or the dev skip. Returns false below level 1.
    /// </summary>
    public bool GoToLevel(int level) {
        if (level < 1) {
            return false;
        }

        GoToLevel(GardenLevels.ForLevel(level));
        return true;
    }

    /// <summary>
    /// Jumps to a level whose garden is already grown, for example off the main thread so the screen doesn't freeze.
    /// </summary>
    public void GoToLevel(LevelGarden levelGarden) {
        ArgumentNullException.ThrowIfNull(levelGarden);

        _levelBoard = null;
        _levelBoard = null;
        _levelPowerUps = null;
        SetGarden(levelGarden, levelGarden.Start(), PowerUpSupply.Starting);
    }

    /// <summary>
    /// Plays a garden outside the levels, setting the level's board aside until this one is solved.
    /// Playing another extra garden on top keeps the same level board waiting.
    /// </summary>
    public void PlayExtraGarden(Puzzle puzzle) {
        ArgumentNullException.ThrowIfNull(puzzle);

        if (_levelBoard is null) {
            _levelBoard = Board;
            _levelPowerUps = PowerUps;
        }

        SetGarden(LevelGarden, Board.Start(puzzle), PowerUpSupply.Starting);
    }

    // A level's garden, fresh.
    private void StartLevel(int level) => GoToLevel(GardenLevels.ForLevel(level));

    // A new garden forgets the last tap, so a tap on the old garden can't pair up with one on the new one.
    private void SetGarden(LevelGarden levelGarden, Board board, PowerUpSupply powerUps) {
        LevelGarden = levelGarden;
        Board = board;
        PowerUps = powerUps;
        CloseHint();
        _doubleTap.Reset();
        _boardBeforeTap = null;
        _dragMode = null;
    }

    #region Actions
    /// <summary>
    /// A tap on a cell at a time in milliseconds.
    /// A single tap toggles ✕.
    /// A second tap on the same cell soon after takes the first tap back and tries to plant on the cell as it was before.
    /// A finished garden ignores taps.
    /// </summary>
    public TapResult Tap(Cell cell, long timeMs) {
        if (Board.IsFinished || IsHintFocused) {
            return new TapResult(TapOutcome.Ignored, EndedGarden: false);
        }

        if (_doubleTap.RegisterTap(cell, timeMs) && _boardBeforeTap is not null) {
            var result = _boardBeforeTap.TryPlant(cell);
            Board = result.Board;
            _boardBeforeTap = null;
            UpdateHint();

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
        UpdateHint();

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
        if (_dragMode is null || Board.IsFinished || IsHintFocused) {
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
        UpdateHint();
        return true;
    }

    /// <summary>
    /// Ends the drag in progress, if any.
    /// </summary>
    public void EndDrag() {
        _dragMode = null;
    }

    /// <summary>
    /// Asks the worm for the next step, spends one Worm (unless tools are unlimited) and focuses the board on it.
    /// Returns null and spends nothing if there's no Worm left, the garden is over, or there's nothing left to work out.
    /// Asking again while a hint is open focuses it again for free.
    /// </summary>
    public WormHint? UseWorm() {
        if (OpenHint is not null) {
            FocusHint();
            return OpenHint;
        }
        if (!CanUse(PowerUp.Worm)) return null;

        var hint = WormHelper.HintFor(Board);
        if (hint is null) return null;

        if (!UnlimitedTools) {
            PowerUps = PowerUps.Spend(PowerUp.Worm);
        }

        _givenHint = hint;
        OpenHint = hint;
        FocusHint();
        return hint;
    }

    /// <summary>
    /// Makes what's left of the open hint: grows its plants (always correct, so no sun is spent) and marks its rule-outs with ✕.
    /// Closes the hint. Does nothing if no hint is open.
    /// </summary>
    public TapResult ApplyHint() {
        if (OpenHint is not { } hint) {
            return new TapResult(TapOutcome.Ignored, EndedGarden: false);
        }

        CloseHint();

        var board = Board;
        foreach (var cell in hint.Plants) {
            board = board.TryPlant(cell).Board; // Planting works on a ✕ too, so a wrong ✕ is replaced.
        }

        foreach (var cell in hint.RuleOuts) {
            board = board.ToggleMark(cell); // Only cells still empty: OpenHint keeps up with the board.
        }

        Board = board;
        var outcome = hint.Plants.Count > 0 ? TapOutcome.Grew : TapOutcome.MarkToggled;
        return new TapResult(outcome, Board.IsFinished);
    }

    /// <summary>
    /// Focuses the board on the open hint again. Does nothing if no hint is open.
    /// </summary>
    public void FocusHint() {
        if (OpenHint is null) return;

        ForgetLastTap();
        IsHintFocused = true;
    }

    /// <summary>
    /// Takes the focus off the hint so the player can keep playing. The hint stays open.
    /// </summary>
    public void UnfocusHint() => IsHintFocused = false;

    /// <summary>
    /// Puts the hint away without applying it.
    /// </summary>
    public void CloseHint() {
        _givenHint = null;
        OpenHint = null;
        IsHintFocused = false;
    }

    // After the player changes the board: the open hint is what's still left of the worm's hint, and closes once nothing is.
    private void UpdateHint() {
        OpenHint = _givenHint?.LeftToDo(Board);
        if (OpenHint is null) {
            CloseHint();
        }
    }
    #endregion

    #region Keyboard Actions
    /// <summary>
    /// Toggles a cell's ✕ straight away, with no double-tap timing (keyboard: Space or X).
    /// </summary>
    public TapResult ToggleMark(Cell cell) {
        ForgetLastTap();
        if (Board.IsFinished || IsHintFocused) {
            return new TapResult(TapOutcome.Ignored, EndedGarden: false);
        }

        var before = Board;
        Board = before.ToggleMark(cell);
        UpdateHint();

        var outcome = ReferenceEquals(Board, before) ? TapOutcome.Ignored : TapOutcome.MarkToggled;
        return new TapResult(outcome, EndedGarden: false);
    }

    /// <summary>
    /// Tries to plant straight away, with no double-tap timing (keyboard: Enter).
    /// Works on empty and ✕ cells, like a double tap.
    /// </summary>
    public TapResult Plant(Cell cell) {
        ForgetLastTap();
        if (Board.IsFinished || IsHintFocused) {
            return new TapResult(TapOutcome.Ignored, EndedGarden: false);
        }

        var result = Board.TryPlant(cell);
        Board = result.Board;
        UpdateHint();
        return new TapResult(ToTapOutcome(result.Outcome), Board.IsFinished);
    }

    private static TapOutcome ToTapOutcome(PlantOutcome outcome) => outcome switch {
        PlantOutcome.Grew => TapOutcome.Grew,
        PlantOutcome.Wilted => TapOutcome.Wilted,
        _ => TapOutcome.Ignored,
    };

    // Ends any half-finished double tap, so the next tap starts fresh.
    private void ForgetLastTap() {
        _doubleTap.Reset();
        _boardBeforeTap = null;
    }
    #endregion
}
