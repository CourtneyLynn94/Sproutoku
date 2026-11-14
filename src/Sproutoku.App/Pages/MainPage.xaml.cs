using Microsoft.Maui.Controls.Shapes;
using Sproutoku.App.Controls.Garden;
using Sproutoku.App.Resources.Styles;
using Sproutoku.App.ViewModels;
using Sproutoku.Core.Game.Input;
using static Sproutoku.App.Controls.GridLayout;
using Cell = Sproutoku.Core.Gardens.Puzzles.Cell;

namespace Sproutoku.App.Pages;

/// <summary>
/// The garden screen's view code: layout, drawing, the sun meter, and turning touches and keys into cells.
/// Everything about the game lives in <see cref="GardenViewModel"/>.
/// Each region is one part of the screen and becomes its own control with the cozy UI.
/// </summary>
public partial class MainPage : ContentPage {
    private readonly GardenViewModel _viewModel;

    public MainPage(GardenViewModel viewModel) {
        _viewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();

        GardenView.Drawable = _garden;

        ApplyLayout(wide: false);
        SizeChanged += (_, _) => ApplyLayout(Width >= WideLayoutMinWidth);

        HookUpGardenInput();

        // The view model lasts for the whole app; for now there's one garden page, so it listens for as long as it lives.
        viewModel.GardenStarted += (_, _) => OnGardenStarted();
        viewModel.BoardChanged += (_, _) => OnBoardChanged();

        _ = viewModel.LoadAsync();
    }

    // A new garden: forget the last one's hover and focus, and give the sun meter the right number of suns.
    private void OnGardenStarted() {
        _garden.HoverCell = null;
        _garden.FocusCell = null;
        _garden.ShowFocus = false;

        BuildSunMeter(_viewModel.StartingSuns);
        UpdateSunMeter(animate: false);
    }

    private void OnBoardChanged() {
        DrawGarden();
        UpdateSunMeter(animate: true);
    }

    #region Layout
    // At this width and up, the desktop layout puts the list, board and shelf side by side.
    private const double WideLayoutMinWidth = 960;
    private bool? _isWide;

    // Phone: stacked vertical with board, planting list, tool shelf.
    // Desktop: stacked horizontal with planting list, board, tool shelf.
    private void ApplyLayout(bool wide) {
        if (_isWide == wide) return;
        _isWide = wide;

        if (wide) {
            GardenView.Margin = new Thickness(0);
            GameArea.RowDefinitions = Rows(GridLength.Star);
            GameArea.ColumnDefinitions = Columns(new GridLength(220), GridLength.Star, new GridLength(220));
            Place(PlantingList, row: 0, column: 0);
            Place(WormNote, row: 0, column: 0);
            Place(BoardArea, row: 0, column: 1);
            Place(ToolShelf, row: 0, column: 2);
        } else {
            // The garden reaches into the page's side padding. Only the labels sit there; the board keeps its distance from the screen's edge.
            GardenView.Margin = new Thickness(-16, 0);
            GameArea.RowDefinitions = Rows(GridLength.Star, GridLength.Auto, GridLength.Auto);
            GameArea.ColumnDefinitions = Columns(GridLength.Star);
            Place(BoardArea, row: 0, column: 0);
            Place(PlantingList, row: 1, column: 0);
            Place(WormNote, row: 1, column: 0);
            Place(ToolShelf, row: 2, column: 0);
        }

        ToolShelf.Arrange(vertical: wide);
    }
    #endregion

    #region Garden view
#if WINDOWS || MACCATALYST
    // Keyboard support. Filled in by Platforms/Windows/MainPage.Keyboard.cs; on other platforms these do nothing.
    partial void HookUpKeyboard();
    partial void FocusGardenForKeyboard();
#endif

    private readonly GardenDrawable _garden = new() { ShouldDrawRegionBorders = false };

    // The press in progress: the cell it started on (null if it started off the board), and its drag tracker.
    private Cell? _pressedCell;
    private DragTracker? _drag;

