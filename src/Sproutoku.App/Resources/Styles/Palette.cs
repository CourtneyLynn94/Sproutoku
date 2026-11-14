using System.Runtime.CompilerServices;

namespace Sproutoku.App.Resources.Styles;

/// <summary>
/// Sproutoku's colors for code that draws: the garden and the sun meter.
/// The colors themselves live in Colors.xaml, so pages and code share one palette, and a Dusk theme later only changes the resources.
/// </summary>
public static class Palette {
    // The bed colors' resource names, in bed-id order (the same order as BedNames).
    private static readonly string[] BedKeys = [
        "BedSoil", "BedWater", "BedSand", "BedPerlite", "BedPebble",
        "BedBark", "BedMoss", "BedClay", "BedMulch", "BedLava",
    ];

    public static Color Ink => Get();
    public static Color InkSoft => Get();
    public static Color Cream => Get();
    public static Color Leaf => Get();
    public static Color Wilt => Get();
    public static Color Sun => Get();
    public static Color SpentSun => Get();

    /// <summary>
    /// A bed's color, by bed id.
    /// </summary>
    public static Color Bed(int bed) => Get(BedKeys[bed]);

    // Looks a color up by its resource name: the calling property's own name unless one is given.
    // A missing color shows as bright magenta, so a typo is obvious on screen rather than failing quietly.
    private static Color Get([CallerMemberName] string key = "") =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : Colors.Magenta;
}
