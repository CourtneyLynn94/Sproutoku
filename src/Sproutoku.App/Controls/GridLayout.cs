namespace Sproutoku.App.Controls;

/// <summary>
/// Small helpers for laying out a Grid from code, for the phone and desktop layouts.
/// Use with <c>using static Sproutoku.App.Controls.GridLayout;</c>.
/// </summary>
public static class GridLayout {
    public static void Place(BindableObject view, int row, int column) {
        Grid.SetRow(view, row);
        Grid.SetColumn(view, column);
    }

    public static RowDefinitionCollection Rows(params GridLength[] heights) =>
        new(heights.Select(height => new RowDefinition(height)).ToArray());

    public static ColumnDefinitionCollection Columns(params GridLength[] widths) =>
        new(widths.Select(width => new ColumnDefinition(width)).ToArray());
}
