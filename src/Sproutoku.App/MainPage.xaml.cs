using Microsoft.Maui.Controls.Shapes;
using Sproutoku.App.Garden;
using Sproutoku.App.Saving;
using Sproutoku.Core.Play;
using Cell = Sproutoku.Core.Puzzles.Cell;

#if ANDROID || IOS
using Sproutoku.App.Feedback;
#endif

namespace Sproutoku.App;

public partial class MainPage : ContentPage {
    // At this width and up, the desktop layout puts the list, board and shelf side by side.
    private const double WideLayoutMinWidth = 960;
    private bool? _isWide;

#if WINDOWS || MACCATALYST
    // Keyboard support. Filled in by Platforms/Windows/MainPage.Keyboard.cs; on other platforms these do nothing.
    partial void HookUpKeyboard();
    partial void FocusGardenForKeyboard();
#elif ANDROID || IOS
    private readonly Haptics _haptics = new();
#endif

    private readonly GardenDrawable _garden = new() { ShouldDrawRegionBorders = false };
    private readonly GameStorage _gameStorage = new();

    // The game itself: level, board and every rule. The page only shows it and passes input in.
    private readonly GameSession _session;

    // The press in progress:
    // the cell it started on (null if it started off the board), the last point it was seen at, and whether it has turned into a drag.
    private Cell? _pressedCell;
    private PointF _lastPoint;
    private bool _isDragging;

    public MainPage() {
        InitializeComponent();
        GardenView.Drawable = _garden;

        ApplyLayout(wide: false);
        SizeChanged += (_, _) => ApplyLayout(Width >= WideLayoutMinWidth);

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

        // Pick up where the player left off, or start at Garden 1.
        var save = _gameStorage.Load();
        _session = GameSession.Resume(save);
        ShowGarden(save is null ? StarterInfo() : "Welcome back");
    }

    // Draws the session's garden from scratch (new garden, Try again, reset) and saves.
    private void ShowGarden(string info) {
        _garden.Board = _session.Board;
        _garden.HoverCell = null;
        _garden.FocusCell = null;
        _garden.ShowFocus = false;

        BuildSunMeter(_session.Board.StartingSuns);
        UpdateSunMeter(animate: false);

        GardenSignLabel.Text = $"Garden {_session.Level}";
        InfoLabel.Text = info;
        UpdatePlantingList();
        GardenView.Invalidate();

        SaveProgress();
    }

    private string StarterInfo() => $"Starter garden, {_session.Board.Size} × {_session.Board.Size}";

    private void SaveProgress() => _gameStorage.Save(_session.ToSave());

    private void UpdatePlantingList() {
        PlantingCountLabel.Text = $"{_session.Board.CountOf(CellState.Planted)} of {_session.Board.Size} grown";
    }

    // Rough feedback.
    private void ShowTapOutcome(TapOutcome outcome) {
        InfoLabel.Text = outcome switch {
            TapOutcome.Grew when _session.Board.IsSolved => "Garden complete",
            TapOutcome.Grew => "It grew",
            TapOutcome.Wilted => "Wrong spot: it wilted",
            _ => InfoLabel.Text,
        };
    }

    // A complete progress reset. Asks first, because it can't be undone.
    private async Task ConfirmResetAsync() {
        bool erase = await DisplayAlertAsync(
            "Reset all progress?",
            "This erases every garden you've grown and everything you've earned, and starts you again at Garden 1. Your settings stay as they are. It can't be undone.",
            "Erase my progress",
            "Keep my progress");

        if (!erase) return;

        _gameStorage.ResetProgress();
        _session.GoToLevel(1);
        ShowGarden(StarterInfo());
    }

    // Rough end-of-garden screens until the real sheets are designed.
    // The session decides whether moving on or starting over is allowed; the page only asks.
    private async void ShowEndOfGarden() {
        var board = _session.Board;

        // Reset hovered cell state
        _garden.HoverCell = null;
        GardenView.Invalidate();

        // Let the last plant grow or the last sun fade before the screen appears.
        await Task.Delay(500);

        // The Journal still works during the wait. If a reset or skip changed the garden, this screen is stale.
        if (!ReferenceEquals(_session.Board, board)) return;

#if ANDROID || IOS
        _haptics.GardenEnded();
#endif

        if (board.IsSolved) {
            string stats =
                $"Garden {_session.Level}\n" +
                $"Suns left: {board.SunsLeft} of {board.StartingSuns}\n" +
                $"Mistakes: {board.Mistakes}\n" +
                $"Beds: {board.Size}";

            if (_session.HasNextLevel) {
                await DisplayAlertAsync("Garden complete", stats, "Next garden");
                if (_session.TryAdvance()) {
                    ShowGarden(StarterInfo());
                }
            } else {
                await DisplayAlertAsync(
                    "Garden complete",
                    stats + "\n\nYou've grown every garden for now. More are coming soon.",
                    "Play it again");
                if (_session.TryRestart()) {
                    ShowGarden("A fresh start");
                }
            }

            return;
        }

        const string Title = "Your garden needs more sun";
        const string Message = "All your suns are spent. Give this garden a fresh start.";

        await DisplayAlertAsync(Title, Message, "Try again");
        if (_session.TryRestart()) {
            ShowGarden("A fresh start");
        }
    }

