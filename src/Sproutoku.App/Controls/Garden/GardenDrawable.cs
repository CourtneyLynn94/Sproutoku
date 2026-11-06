using Sproutoku.Core.Game.Boards;
using Sproutoku.Core.Game.Input;
using Sproutoku.Core.Gardens.Puzzles;
using Cell = Sproutoku.Core.Gardens.Puzzles.Cell;

namespace Sproutoku.App.Controls.Garden;

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

    private static readonly Color Cream = Color.FromArgb("#FBF7EA");
    private static readonly Color HintScrim = Color.FromArgb("#2E241B").WithAlpha(0.55f);
    private static readonly Color HintOutline = Color.FromArgb("#FFF3B0");
    private static readonly Color HintRing = Color.FromArgb("#2F6F8F");

    // Worked out on each draw; used to turn a point into a cell.
    private PointF _origin;
    private float _cellSize;

    public Board? Board { get; set; }

    /// <summary>
    /// The cell under the mouse pointer (desktop only), or null.
    /// </summary>
    public Cell? HoverCell { get; set; }

    /// <summary>
    /// The keyboard focus cell, or null before the keyboard has been used.
    /// </summary>
    public Cell? FocusCell { get; set; }

    /// <summary>
    /// Whether to draw the focus ring: only while the keyboard is in use.
    /// </summary>
    public bool ShowFocus { get; set; }

    /// <summary>
    /// Optional: Draw region borders.
    /// </summary>
    public bool ShouldDrawRegionBorders { get; set; } = false;

    /// <summary>
    /// Developer view: ring every solution cell.
    /// Drawing only; the board is not changed.
    /// </summary>
    public bool ShowSolution { get; set; }

    /// <summary>
    /// The worm's area to look at: outlined, while the rest of the board is dimmed. Null when there's no hint.
    /// </summary>
    public IReadOnlyCollection<Cell>? HintArea { get; set; }

    /// <summary>
    /// The cells the worm's step plants or rules out: ringed, and never dimmed.
    /// </summary>
    public IReadOnlyCollection<Cell>? HintTargets { get; set; }

    /// <summary>
    /// ✕ marks the worm's step shows are mistakes: ringed in red.
    /// </summary>
    public IReadOnlyCollection<Cell>? HintMistakes { get; set; }

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

                // The worm's focus: everything outside its area and targets is dimmed.
                if (HintArea is not null && !HintArea.Contains(cell) && HintTargets?.Contains(cell) != true) {
                    canvas.FillColor = HintScrim;
                    canvas.FillRectangle(rect);
                }

                if (HintMistakes?.Contains(cell) == true) {
                    DrawHintRing(canvas, rect, Wilt);
                } else if (HintTargets?.Contains(cell) == true) {
                    DrawHintRing(canvas, rect, HintRing);
                }

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

        if (ShowFocus && FocusCell is { } focus && focus.IsInside(size)) {
            DrawFocusRing(canvas, CellRect(focus));
        }

        canvas.StrokeColor = Ink;
        canvas.StrokeSize = 3;
        canvas.DrawRectangle(_origin.X, _origin.Y, side, side);

        if (HintArea is not null) {
            DrawHintOutline(canvas, HintArea);
        }
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

    /// <summary>
    /// The board cells a straight line between two points passes through, in order (S27).
    /// </summary>
    public IReadOnlyList<Cell> CellsAlong(PointF from, PointF to) {
        if (Board is null || _cellSize <= 0) return [];

        return DragGridLine.CellsAlong(
                (from.X - _origin.X) / _cellSize,
                (from.Y - _origin.Y) / _cellSize,
                (to.X - _origin.X) / _cellSize,
                (to.Y - _origin.Y) / _cellSize)
            .Where(cell => cell.IsInside(Board.Size))
            .ToList();
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

    // A dashed ring inside a cell the worm points at.
    private static void DrawHintRing(ICanvas canvas, RectF rect, Color color) {
        canvas.StrokeColor = color;
        canvas.StrokeSize = Math.Max(2, rect.Width * 0.07f);
        canvas.StrokeDashPattern = [4, 3];
        canvas.DrawEllipse(rect.Inflate(-rect.Width * 0.18f, -rect.Height * 0.18f));
        canvas.StrokeDashPattern = null;
    }

    // A thick outline around the worm's area: drawn on every side where the area meets a cell outside it.
    // A dark line under a light one, so it shows on every bed color and on the dimmed cells.
    private void DrawHintOutline(ICanvas canvas, IReadOnlyCollection<Cell> area) {
        var inArea = area.ToHashSet();
        canvas.StrokeLineCap = LineCap.Round;

        foreach (var (color, width) in new[] { (Ink, 8f), (HintOutline, 4f) }) {
            canvas.StrokeColor = color;
            canvas.StrokeSize = width;

            foreach (var cell in inArea) {
                var rect = CellRect(cell);
                if (!inArea.Contains(new Cell(cell.Row - 1, cell.Column))) canvas.DrawLine(rect.Left, rect.Top, rect.Right, rect.Top);
                if (!inArea.Contains(new Cell(cell.Row + 1, cell.Column))) canvas.DrawLine(rect.Left, rect.Bottom, rect.Right, rect.Bottom);
                if (!inArea.Contains(new Cell(cell.Row, cell.Column - 1))) canvas.DrawLine(rect.Left, rect.Top, rect.Left, rect.Bottom);
                if (!inArea.Contains(new Cell(cell.Row, cell.Column + 1))) canvas.DrawLine(rect.Right, rect.Top, rect.Right, rect.Bottom);
            }
        }

        canvas.StrokeLineCap = LineCap.Square;
    }

    // Cell focus is ink inside cream, drawn on top so it shows on every region color.
    private static void DrawFocusRing(ICanvas canvas, RectF rect) {
        canvas.StrokeColor = Cream;
        canvas.StrokeSize = 4;
        canvas.DrawRectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);

        canvas.StrokeColor = Ink;
        canvas.StrokeSize = 2.5f;
        canvas.DrawRectangle(rect.X + 5, rect.Y + 5, rect.Width - 10, rect.Height - 10);
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
