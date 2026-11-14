using Sproutoku.App.Pages;
using Sproutoku.Core.Game.Input;
using Cell = Sproutoku.Core.Gardens.Puzzles.Cell;

namespace Sproutoku.App.Controls.Garden;

/// <summary>
/// The garden board: draws the board and the worm's hint, and turns touches, drags, hover and keys into cells for <see cref="GardenViewModel"/>.
/// It finds the view model through its <c>BindingContext</c> (the page's).
/// </summary>
public sealed partial class GardenView : ContentView {
#if WINDOWS || MACCATALYST
    // Keyboard support. Filled in by Platforms/Windows/GardenView.Keyboard.cs; on other platforms these do nothing.
    partial void HookUpKeyboard();
    partial void FocusGardenForKeyboard();
#endif

    private readonly GraphicsView _canvas = new();
    private readonly GardenDrawable _garden = new() { ShouldDrawRegionBorders = false };
    private GardenViewModel? _viewModel;

    // The press in progress: the cell it started on (null if it started off the board), and its drag tracker.
    private Cell? _pressedCell;
    private DragTracker? _drag;

    public GardenView() {
        _canvas.Drawable = _garden;
        Content = _canvas;

        _canvas.StartInteraction += OnPress;
        _canvas.DragInteraction += OnDrag;
        _canvas.EndInteraction += OnRelease;
        _canvas.CancelInteraction += (_, _) => FinishPress();

#if WINDOWS || MACCATALYST
        // Desktop hover. Touch screens don't send hover events, so this does nothing on a phone.
        _canvas.MoveHoverInteraction += (_, e) => SetHover(_garden.CellAt(e.Touches[0]));
        _canvas.EndHoverInteraction += (_, _) => SetHover(null);

        // Desktop keyboard.
        HookUpKeyboard();
#endif
    }

    // Listens to the view model it's given, and stops listening to the old one.
    protected override void OnBindingContextChanged() {
        base.OnBindingContextChanged();

        if (_viewModel is not null) {
            _viewModel.GardenStarted -= OnGardenStarted;
            _viewModel.BoardChanged -= OnBoardChanged;
        }

        _viewModel = BindingContext as GardenViewModel;

        if (_viewModel is not null) {
            _viewModel.GardenStarted += OnGardenStarted;
            _viewModel.BoardChanged += OnBoardChanged;
            Draw();
        }
    }

    // A new garden: forget the last one's hover and focus, and any press still in progress,
    // so lifting a finger that went down on the old garden does nothing to the new one.
    private void OnGardenStarted(object? sender, EventArgs e) {
        _garden.HoverCell = null;
        _garden.FocusCell = null;
        _garden.ShowFocus = false;
        _pressedCell = null;
        _drag = null;
        Draw();
    }

    private void OnBoardChanged(object? sender, EventArgs e) => Draw();

    #region Drawing
    // Shows the view model's board and hint.
    // A focused hint dims the board and shows its preview marks; its labels stay while the note is up, focused or not.
    private void Draw() {
        if (_viewModel is null) return;

        var hint = _viewModel.OpenHint;
        var shown = _viewModel.IsHintFocused ? hint : null;

        _garden.Board = _viewModel.Board;
        _garden.ShowSolution = _viewModel.ShowSolution;
        _garden.HintFocus = shown?.Focus;
        _garden.HintPlants = shown?.Plants;
        _garden.HintRuleOuts = shown?.RuleOuts;
        _garden.HintMistakes = shown?.WrongMarks;
        _garden.HintRows = hint?.NamedRows;
        _garden.HintColumns = hint?.NamedColumns;

        // A finished garden takes no more input, so the hover highlight goes.
        if (_viewModel.Board?.IsFinished == true) {
            _garden.HoverCell = null;
        }

        _canvas.Invalidate();
    }
    #endregion

