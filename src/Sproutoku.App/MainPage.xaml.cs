using Microsoft.Maui.Controls.Shapes;
using Sproutoku.App.Garden;
using Sproutoku.Core.Input;
using Sproutoku.Core.Play;
using Sproutoku.Core.Puzzles;
using Cell = Sproutoku.Core.Puzzles.Cell;

namespace Sproutoku.App;

public partial class MainPage : ContentPage {
    // At this width and up, the desktop layout puts the list, board and shelf side by side.
    private const double WideLayoutMinWidth = 960;
    private bool? _isWide;

    private readonly GardenDrawable _garden = new() { ShouldDrawRegionBorders = false };

    // The level being played (1 is the first garden).
    private int _level = 1;

    // The cell the current press started on, or null.
    private Cell? _pressedCell;

    private readonly DoubleTapDetector _doubleTap = new();

    // The board from just before the last single tap, so a double tap can take that tap back.
    private Board? _boardBeforeTap;

    public MainPage() {
        InitializeComponent();
        GardenView.Drawable = _garden;

        ApplyLayout(wide: false);
        SizeChanged += (_, _) => ApplyLayout(Width >= WideLayoutMinWidth);

        GardenView.StartInteraction += OnPress;
        GardenView.EndInteraction += OnRelease;
        GardenView.CancelInteraction += (_, _) => _pressedCell = null;

#if WINDOWS || MACCATALYST
        // Desktop hover. Touch screens don't send hover events, so this does nothing on a phone.
        GardenView.MoveHoverInteraction += (_, e) => SetHover(_garden.CellAt(e.Touches[0]));
        GardenView.EndHoverInteraction += (_, _) => SetHover(null);
#endif

        ShowLevel(1);
    }

    // Plays the garden for a level and shows its number on the sign.
    private void ShowLevel(int level) {
        var puzzle = GardenLevels.ForLevel(level)
            ?? throw new ArgumentOutOfRangeException(nameof(level), $"There is no garden for level {level}.");

        _level = level;
        ShowBoard(Board.Start(puzzle), $"Starter garden, {puzzle.Size} × {puzzle.Size}");
    }

    // Try again: the same garden, every cell empty, every sun back.
    private void RestartGarden() {
        if (_garden.Board is null) return;

        ShowBoard(_garden.Board.Restart(), "A fresh start");
    }

    private void ShowBoard(Board board, string info) {
        _garden.Board = board;
        _garden.HoverCell = null;
        _doubleTap.Reset();
        _boardBeforeTap = null;

        BuildSunMeter(board.StartingSuns);
        UpdateSunMeter(animate: false);

        GardenSignLabel.Text = $"Garden {_level}";
        InfoLabel.Text = info;
        UpdatePlantingList();
        GardenView.Invalidate();
    }

    private void UpdatePlantingList() {
        if (_garden.Board is null) return;

        PlantingCountLabel.Text = $"{_garden.Board.CountOf(CellState.Planted)} of {_garden.Board.Size} grown";
    }

    // Rough end-of-garden screens until the real sheets are designed.
    private async void ShowEndOfGarden() {
        var board = _garden.Board;
        if (board is null) return;

        // Reset hovered cell state
        _garden.HoverCell = null;
        GardenView.Invalidate();

        // Let the last plant grow or the last sun fade before the screen appears.
        await Task.Delay(500);

        if (board.IsSolved) {
            string stats =
                $"Garden {_level}\n" +
                $"Suns left: {board.SunsLeft} of {board.StartingSuns}\n" +
                $"Mistakes: {board.Mistakes}\n" +
                $"Beds: {board.Size}";

            bool hasNextLevel = GardenLevels.ForLevel(_level + 1) is not null;
            if (hasNextLevel) {
                await DisplayAlertAsync("Garden complete", stats, "Next garden");
                ShowLevel(_level + 1);
            } else {
                await DisplayAlertAsync("Garden complete", stats + "\n\nMore gardens coming soon.", "Close");
            }

            return;
        }

        const string Title = "Your garden needs more sun";
        const string Message = "All your suns are spent. Give this garden a fresh start.";

        await DisplayAlertAsync(Title, Message, "Try again");
        RestartGarden();
    }

    // Rough feedback.
    private void ShowPlantOutcome(PlantResult result) {
        InfoLabel.Text = result.Outcome switch {
            PlantOutcome.Grew when result.Board.IsSolved => "Garden complete",
            PlantOutcome.Grew => "It grew",
            PlantOutcome.Wilted => "Wrong spot: it wilted",
            _ => InfoLabel.Text,
        };
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

        string choice = await DisplayActionSheetAsync("Garden journal", "Keep gardening", null, [.. options]);

        switch (choice) {
            case "How to play":
            await Navigation.PushModalAsync(new HowToPlayPage());
            break;
#if DEBUG
            case "Skip to next garden (dev)":
            if (GardenLevels.ForLevel(_level + 1) is not null) {
                ShowLevel(_level + 1);
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

    // Finger or mouse down: remember which cell the press started on.
    private void OnPress(object? sender, TouchEventArgs e) {
        _pressedCell = _garden.CellAt(e.Touches[0]);
    }

    // Finger or mouse up on the same cell it went down on counts as a tap.
    private void OnRelease(object? sender, TouchEventArgs e) {
        var pressed = _pressedCell;
        _pressedCell = null;

        var released = _garden.CellAt(e.Touches[0]);
        if (released is null || released != pressed) {
            return;
        }

        OnCellTapped(released.Value);
    }

    // A single tap toggles ✕.
    // A double tap takes the first tap back and tries to plant on the cell as it was before.
    private void OnCellTapped(Cell cell) {
        if (_garden.Board is null || _garden.Board.IsFinished) return;

        if (_doubleTap.RegisterTap(cell, Environment.TickCount64) && _boardBeforeTap is not null) {
            var result = _boardBeforeTap.TryPlant(cell);
            _garden.Board = result.Board;
            _boardBeforeTap = null;
            ShowPlantOutcome(result);
        } else {
            _boardBeforeTap = _garden.Board;
            _garden.Board = _garden.Board.ToggleMark(cell);
        }

        UpdatePlantingList();
        UpdateSunMeter(animate: true);
        GardenView.Invalidate();
        if (_garden.Board.IsFinished) {
            ShowEndOfGarden();
        }
    }

#if WINDOWS || MACCATALYST
    private void SetHover(Cell? cell) {
        var board = _garden.Board;
        if (board is null) return;

        if (board.IsSolved || _garden.HoverCell == cell) return;

        _garden.HoverCell = cell;
        GardenView.Invalidate();
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
        if (_garden.Board is null) return;

        for (int i = 0; i < _brightSuns.Count; i++) {
            var bright = _brightSuns[i];
            double target = i < _garden.Board.SunsLeft ? 1 : 0;
            if (bright.Opacity == target) continue;

            if (animate) {
                _ = bright.FadeToAsync(target, 300);
            } else {
                bright.Opacity = target;
            }
        }

        SemanticProperties.SetDescription(SunMeter, $"{_garden.Board.SunsLeft} of {_garden.Board.StartingSuns} suns left");
    }

    #endregion
}
