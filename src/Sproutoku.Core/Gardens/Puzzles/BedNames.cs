namespace Sproutoku.Core.Gardens.Puzzles;

/// <summary>
/// Each bed is a growing medium; its name comes from its bed id. The app's bed colors follow the same order.
/// </summary>
public static class BedNames {
    //private static readonly string[] Names = ["Soil", "Water", "Sand", "Perlite", "Pebble", "Bark", "Moss", "Clay", "Mulch", "Lava"];
    private static readonly string[] Names = ["Brown", "Blue", "Yellow", "White", "Grey", "Orange", "Green", "Red", "Dark Green", "Purple"];

    public static string For(int bed) => Names[bed];
}