    #region Layout
    // Phone: stacked vertical with board, planting list, tool shelf.
    // Desktop: stacked horizontal with planting list, board, tool shelf.
    private void ApplyLayout(bool wide) {
        if (_isWide == wide) return;
        _isWide = wide;

        if (wide) {
            GameArea.RowDefinitions = Rows(GridLength.Star);
            GameArea.ColumnDefinitions = Columns(new GridLength(240), GridLength.Star, new GridLength(200));
            Place(PlantingList, row: 0, column: 0);
            Place(BoardArea, row: 0, column: 1);
            Place(ToolShelf, row: 0, column: 2);
        } else {
            GameArea.RowDefinitions = Rows(GridLength.Star, GridLength.Auto, GridLength.Auto);
            GameArea.ColumnDefinitions = Columns(GridLength.Star);
            Place(BoardArea, row: 0, column: 0);
            Place(PlantingList, row: 1, column: 0);
            Place(ToolShelf, row: 2, column: 0);
        }

        ArrangeToolShelf(vertical: wide);
    }

    // Phone: tools share one row, so they shrink to fit a 360px screen.
    // Desktop: tools stack in one column.
    private void ArrangeToolShelf(bool vertical) {
        View[] tools = [SeedsButton, WormButton, WaterButton, ShelfDivider, RakeButton];

        if (vertical) {
            ToolShelf.ColumnDefinitions = Columns(GridLength.Star);
            ToolShelf.RowDefinitions = Rows(GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto);
        } else {
            ToolShelf.ColumnDefinitions = Columns(GridLength.Star, GridLength.Star, GridLength.Star, GridLength.Auto, GridLength.Star);
            ToolShelf.RowDefinitions = Rows(GridLength.Auto);
        }

        for (int i = 0; i < tools.Length; i++) {
            Place(tools[i], row: vertical ? i : 0, column: vertical ? 0 : i);
        }

        // The divider is a thin vertical line in a row, a thin horizontal line in a column. -1 means "not set".
        ShelfDivider.WidthRequest = vertical ? -1 : 2;
        ShelfDivider.HeightRequest = vertical ? 2 : -1;
    }

    private static void Place(BindableObject view, int row, int column) {
        Grid.SetRow(view, row);
        Grid.SetColumn(view, column);
    }

    private static RowDefinitionCollection Rows(params GridLength[] heights) =>
        new(heights.Select(height => new RowDefinition(height)).ToArray());

    private static ColumnDefinitionCollection Columns(params GridLength[] widths) =>
        new(widths.Select(width => new ColumnDefinition(width)).ToArray());
    #endregion

    #region Event Interactions
    // Rough Garden journal.
    private async void OnJournalClicked(object? sender, EventArgs e) {
        List<string> options = ["How to play"];
#if DEBUG
        options.Add("Skip to next garden (dev)");
        options.Add(_garden.ShowSolution ? "Hide solution (dev)" : "Show solution (dev)");
#endif

        string choice = await DisplayActionSheetAsync("Garden journal", "Keep gardening", "Reset all progress", [.. options]);

        switch (choice) {
            case "How to play":
                await Navigation.PushModalAsync(new HowToPlayPage());
                break;
            case "Reset all progress":
                await ConfirmResetAsync();
                break;
#if DEBUG
            case "Skip to next garden (dev)":
                if (_session.GoToLevel(_session.Level + 1)) {
                    ShowGarden(StarterInfo());
                }
                break;
            case "Show solution (dev)":
            case "Hide solution (dev)":
                _garden.ShowSolution = !_garden.ShowSolution;
                GardenView.Invalidate();
                break;
#endif
        }
    }

    // Finger or mouse down: remember where the press started. Nothing changes until it lifts or moves.
    private void OnPress(object? sender, TouchEventArgs e) {
        _pressedCell = _garden.CellAt(e.Touches[0]);
        _lastPoint = e.Touches[0];
        _isDragging = false;

        if (_pressedCell is { } pressed) {
            _garden.FocusCell = pressed;
        }

        _garden.ShowFocus = false;

#if WINDOWS || MACCATALYST
        FocusGardenForKeyboard();
#endif
    }

