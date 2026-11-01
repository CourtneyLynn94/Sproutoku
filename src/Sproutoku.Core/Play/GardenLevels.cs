using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Play;

/// <summary>
/// Which garden each level plays.
/// For now the starter gardens in order; generated gardens take over after them in S43.
/// </summary>
public static class GardenLevels {
    /// <summary>
    /// The garden for a level (1 is the first), or null if there isn't one yet.
    /// </summary>
    public static Puzzle? ForLevel(int level) =>
        level >= 1 && level <= StarterGardens.All.Count ? StarterGardens.All[level - 1] : null;
}
