using System.Runtime.Versioning;
using Android.Content;
using Android.OS;
using Primitive = Android.OS.VibrationEffectCompositionPrimitive;

namespace Sproutoku.App.Services;

// Android's half of Haptics: each feel as the crispest effect this phone can play.
// Android 11+ with haptic primitives: composed clicks, ticks and ramps (the sharpest).
// Android 10+: predefined effects (tick, click, double click, heavy click).
// Android 8+: hand-made waveforms with strength levels; older phones get plain on/off timings.
public sealed partial class Haptics {
    private Vibrator? _vibrator;
    private bool _vibratorLookedUp;

    partial void PlayOnDevice(HapticFeel feel) {
        var vibrator = DeviceVibrator();
        if (vibrator is null || !vibrator.HasVibrator) return;

        if (OperatingSystem.IsAndroidVersionAtLeast(33)) {
            // Played as touch feedback, so it follows the phone's touch vibration strength, like the keyboard and other apps' taps.
            vibrator.Vibrate(Effect(vibrator, feel), VibrationAttributes.CreateForUsage((int)VibrationAttributesUsageType.Touch));
        } else if (OperatingSystem.IsAndroidVersionAtLeast(26)) {
            vibrator.Vibrate(Effect(vibrator, feel));
        } else {
            var (timings, strengths) = Waveform(feel);
            vibrator.Vibrate(OnOff(timings, strengths), -1);
        }
    }

    // The crispest effect this phone can play for the feel: composed, then predefined, then the hand-made waveform.
    [SupportedOSPlatform("android26.0")]
    private static VibrationEffect? Effect(Vibrator vibrator, HapticFeel feel) {
        if (OperatingSystem.IsAndroidVersionAtLeast(30) && Composed(vibrator, feel) is { } composed) {
            return composed;
        }
        if (OperatingSystem.IsAndroidVersionAtLeast(29) && Predefined(feel) is { } predefined) {
            return predefined;
        }

        var (timings, strengths) = Waveform(feel);
        return VibrationEffect.CreateWaveform(timings, strengths, -1);
    }

    // Found once: Android 12+ goes through the vibrator manager.
    private Vibrator? DeviceVibrator() {
        if (_vibratorLookedUp) return _vibrator;
        _vibratorLookedUp = true;

        var context = Android.App.Application.Context;
        if (OperatingSystem.IsAndroidVersionAtLeast(31)) {
            _vibrator = (context.GetSystemService(Context.VibratorManagerService) as VibratorManager)?.DefaultVibrator;
        } else {
            _vibrator = context.GetSystemService(Context.VibratorService) as Vibrator;
        }

        if (_vibrator is not null) {
            bool primitives = OperatingSystem.IsAndroidVersionAtLeast(30)
                && _vibrator.AreAllPrimitivesSupported([(int)Primitive.Click, (int)Primitive.QuickFall, (int)Primitive.SlowRise]);
            bool strength = OperatingSystem.IsAndroidVersionAtLeast(26) && _vibrator.HasAmplitudeControl;
            System.Diagnostics.Debug.WriteLine($"Haptics: Android API {(int)Build.VERSION.SdkInt}, composed effects {primitives}, strength control {strength}");
        }

        return _vibrator;
    }

    // Short primitives the haptic engine plays exactly. Null if this phone can't play every primitive the feel needs.
    // Each step is (primitive, strength 0–1, wait in ms after the step before it).
    [SupportedOSPlatform("android30.0")]
    private static VibrationEffect? Composed(Vibrator vibrator, HapticFeel feel) {
        (Primitive Kind, float Strength, int Wait)[] steps = feel switch {
            HapticFeel.Tick => [(Primitive.Click, 0.8f, 0)],
            HapticFeel.Grew => [(Primitive.Click, 0.8f, 0), (Primitive.Click, 1.0f, 60)],
            HapticFeel.Wilted => [(Primitive.Click, 1.0f, 0), (Primitive.QuickFall, 1.0f, 20)],
            HapticFeel.GardenComplete => [(Primitive.SlowRise, 0.8f, 0), (Primitive.Click, 1.0f, 0), (Primitive.Click, 1.0f, 80)],
            _ => [(Primitive.QuickFall, 1.0f, 0), (Primitive.QuickFall, 0.8f, 120)], // OutOfSun
        };

        int[] kinds = steps.Select(step => (int)step.Kind).Distinct().ToArray();
        if (!vibrator.AreAllPrimitivesSupported(kinds)) {
            return null;
        }

        var composition = VibrationEffect.StartComposition();
        foreach (var (kind, strength, wait) in steps) {
            composition.AddPrimitive((int)kind, strength, wait);
        }

        return composition.Compose();
    }

    // The phone's own tuned effects. The end-of-garden feels have no match here, so they use the waveform.
    [SupportedOSPlatform("android29.0")]
    private static VibrationEffect? Predefined(HapticFeel feel) => feel switch {
        HapticFeel.Tick => VibrationEffect.CreatePredefined(VibrationEffect.EffectClick),
        HapticFeel.Grew => VibrationEffect.CreatePredefined(VibrationEffect.EffectDoubleClick),
        HapticFeel.Wilted => VibrationEffect.CreatePredefined(VibrationEffect.EffectHeavyClick),
        _ => null,
    };

    // Hand-made: how long each part lasts (ms) and how strong it is (0 = off, 255 = full).
    // Phones without strength control play every non-zero part at full strength.
    private static (long[] Timings, int[] Strengths) Waveform(HapticFeel feel) => feel switch {
        HapticFeel.Tick => ([20], [220]),
        HapticFeel.Grew => ([35, 60, 40], [220, 0, 255]),
        HapticFeel.Wilted => ([50, 30, 110], [255, 0, 180]),
        HapticFeel.GardenComplete => ([50, 50, 50, 70, 40], [120, 180, 240, 0, 255]),
        _ => ([90, 110, 140], [255, 0, 200]), // OutOfSun
    };

    // Before Android 8: no strength control, so a waveform becomes plain off/on timings (starting with "off").
    private static long[] OnOff(long[] timings, int[] strengths) {
        var pattern = new List<long> { 0 };
        bool on = false;

        for (int i = 0; i < timings.Length; i++) {
            bool partOn = strengths[i] > 0;
            if (partOn == on) {
                pattern[^1] += timings[i];
            } else {
                pattern.Add(timings[i]);
                on = partOn;
            }
        }

        return [.. pattern];
    }
}
