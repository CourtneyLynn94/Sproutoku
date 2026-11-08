using System.Diagnostics;

namespace Sproutoku.App.Services;

/// <summary>
/// How each moment feels in the hand. Each platform plays them its own way.
/// </summary>
public enum HapticFeel {
    /// <summary>
    /// A ✕ placed or cleared, including each cell a drag changes: one crisp click.
    /// </summary>
    Tick,

    /// <summary>
    /// A plant grew: two quick pops, the second stronger.
    /// </summary>
    Grew,

    /// <summary>
    /// A wrong plant wilted and a sun was spent: one firm knock that falls away.
    /// </summary>
    Wilted,

    /// <summary>
    /// The end screen for a finished garden: a short swell ending in two pops.
    /// </summary>
    GardenComplete,

    /// <summary>
    /// The end screen for running out of sun: two heavy knocks, the second softer.
    /// </summary>
    OutOfSun,
}

/// <summary>
/// Haptic feedback for marks, plants, mistakes and the end of a garden, each with its own feel.
/// Plays nothing when Vibration is off in Settings or the device can't vibrate (like desktops), and never stops the game if it fails.
/// Android plays the feels in Platforms/Android/Haptics.Android.cs. Other platforms play nothing yet;
/// iOS will map them to its own light, medium and heavy taps.
/// </summary>
public sealed partial class Haptics {
    // A drag can change cells faster than a motor can tick cleanly, so ticks closer together than this are skipped.
    private const long MinTickGapMs = 35;

    private readonly GameSettings _settings;
    private long _lastTickMs;

    public Haptics(GameSettings settings) {
        _settings = settings;
    }

    /// <summary>
    /// A ✕ placed or cleared, including each cell a drag changes.
    /// </summary>
    public void Mark() {
        long now = Environment.TickCount64;
        if (now - _lastTickMs < MinTickGapMs) return;

        _lastTickMs = now;
        Play(HapticFeel.Tick);
    }

    /// <summary>
    /// A plant grew.
    /// </summary>
    public void Grew() => Play(HapticFeel.Grew);

    /// <summary>
    /// A wrong plant wilted and a sun was spent.
    /// </summary>
    public void Wilted() => Play(HapticFeel.Wilted);

    /// <summary>
    /// The end screen for a finished garden is appearing.
    /// </summary>
    public void GardenComplete() => Play(HapticFeel.GardenComplete);

    /// <summary>
    /// The end screen for running out of sun is appearing.
    /// </summary>
    public void OutOfSun() => Play(HapticFeel.OutOfSun);

    private void Play(HapticFeel feel) {
        if (!_settings.Vibration) return;

        try {
            PlayOnDevice(feel);
        }
        catch (Exception ex) {
            Debug.WriteLine($"Haptics failed: {ex.Message}");
        }
    }

    // Plays one feel on this device. Only Android has a body so far; on other platforms the compiler drops the call.
    partial void PlayOnDevice(HapticFeel feel);
}
