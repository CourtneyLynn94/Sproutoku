namespace Sproutoku.App.Pages;

/// <summary>
/// The first screen: the game's name and the way into the garden.
/// </summary>
public partial class HomePage : ContentPage {
    private readonly HomePageViewModel _viewModel;

    public HomePage(HomePageViewModel viewModel) {
        _viewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    // Each time Home shows, it reads where the player is.
    protected override void OnAppearing() {
        base.OnAppearing();
        _viewModel.Appearing();
    }
}
