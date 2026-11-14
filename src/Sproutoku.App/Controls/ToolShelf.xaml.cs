using static Sproutoku.App.Controls.GridLayout;

namespace Sproutoku.App.Controls;

/// <summary>
/// The tool shelf: Seedling, Worm and Water with their counts, a divider, then the Rake.
/// </summary>
public partial class ToolShelf : ContentView {
    public ToolShelf() {
        InitializeComponent();
    }

    /// <summary>
    /// Phone: the tools share one row, so they shrink to fit a 360px screen. Desktop: they stack in one column.
    /// </summary>
    public void Arrange(bool vertical) {
        View[] tools = [SeedlingButton, WormButton, WaterButton, Divider, RakeButton];

        if (vertical) {
            Shelf.ColumnDefinitions = Columns(GridLength.Star);
            Shelf.RowDefinitions = Rows(GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto);
        } else {
            Shelf.ColumnDefinitions = Columns(GridLength.Star, GridLength.Star, GridLength.Star, GridLength.Auto, GridLength.Star);
            Shelf.RowDefinitions = Rows(GridLength.Auto);
        }

        for (int i = 0; i < tools.Length; i++) {
            Place(tools[i], row: vertical ? i : 0, column: vertical ? 0 : i);
        }

        // The divider is a thin vertical line in a row, a thin horizontal line in a column. -1 means "not set".
        Divider.WidthRequest = vertical ? -1 : 2;
        Divider.HeightRequest = vertical ? 2 : -1;
    }
}
