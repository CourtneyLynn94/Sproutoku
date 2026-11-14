using Sproutoku.App.Services;

namespace Sproutoku.App.Pages;

/// <summary>
/// The Settings sheet, opened from Home or the Garden journal. Changes are saved as soon as a switch flips.
/// </summary>
public partial class SettingsPage : ContentPage {
    private readonly GameSettings _settings;
    private readonly GardenPageViewModel _garden;

    /// <summary>
    /// Shows the saved settings and saves each change as soon as a switch flips.
    /// </summary>
    public SettingsPage(GameSettings settings, GardenPageViewModel garden) {
        InitializeComponent();
        _settings = settings;
        _garden = garden;

        VibrationSwitch.IsToggled = settings.Vibration;
        BedPatternsSwitch.IsToggled = settings.BedPatterns;
        SoundSwitch.IsToggled = settings.Sound;

        // Desktops can't vibrate, so the setting isn't shown there at all.
        VibrationRow.IsVisible = Vibration.Default.IsSupported;

        // Hooked up after the switches show the saved values, so setting them above doesn't count as a change.
        VibrationSwitch.Toggled += (_, e) => _settings.Vibration = e.Value;
        BedPatternsSwitch.Toggled += (_, e) => _settings.BedPatterns = e.Value;
        SoundSwitch.Toggled += (_, e) => _settings.Sound = e.Value;

        string raw = AppInfo.Current.VersionString;
        VersionLabel.Text = Version.TryParse(raw, out var version) && version.Build >= 0
            ? $"Sproutoku {version.ToString(3)}"
            : $"Sproutoku {raw}";
    }

    private async void OnDoneClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    // Asks first. After a reset, Settings closes, so the player sees where they now are (Garden 1, or Home).
    private async void OnResetClicked(object? sender, EventArgs e) {
        if (await _garden.ResetProgressAsync()) {
            await Shell.Current.GoToAsync("..");
        }
    }
}
