using System.Diagnostics;

namespace Sproutoku.App.Services;

/// <summary>
/// The player's settings, saved on the device in the default Preferences container,
/// so Reset all progress (which clears only the progress container) never touches them.
/// Read once at startup and kept in memory; every change is saved straight away and announced through <see cref="Changed"/>.
/// </summary>
public sealed class GameSettings {
    private const string VibrationKey = "settings.vibration";
    private const string BedPatternsKey = "settings.bedPatterns";
    private const string SoundKey = "settings.sound";
    private const string LightKey = "settings.light";

    private bool _vibration = Load(VibrationKey, true);
    private bool _bedPatterns = Load(BedPatternsKey, false);
    private bool _sound = Load(SoundKey, true);
    private AppTheme _light = (AppTheme)Load(LightKey, (int)AppTheme.Unspecified);

    /// <summary>
    /// Raised after any setting changes, so the page can redraw or update what depends on it.
    /// </summary>
    public event EventHandler? Changed;

    public bool Vibration {
        get => _vibration;
        set => Save(ref _vibration, value, VibrationKey);
    }

    /// <summary>
    /// Adds a pattern to each bed, so beds can be told apart without color.
    /// </summary>
    public bool BedPatterns {
        get => _bedPatterns;
        set => Save(ref _bedPatterns, value, BedPatternsKey);
    }

    /// <summary>
    /// Sound effects.
    /// </summary>
    public bool Sound {
        get => _sound;
        set => Save(ref _sound, value, SoundKey);
    }

    /// <summary>
    /// Morning (Light), Dusk (Dark), or Unspecified to match the device.
    /// Applied through Application.UserAppTheme once the Dusk colors exist, so AppThemeBinding picks them.
    /// </summary>
    public AppTheme Light {
        get => _light;
        set {
            if (_light == value) return;
            _light = value;
            Store(LightKey, (int)value);
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Save(ref bool field, bool value, string key) {
        if (field == value) return;

        field = value;
        Store(key, value);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    // Storage problems never stop the game: a setting just isn't remembered.
    private static T Load<T>(string key, T fallback) {
        try {
            return Preferences.Default.Get(key, fallback);
        }
        catch (Exception ex) {
            Debug.WriteLine($"Couldn't read setting {key}: {ex.Message}");
            return fallback;
        }
    }

    private static void Store<T>(string key, T value) {
        try {
            Preferences.Default.Set(key, value);
        }
        catch (Exception ex) {
            Debug.WriteLine($"Couldn't save setting {key}: {ex.Message}");
        }
    }
}