    #region Touch and mouse
    // Finger or mouse down: remember where the press started. Nothing changes until it lifts or moves.
    private void OnPress(object? sender, TouchEventArgs e) {
        if (_viewModel is null) return;

        // While a hint is focused, a press on the garden only unfocuses it: no tap, no drag.
        if (_viewModel.UnfocusHintIfFocused()) {
            _pressedCell = null;
            _drag = null;
            return;
        }

        var point = e.Touches[0];
        _pressedCell = _garden.CellAt(point);
        _drag = _pressedCell is not null && _garden.CellPosition(point) is { } position
            ? new DragTracker(position.X, position.Y, _garden.Board!.Size)
            : null;

        if (_pressedCell is { } pressed) {
            _garden.FocusCell = pressed;
        }

        _garden.ShowFocus = false;

#if WINDOWS || MACCATALYST
        FocusGardenForKeyboard();
#endif
    }

    private void OnDrag(object? sender, TouchEventArgs e) => DragTo(e.Touches[0]);

    // Moving a third of a cell turns the press into a drag that marks or clears every cell it passes, starting with its own.
    // A drag that starts along a row or column stays in it until the finger is clearly in another line.
    private void DragTo(PointF point) {
        if (_viewModel is null || _drag is null || _garden.CellPosition(point) is not { } position) return; // Started off the board.

        bool wasDragging = _drag.IsDragging;
        var cells = _drag.MoveTo(position.X, position.Y);
        if (cells.Count == 0) return; // Still a press.

        if (!wasDragging) {
            _viewModel.BeginDrag();
        }

        foreach (var cell in cells) {
            if (cell.IsInside(_garden.Board!.Size)) {
                _viewModel.DragOver(cell); // Redraws when it changes.
            }
        }
    }

    // Finger or mouse up: the end of a drag, or a tap if it lifted on the cell it went down on.
    private void OnRelease(object? sender, TouchEventArgs e) {
        var pressed = _pressedCell;
        var point = e.Touches[0];

        // A fast swipe can cross cells after its last move update, so cover the stretch up to where it lifted.
        DragTo(point);

        bool wasDragging = _drag?.IsDragging == true;
        FinishPress();

        if (wasDragging) return;

        var released = _garden.CellAt(point);
        if (released is null || released != pressed) return;

        _viewModel?.Tap(released.Value);
    }

    // Clears the press. A drag in progress ends and is saved once.
    private void FinishPress() {
        if (_drag?.IsDragging == true) {
            _viewModel?.EndDrag();
        }

        _pressedCell = null;
        _drag = null;
    }
    #endregion

#if WINDOWS || MACCATALYST
    #region Hover and keyboard (desktop)
    private void SetHover(Cell? cell) {
        if (_viewModel?.Board is not { } board || board.IsFinished || _garden.HoverCell == cell) return;

        _garden.HoverCell = cell;
        _canvas.Invalidate();
    }

    // Arrows move the focus ring; Space or X toggles ✕; Enter plants.
    private void OnGardenKey(GardenKeyboardKey key) {
        if (_viewModel?.Board is not { } board) return; // Still loading.

        // While a hint is focused, a key only unfocuses it, so Enter can't plant by accident.
        if (_viewModel.UnfocusHintIfFocused()) return;

        int size = board.Size;
        var focus = _garden.FocusCell ?? new Cell(0, 0);
        _garden.ShowFocus = true;

        switch (key) {
            case GardenKeyboardKey.Up:
                focus = focus.MovedBy(-1, 0, size);
                break;
            case GardenKeyboardKey.Down:
                focus = focus.MovedBy(1, 0, size);
                break;
            case GardenKeyboardKey.Left:
                focus = focus.MovedBy(0, -1, size);
                break;
            case GardenKeyboardKey.Right:
                focus = focus.MovedBy(0, 1, size);
                break;
            case GardenKeyboardKey.Mark:
                _viewModel.ToggleMark(focus);
                break;
            case GardenKeyboardKey.Plant:
                _viewModel.Plant(focus);
                break;
        }

        _garden.FocusCell = focus;
        _canvas.Invalidate();
    }

    // Shows or hides the focus ring when the garden gains or loses keyboard focus.
    private void ShowKeyboardFocus(bool show) {
        _garden.ShowFocus = show;
        if (show) {
            _garden.FocusCell ??= new Cell(0, 0);
        }

        _canvas.Invalidate();
    }
    #endregion
#endif
}