    // Moving onto another cell turns the press into a drag that marks or clears every cell it passes.
    private void OnDrag(object? sender, TouchEventArgs e) {
        if (_pressedCell is not { } start) return; // started off the board

        var point = e.Touches[0];
        bool changed = false;

        if (!_isDragging) {
            // Still on the starting cell, so it may yet be a tap.
            if (_garden.CellAt(point) == start) {
                _lastPoint = point;
                return;
            }

            _isDragging = true;
            _session.BeginDrag();
            if (_session.DragOver(start)) {
                changed = true;
#if ANDROID || IOS
                _haptics.Mark();
#endif
            }
        }

        foreach (var cell in _garden.CellsAlong(_lastPoint, point)) {
            if (_session.DragOver(cell)) {
                changed = true;
#if ANDROID || IOS
                _haptics.Mark();
#endif
            }
        }

        _lastPoint = point;

        if (changed) {
            _garden.Board = _session.Board;
            GardenView.Invalidate();
        }
    }

    // Finger or mouse up: the end of a drag, or a tap if it lifted on the cell it went down on.
    private void OnRelease(object? sender, TouchEventArgs e) {
        var pressed = _pressedCell;
        bool wasDragging = _isDragging;
        FinishPress();

        if (wasDragging) return;

        var released = _garden.CellAt(e.Touches[0]);
        if (released is null || released != pressed) return;

        OnCellTapped(released.Value);
    }

    // Clears the press. A drag in progress ends and is saved once.
    private void FinishPress() {
        if (_isDragging) {
            _session.EndDrag();
            SaveProgress();
        }

        _pressedCell = null;
        _isDragging = false;
    }

    private void OnCellTapped(Cell cell) => ShowResult(_session.Tap(cell, Environment.TickCount64));

    // Shows what a tap or key did, saves, and opens the end screen if the garden is over.
    private void ShowResult(TapResult result) {
        if (result.Outcome == TapOutcome.Ignored) return;

#if ANDROID || IOS
        PlayHaptic(result.Outcome);
#endif

        _garden.Board = _session.Board;
        ShowTapOutcome(result.Outcome);
        UpdatePlantingList();
        UpdateSunMeter(animate: true);
        GardenView.Invalidate();

        SaveProgress();

        if (result.EndedGarden) {
            ShowEndOfGarden();
        }
    }

#if WINDOWS || MACCATALYST
    private void SetHover(Cell? cell) {
        if (_session.Board.IsFinished || _garden.HoverCell == cell) return;

        _garden.HoverCell = cell;
        GardenView.Invalidate();
    }

    // Arrows move the focus ring; Space or X toggles ✕; Enter plants.
    private void OnGardenKey(GardenKeyboardKey key) {
        int size = _session.Board.Size;
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
                ShowResult(_session.ToggleMark(focus));
                break;
            case GardenKeyboardKey.Plant:
                ShowResult(_session.Plant(focus));
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
#elif ANDROID || IOS
    // A light tick for ✕, a firmer press for a plant, a buzz for a mistake.
    private void PlayHaptic(TapOutcome outcome) {
        switch (outcome) {
            case TapOutcome.MarkToggled:
                _haptics.Mark();
                break;
            case TapOutcome.Grew:
                _ = _haptics.GrewAsync();
                break;
            case TapOutcome.Wilted:
                _haptics.Wilted();
                break;
        }
    }
#endif
    #endregion

    #region Sun meter
    private static readonly Color Ink = Color.FromArgb("#2E241B");
    private static readonly Color SunColor = Color.FromArgb("#F2B632");
    private static readonly Color SpentSunColor = Color.FromArgb("#D9CFB6");

    // The bright sun in each sun slot, left to right.
    // Spending a sun fades one out.
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

    // Shows as many bright suns as the board has left.
    // The rightmost sun is spent first.
    private void UpdateSunMeter(bool animate) {
        var board = _session.Board;

        for (int i = 0; i < _brightSuns.Count; i++) {
            var bright = _brightSuns[i];
            double target = i < board.SunsLeft ? 1 : 0;
            if (bright.Opacity == target) continue;

            if (animate) {
                _ = bright.FadeToAsync(target, 300);
            } else {
                bright.Opacity = target;
            }
        }

        SemanticProperties.SetDescription(SunMeter, $"{board.SunsLeft} of {board.StartingSuns} suns left");
    }
    #endregion
}
