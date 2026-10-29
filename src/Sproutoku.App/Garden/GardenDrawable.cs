using Sproutoku.Core.Play;
using Sproutoku.Core.Puzzles;
using Cell = Sproutoku.Core.Puzzles.Cell;

namespace Sproutoku.App.Garden;

/// <summary>
/// Draws a board: region colors, cell lines, region borders, ✕ marks and plants.
/// </summary>
public sealed class GardenDrawable : IDrawable {
    private const float MaxBoardSize = 520;

    // Growing-medium colors, one per region.
    private static readonly Color[] RegionColors = [
        Color.FromArgb("#B08560"), // soil
        Color.FromArgb("#9FCAD4"), // water
        Color.FromArgb("#EBD498"), // sand
        Color.FromArgb("#E4E2D8"), // perlite
        Color.FromArgb("#AEB4B3"), // pebble
        Color.FromArgb("#D69A5C"), // bark
        Color.FromArgb("#A9C47B"), // moss
        Color.FromArgb("#E0A090"), // clay
        Color.FromArgb("#A59C6C"), // mulch
        Color.FromArgb("#B79CB2"), // lava
    ];

    private static readonly Color Ink = Color.FromArgb("#2E241B");
    private static readonly Color Leaf = Color.FromArgb("#487634");
    private static readonly Color Wilt = Color.FromArgb("#8E3F45");

    // Worked out on each draw; used to turn a point into a cell.
    private PointF _origin;
    private float _cellSize;

    public Board? Board { get; set; }

    /// <summary>
    /// The cell under the mouse pointer (desktop only), or null.
    /// </summary>
    public Cell? HoverCell { get; set; }

    /// <summary>
    /// Optional: Draw region borders.
    /// </summary>
    public bool ShouldDrawRegionBorders { get; set; } = false;

    /// <summary>
    /// Developer view: ring every solution cell.
    /// Drawing only; the board is not changed.
    /// </summary>
    public bool ShowSolution { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect) {
        if (Board is null) return;

        int size = Board.Size;
        float side = Math.Min(Math.Min(dirtyRect.Width, dirtyRect.Height), MaxBoardSize);
        _cellSize = side / size;
        _origin = new PointF(dirtyRect.Center.X - (side / 2), dirtyRect.Center.Y - (side / 2));

        for (int row = 0; row < size; row++) {
            for (int column = 0; column < size; column++) {
                var cell = new Cell(row, column);
                RectF rect = CellRect(cell);

                canvas.FillColor = RegionColors[Board.Puzzle.RegionAt(cell)];
                canvas.FillRectangle(rect);

                if (cell == HoverCell) {
                    canvas.FillColor = Colors.White.WithAlpha(0.35f);
                    canvas.FillRectangle(rect);
                }

                DrawContent(canvas, rect, Board[cell]);

                // Developer view:
                if (ShowSolution && Board.Puzzle.IsSolutionCell(cell) && Board[cell] != CellState.Planted) {
                    DrawSolutionRing(canvas, rect);
                }
            }
        }

        canvas.StrokeLineCap = LineCap.Square;
        DrawEdges(canvas, Board.Puzzle, regionBorders: false);
        if (ShouldDrawRegionBorders) {
            DrawEdges(canvas, Board.Puzzle, regionBorders: true);
        }

        canvas.StrokeColor = Ink;
        canvas.StrokeSize = 3;
        canvas.DrawRectangle(_origin.X, _origin.Y, side, side);
    }

    /// <summary>
    /// The cell at a point in the view, or null if the point is outside the board.
    /// </summary>
    public Cell? CellAt(PointF point) {
        if (Board is null || _cellSize <= 0) return null;

        var cell = new Cell(
            (int)MathF.Floor((point.Y - _origin.Y) / _cellSize),
            (int)MathF.Floor((point.X - _origin.X) / _cellSize)
        );
        return cell.IsInside(Board.Size) ? cell : null;
    }

    private RectF CellRect(Cell cell) =>
        new(_origin.X + (cell.Column * _cellSize), _origin.Y + (cell.Row * _cellSize), _cellSize, _cellSize);

    private static void DrawContent(ICanvas canvas, RectF rect, CellState state) {
        float inset = rect.Width * 0.28f;
        var inner = new RectF(rect.X + inset, rect.Y + inset, rect.Width - (2 * inset), rect.Height - (2 * inset));

        switch (state) {
            case CellState.Marked:
                canvas.StrokeColor = Ink;
                canvas.StrokeSize = 3;
                DrawCross(canvas, inner);
                break;

            case CellState.Planted:
                canvas.FillColor = Leaf;
                canvas.FillCircle(rect.Center.X, rect.Center.Y, inner.Width / 2);
                break;

            case CellState.Wilted:
                canvas.StrokeColor = Wilt;
                canvas.StrokeSize = 3;
                canvas.DrawCircle(rect.Center.X, rect.Center.Y, inner.Width / 2);
                canvas.DrawLine(inner.Left, inner.Bottom, inner.Right, inner.Top);
                break;

            case CellState.RevealedEmpty:
                canvas.StrokeColor = Ink.WithAlpha(0.45f);
                canvas.StrokeSize = 2;
                DrawCross(canvas, inner);
                break;
        }
    }

    private static void DrawCross(ICanvas canvas, RectF area) {
        canvas.DrawLine(area.Left, area.Top, area.Right, area.Bottom);
        canvas.DrawLine(area.Right, area.Top, area.Left, area.Bottom);
    }

    private static void DrawSolutionRing(ICanvas canvas, RectF rect) {
        canvas.StrokeColor = Leaf;
        canvas.StrokeSize = 3;
        canvas.DrawCircle(rect.Center.X, rect.Center.Y, rect.Width * 0.36f);
    }

    // Draws the line on each cell's right and bottom edge:
    // thin inside a region (first pass),
    // thick where two regions meet (second pass, so borders sit on top).
    private void DrawEdges(ICanvas canvas, Puzzle puzzle, bool regionBorders) {
        canvas.StrokeColor = regionBorders ? Ink : Ink.WithAlpha(0.22f);
        canvas.StrokeSize = regionBorders ? 3 : 1;

        for (int row = 0; row < puzzle.Size; row++) {
            for (int column = 0; column < puzzle.Size; column++) {
                var cell = new Cell(row, column);
                RectF rect = CellRect(cell);

                var right = new Cell(row, column + 1);
                if (right.IsInside(puzzle.Size) && ShouldDrawEdge(puzzle, cell, right, regionBorders)) {
                    canvas.DrawLine(rect.Right, rect.Top, rect.Right, rect.Bottom);
                }

                var below = new Cell(row + 1, column);
                if (below.IsInside(puzzle.Size) && ShouldDrawEdge(puzzle, cell, below, regionBorders)) {
                    canvas.DrawLine(rect.Left, rect.Bottom, rect.Right, rect.Bottom);
                }
            }
        }
    }

    // Thin pass: every edge, unless thick borders will cover region edges.
    // Thick pass: region edges only.
    private bool ShouldDrawEdge(Puzzle puzzle, Cell a, Cell b, bool regionBorders) {
        bool isBorder = IsBorder(puzzle, a, b);
        return regionBorders ? isBorder : !(isBorder && ShouldDrawRegionBorders);
    }

    private static bool IsBorder(Puzzle puzzle, Cell a, Cell b) => puzzle.RegionAt(a) != puzzle.RegionAt(b);
}
