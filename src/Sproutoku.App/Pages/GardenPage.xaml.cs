using static Sproutoku.App.Controls.GridLayout;

namespace Sproutoku.App.Pages;

/// <summary>
/// The garden screen: lays out the sky band, the garden, the planting list or worm's note, and the tool shelf, for a phone or a desktop.
/// Each part is its own control, and everything about the game lives in <see cref="GardenPageViewModel"/>.
/// </summary>
public partial class GardenPage : ContentPage {
    public GardenPage(GardenPageViewModel viewModel) {
        BindingContext = viewModel;
        InitializeComponent();

        ApplyLayout(wide: false);
        SizeChanged += (_, _) => ApplyLayout(Width >= WideLayoutMinWidth);

        _ = viewModel.LoadAsync();
    }

    // Android's back gesture opens the Journal instead of leaving the garden; Back to home is in the Journal.
    protected override bool OnBackButtonPressed() {
        var journal = ((GardenPageViewModel)BindingContext).OpenJournalCommand;
        if (journal.CanExecute(null)) {
            journal.Execute(null);
        }

        return true;
    }

    #region Layout
    // At this width and up, the desktop layout puts the list, board and shelf side by side.
    private const double WideLayoutMinWidth = 960;
    private bool? _isWide;

    // Phone: stacked vertical with board, planting list, tool shelf.
    // Desktop: stacked horizontal with planting list, board, tool shelf.
    private void ApplyLayout(bool wide) {
        if (_isWide == wide) return;
        _isWide = wide;

        if (wide) {
            Garden.Margin = new Thickness(0);
            GameArea.RowDefinitions = Rows(GridLength.Star);
            GameArea.ColumnDefinitions = Columns(new GridLength(220), GridLength.Star, new GridLength(220));
            Place(PlantingList, row: 0, column: 0);
            Place(WormNote, row: 0, column: 0);
            Place(BoardArea, row: 0, column: 1);
            Place(ToolShelf, row: 0, column: 2);
        } else {
            // The garden reaches into the page's side padding. Only the labels sit there; the board keeps its distance from the screen's edge.
            Garden.Margin = new Thickness(-16, 0);
            GameArea.RowDefinitions = Rows(GridLength.Star, GridLength.Auto, GridLength.Auto);
            GameArea.ColumnDefinitions = Columns(GridLength.Star);
            Place(BoardArea, row: 0, column: 0);
            Place(PlantingList, row: 1, column: 0);
            Place(WormNote, row: 1, column: 0);
            Place(ToolShelf, row: 2, column: 0);
        }

        ToolShelf.Arrange(vertical: wide);
    }
    #endregion
}
