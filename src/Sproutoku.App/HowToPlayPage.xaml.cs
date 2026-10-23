using Sproutoku.App.Garden;
using Sproutoku.Core.Play;
using Sproutoku.Core.Puzzles;
using Cell = Sproutoku.Core.Puzzles.Cell;

namespace Sproutoku.App;

public partial class HowToPlayPage : ContentPage {
    public HowToPlayPage() {
        InitializeComponent();
        ExampleView.Drawable = new GardenDrawable { Board = ExampleBoard() };
    }

    // The 4×4 tutorial garden, part solved: one plant grown, with ✕ where nothing else can grow.
    private static Board ExampleBoard() {
        var puzzle = PuzzleText.Parse(
            "a A b b",
            "a a b B",
            "C c d b",
            "c d D d");

        var board = Board.Start(puzzle).TryPlant(new Cell(1, 3)).Board;

        Cell[] marks = [new(1, 0), new(1, 1), new(1, 2), new(0, 3), new(2, 3), new(3, 3), new(0, 2), new(2, 2)];
        foreach (var cell in marks) {
            board = board.ToggleMark(cell);
        }

        return board;
    }

    private async void OnCloseClicked(object? sender, EventArgs e) {
        await Navigation.PopModalAsync();
    }
}
