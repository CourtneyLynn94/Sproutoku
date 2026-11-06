namespace Sproutoku.Core.Puzzles;

/// <summary>
/// Hand-made gardens to play before the generator exists, easiest first. 
/// Each was checked to have exactly one solution that can be reached by logic alone.
/// Uppercase letters mark the solution.
/// </summary>
public static class StarterGardens {
    public static IReadOnlyList<Puzzle> All { get; } =
    [
        // 5×5
        PuzzleText.Parse(
            "c c c D d",
            "c C c d d",
            "c c c e B",
            "A a a e b",
            "a a E e b"),

        // 6×6, with a 1×1 region (f, bottom left)
        PuzzleText.Parse(
            "e e d D d d",
            "e e d d b B",
            "e E d d b b",
            "e a a a A b",
            "c c C a a b",
            "F c c c c b"),

        // 7×7
        PuzzleText.Parse(
            "f f f b b A a",
            "f f F f b e e",
            "g c c c b e E",
            "g c c d B d d",
            "g C c d d d d",
            "g g d D d d d",
            "G g g g g g g"),
    ];
}
