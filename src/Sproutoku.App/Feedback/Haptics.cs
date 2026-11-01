using System.Diagnostics;
using Microsoft.Maui.Devices;

namespace Sproutoku.App.Feedback;

/// <summary>
/// Vibrations for marks, plants, mistakes and the end of a garden, each with its own rhythm.
/// Does nothing on devices without vibration (like desktops), and never stops the game if it fails.
/// </summary>
public sealed class Haptics {
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(20);
    private static readonly TimeSpan Pulse = TimeSpan.FromMilliseconds(70);
    private static readonly TimeSpan PulseGap = TimeSpan.FromMilliseconds(110);
    private static readonly TimeSpan WiltBuzz = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan GardenEndBuzz = TimeSpan.FromMilliseconds(700);

    /// <summary>
    /// The Settings screen (S54) can turn haptics off.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// One short tick: a ✕ placed or cleared, including each cell a drag changes.
    /// </summary>
    public void Mark() => Vibrate(Tick);

    /// <summary>
    /// Two pulses: a plant grew.
    /// </summary>
    public async Task GrewAsync() {
        Vibrate(Pulse);
        await Task.Delay(Pulse + PulseGap);
        Vibrate(Pulse);
    }

    /// <summary>
    /// One long buzz: a wrong plant wilted and a sun was spent.
    /// </summary>
    public void Wilted() => Vibrate(WiltBuzz);

    /// <summary>
    /// A longer buzz as the end screen appears: garden complete, or out of sun.
    /// </summary>
    public void GardenEnded() => Vibrate(GardenEndBuzz);

    private void Vibrate(TimeSpan duration) {
        if (!IsEnabled) return;

        try {
            if (Vibration.Default.IsSupported) {
                Vibration.Default.Vibrate(duration);
            }
        }
        catch (Exception ex) {
            Debug.WriteLine($"Vibration failed: {ex.Message}");
        }
    }
}
