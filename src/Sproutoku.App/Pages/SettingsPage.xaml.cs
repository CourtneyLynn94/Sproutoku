using Sproutoku.App.Services;

namespace Sproutoku.App.Pages;

/// <summary>
/// The Settings sheet, opened from the Garden journal. Changes are saved as soon as a switch flips.
/// </summary>
public partial class SettingsPage : ContentPage {
    private readonly GameSettings _settings;

    /// <summary>
    /// Shows the saved settings and saves each change as soon as a switch flips.
    /// </summary>
    public SettingsPage(GameSettings settings) {
        InitializeComponent();
        _settings = settings;

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
}