    private void HookUpGardenInput() {
        GardenView.StartInteraction += OnPress;
        GardenView.DragInteraction += OnDrag;
        GardenView.EndInteraction += OnRelease;
        GardenView.CancelInteraction += (_, _) => FinishPress();

#if WINDOWS || MACCATALYST
        // Desktop hover. Touch screens don't send hover events, so this does nothing on a phone.
        GardenView.MoveHoverInteraction += (_, e) => SetHover(_garden.CellAt(e.Touches[0]));
        GardenView.EndHoverInteraction += (_, _) => SetHover(null);

        // Desktop keyboard.
        HookUpKeyboard();
#endif
    }

    // Shows the view model's board and hint.
    // A focused hint dims the board and shows its preview marks; its labels stay while the note is up, focused or not.
    private void DrawGarden() {
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

        GardenView.Invalidate();
    }

    // Finger or mouse down: remember where the press started. Nothing changes until it lifts or moves.
    private void OnPress(object? sender, TouchEventArgs e) {
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
        if (_drag is null || _garden.CellPosition(point) is not { } position) return; // Started off the board.

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

        _viewModel.Tap(released.Value);
    }

    // Clears the press. A drag in progress ends and is saved once.
    private void FinishPress() {
        if (_drag?.IsDragging == true) {
            _viewModel.EndDrag();
        }

        _pressedCell = null;
        _drag = null;
    }

#if WINDOWS || MACCATALYST
    private void SetHover(Cell? cell) {
        if (_viewModel.Board is not { } board || board.IsFinished || _garden.HoverCell == cell) return;

        _garden.HoverCell = cell;
        GardenView.Invalidate();
    }

    // Arrows move the focus ring; Space or X toggles ✕; Enter plants.
    private void OnGardenKey(GardenKeyboardKey key) {
        if (_viewModel.Board is not { } board) return; // Still loading.

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
        GardenView.Invalidate();
    }

    // Shows or hides the focus ring when the garden gains or loses keyboard focus.
    private void ShowKeyboardFocus(bool show) {
        _garden.ShowFocus = show;
        if (show) {
            _garden.FocusCell ??= new Cell(0, 0);
        }

        GardenView.Invalidate();
    }
#endif
    #endregion

    #region Sun meter
    private static Color Ink => Palette.Ink;
    private static Color SunColor => Palette.Sun;
    private static Color SpentSunColor => Palette.SpentSun;

    // The bright sun in each sun slot, left to right. Spending a sun fades one out.
    private readonly List<View> _brightSuns = [];

    // Each sun slot stacks a bright sun on top of a spent one (pale, dashed outline).
    // A gentle arc: with more than one sun, the outer suns sit a little lower.
    private void BuildSunMeter(int suns) {
        SunMeter.Children.Clear();
        _brightSuns.Clear();

        for (int i = 0; i < suns; i++) {
            var spent = new Ellipse {
                Fill = new SolidColorBrush(SpentSunColor),
                Stroke = new SolidColorBrush(Ink),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 3, 3 },
            };
            var bright = new Ellipse {
                Fill = new SolidColorBrush(SunColor),
                Stroke = new SolidColorBrush(Ink),
                StrokeThickness = 2,
            };

            SunMeter.Children.Add(new Grid {
                WidthRequest = 44,
                HeightRequest = 44,
                TranslationY = suns > 1 && (i == 0 || i == suns - 1) ? 6 : 0,
                Children = { spent, bright },
            });
            _brightSuns.Add(bright);
        }
    }

    // Shows as many bright suns as are left. The rightmost sun is spent first.
    private void UpdateSunMeter(bool animate) {
        for (int i = 0; i < _brightSuns.Count; i++) {
            var bright = _brightSuns[i];
            double target = i < _viewModel.SunsLeft ? 1 : 0;
            if (bright.Opacity == target) continue;

            if (animate) {
                _ = bright.FadeToAsync(target, 300);
            } else {
                bright.Opacity = target;
            }
        }
    }
    #endregion
}
