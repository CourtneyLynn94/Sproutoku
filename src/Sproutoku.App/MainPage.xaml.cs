using Sproutoku.App.DevTools;
using Sproutoku.App.Garden;
using Sproutoku.Core.Input;
using Sproutoku.Core.Play;
using Sproutoku.Core.Puzzles;
using Cell = Sproutoku.Core.Puzzles.Cell;

namespace Sproutoku.App;

public partial class MainPage : ContentPage {
    // At this width and up, the desktop layout puts the list, board and shelf side by side.
    private const double WideLayoutMinWidth = 960;

    private readonly GardenDrawable _garden = new() { ShouldDrawRegionBorders = false };
    private int _shown;
    private bool? _isWide;

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

        ShowNewGarden();
    }

    // Steps through the starter gardens, then rough random ones.
    private void ShowNewGarden() {
        Puzzle puzzle;
        string source;

        if (_shown < StarterGardens.All.Count) {
            puzzle = StarterGardens.All[_shown];
            source = "Starter garden";
        } else {
            puzzle = RoughGenerator.Generate(Random.Shared.Next(5, 10), Random.Shared);
            source = "Rough random garden";
        }

        _shown++;

        _garden.Board = Board.Start(puzzle);
        _garden.HoverCell = null;
        _doubleTap.Reset();
        _boardBeforeTap = null;

        GardenSignLabel.Text = $"Garden {_shown}";
        InfoLabel.Text = $"{source}, {puzzle.Size} × {puzzle.Size}";
        UpdatePlantingList();
        GardenView.Invalidate();
    }

    private void UpdatePlantingList() {
        if (_garden.Board is null) return;

        PlantingCountLabel.Text = $"{_garden.Board.CountOf(CellState.Planted)} of {_garden.Board.Size} grown";
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
        List<string> options = ["New garden"];
#if DEBUG
        options.Add(_garden.ShowSolution ? "Hide solution (dev)" : "Show solution (dev)");
#endif

        string choice = await DisplayActionSheetAsync("Garden journal", "Keep gardening", null, [.. options]);

        switch (choice) {
            case "New garden":
            ShowNewGarden();
            break;
#if DEBUG
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
        if (_garden.Board is null) return;

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
        GardenView.Invalidate();
    }

    // Rough feedback until sunlight (S18) and the complete screen (S20) exist.
    private void ShowPlantOutcome(PlantResult result) {
        InfoLabel.Text = result.Outcome switch {
            PlantOutcome.Grew when result.Board.IsSolved => "Garden complete",
            PlantOutcome.Grew => "It grew",
            PlantOutcome.Wilted => "Wrong spot: it wilted",
            _ => InfoLabel.Text,
        };
    }

#if WINDOWS || MACCATALYST
    private void SetHover(Cell? cell) {
        if (_garden.HoverCell == cell) return;

        _garden.HoverCell = cell;
        GardenView.Invalidate();
    }
#endif

    #endregion
}
