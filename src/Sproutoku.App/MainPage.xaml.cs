using System.Text;
using Sproutoku.App.DevTools;
using Sproutoku.Core.Puzzles;
using Cell = Sproutoku.Core.Puzzles.Cell;

namespace Sproutoku.App;

public partial class MainPage : ContentPage {
    public MainPage() {
        InitializeComponent();
        ShowNewGrid();
    }

    private void OnNewGridClicked(object? sender, EventArgs e) => ShowNewGrid();

    private void ShowNewGrid() {
        int size = Random.Shared.Next(5, 10); // 5 to 9
        var puzzle = RoughGenerator.Generate(size, Random.Shared);

        InfoLabel.Text = $"{size} × {size}   (* = solution)";
        GridLabel.Text = Format(puzzle);
    }

    // One line per row: each cell's region id, then * if it holds the solution plant.
    private static string Format(Puzzle puzzle) {
        var text = new StringBuilder();
        for (int row = 0; row < puzzle.Size; row++) {
            var line = new StringBuilder();
            for (int column = 0; column < puzzle.Size; column++) {
                var cell = new Cell(row, column);
                line.Append(puzzle.RegionAt(cell));
                line.Append(puzzle.IsSolutionCell(cell) ? "* " : "  ");
            }

            text.AppendLine(line.ToString().TrimEnd());
        }

        return text.ToString().TrimEnd();
    }
}