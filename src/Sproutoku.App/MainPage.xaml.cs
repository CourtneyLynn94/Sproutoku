using Sproutoku.App.DevTools;
using Sproutoku.App.Garden;
using Sproutoku.Core.Play;
using Sproutoku.Core.Puzzles;

namespace Sproutoku.App;

public partial class MainPage : ContentPage {
    private readonly GardenDrawable _garden = new();
    private int _shown;

    public MainPage() {
        InitializeComponent();
        GardenView.Drawable = _garden;

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
        InfoLabel.Text = $"{source}: {puzzle.Size} × {puzzle.Size}";
        GardenView.Invalidate();
    }

    private void SetHover(Core.Puzzles.Cell? cell) {
        if (_garden.HoverCell == cell) return;

        _garden.HoverCell = cell;
        GardenView.Invalidate();
    }
}
