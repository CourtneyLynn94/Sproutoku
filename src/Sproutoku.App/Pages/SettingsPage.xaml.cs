using Sproutoku.App.Services;

namespace Sproutoku.App.Pages;

/// <summary>
/// The Settings sheet, opened from the Garden journal. Changes are saved as soon as a switch flips.
/// </summary>
public partial class SettingsPage : ContentPage {
    private readonly GameSettings _settings;

    public SettingsPage(GameSettings settings) {
        InitializeComponent();
        _settings = settings;

        VibrationSwitch.IsToggled = settings.Vibration;
        BedPatternsSwitch.IsToggled = settings.BedPatterns;
        SoundSwitch.IsToggled = settings.Sound;

        // Desktops and some tablets can't vibrate: show the setting, but say why it's off.
        if (!Vibration.Default.IsSupported) {
            VibrationSwitch.IsEnabled = false;
            VibrationNote.Text = "This device can't vibrate.";
        }

        // Hooked up after the switches show the saved values, so setting them above doesn't count as a change.
        VibrationSwitch.Toggled += (_, e) => _settings.Vibration = e.Value;
        BedPatternsSwitch.Toggled += (_, e) => _settings.BedPatterns = e.Value;
        SoundSwitch.Toggled += (_, e) => _settings.Sound = e.Value;
    }

    private async void OnDoneClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}
