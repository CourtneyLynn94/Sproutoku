using Sproutoku.App.Resources.Styles;
using System.Windows.Input;

namespace Sproutoku.App.Pages;

/// <summary>
/// The Garden journal sheet, opened from the garden's Journal button or the back gesture: the garden's facts and where to go next.
/// </summary>
public partial class JournalPage : ContentPage {
    public JournalPage(GardenPageViewModel viewModel) {
        BindingContext = viewModel;
        InitializeComponent();

#if DEBUG
        AddDevOptions(viewModel);
#endif
    }

#if DEBUG
    // Developer options. Each one closes the Journal so its effect shows straight away.
    private void AddDevOptions(GardenPageViewModel viewModel) {
        DevOptions.Children.Add(new Label {
            Text = "Developer",
            FontAttributes = FontAttributes.Bold,
            TextColor = Palette.InkSoft,
        });

        Add("Go to level", viewModel.GoToLevelDevCommand);
        Add("Skip to next garden", viewModel.SkipLevelDevCommand);
        Add("Unlimited tools on or off", viewModel.ToggleUnlimitedToolsDevCommand);
        Add("Show or hide the solution", viewModel.ToggleSolutionDevCommand);

        void Add(string text, ICommand command) => DevOptions.Children.Add(new Button { Text = text, Command = command });
    }
#endif
}
