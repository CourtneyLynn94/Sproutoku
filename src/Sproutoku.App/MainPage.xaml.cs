using Sproutoku.App.DevTools;
using Sproutoku.App.Garden;
using Sproutoku.Core.Play;
using Sproutoku.Core.Puzzles;
using Sproutoku.Core.Input;
using Cell = Sproutoku.Core.Puzzles.Cell;

namespace Sproutoku.App;

public partial class MainPage : ContentPage {
    private readonly GardenDrawable _garden = new() { ShouldDrawRegionBorders = false };
    private int _shown;

    // The cell the current press started on, or null.
    private Cell? _pressedCell;

    private readonly DoubleTapDetector _doubleTap = new();

    // The board from just before the last single tap, so a double tap can take that tap back.
    private Board? _boardBeforeTap;

    public MainPage() {
        InitializeComponent();
        GardenView.Drawable = _garden;

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

    private void OnNewGardenClicked(object? sender, EventArgs e) => ShowNewGarden();

    // Steps through the starter gardens, then rough random ones.
    private void ShowNewGarden() {
        Puzzle puzzle;
        string source;

        if (_shown < StarterGardens.All.Count) {
            puzzle = StarterGardens.All[_shown];
            source = $"Starter garden {_shown + 1}";
        } else {
            puzzle = RoughGenerator.Generate(Random.Shared.Next(5, 10), Random.Shared);
            source = "Rough random garden";
        }

        _shown++;

        _garden.Board = Board.Start(puzzle);
        _garden.HoverCell = null;
        _doubleTap.Reset();
        _boardBeforeTap = null;
        InfoLabel.Text = $"{source}: {puzzle.Size} × {puzzle.Size}";
        GardenView.Invalidate();
    }

    #region Event Interactions

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
