using System.Text;
using Sproutoku.Core.Gardens.Puzzles;

namespace Sproutoku.Core.Gardens.Generation;

/// <summary>
/// Tells gardens apart: a fingerprint for exact copies, and a shared-borders score for look-alikes.
/// </summary>
public static class GardenLikeness {
    /// <summary>
    /// A shared-borders score at or above this means two gardens look alike. Typical pairs score about 0.3.
    /// </summary>
    public const double LookAlikeScore = 0.4;

    /// <summary>
    /// The garden's bed layout with beds renamed a, b, c... in reading order, so a recolored copy has the same fingerprint.
    /// Rotated and mirrored copies read differently, so they count as different gardens.
    /// </summary>
    public static string Fingerprint(Puzzle puzzle) {
        ArgumentNullException.ThrowIfNull(puzzle);

        var names = new Dictionary<int, char>();
        var text = new StringBuilder(puzzle.Size * puzzle.Size);

        for (int row = 0; row < puzzle.Size; row++) {
            for (int column = 0; column < puzzle.Size; column++) {
                int bed = puzzle.RegionAt(new Cell(row, column));
                if (!names.TryGetValue(bed, out char name)) {
                    name = (char)('a' + names.Count);
                    names[bed] = name;
                }

                text.Append(name);
            }
        }

        return text.ToString();
    }

    /// <summary>
    /// True if two gardens look alike, which includes being exact copies.
    /// </summary>
    public static bool LookAlike(Puzzle a, Puzzle b) => SharedBorders(a, b) >= LookAlikeScore;

    public static double SharedBorders(Puzzle a, Puzzle b) {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return SharedBorders(BedsOf(a), BedsOf(b));
    }

    /// <summary>
    /// How much two gardens' bed borders line up, from 0 (not at all) to 1 (exactly).
    /// The smaller garden is lined up with each corner of the larger one, and the best match counts.
    /// The score is the borders both have, out of the borders either has, inside the smaller garden's area.
    /// </summary>
    public static double SharedBorders(int[][] a, int[][] b) {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (a.Length > b.Length) {
            (a, b) = (b, a);
        }

        int small = a.Length;
        var smallBorders = BordersIn(a, top: 0, left: 0, small);

        // Line the smaller garden up with each corner of the bigger one: that's how look-alikes catch the eye.
        // (Sliding it to every position almost always found a match somewhere, especially on much bigger gardens.)
        int far = b.Length - small;
        int[] positions = far == 0 ? [0] : [0, far];

        double best = 0;
        foreach (int top in positions) {
            foreach (int left in positions) {
                var bigBorders = BordersIn(b, top, left, small);
                int shared = smallBorders.Count(bigBorders.Contains);
                int either = smallBorders.Count + bigBorders.Count - shared;

                if (either > 0) {
                    best = Math.Max(best, (double)shared / either);
                }
            }
        }

        return best;
    }

    // The borders between beds inside a size×size window, by their position within the window.
    private static HashSet<(bool Across, int Row, int Column)> BordersIn(int[][] beds, int top, int left, int size) {
        var borders = new HashSet<(bool Across, int Row, int Column)>();

        for (int row = 0; row < size; row++) {
            for (int column = 0; column < size; column++) {
                int bed = beds[top + row][left + column];

                if (column + 1 < size && bed != beds[top + row][left + column + 1]) {
                    borders.Add((true, row, column));   // Between this cell and the one to its right.
                }

                if (row + 1 < size && bed != beds[top + row + 1][left + column]) {
                    borders.Add((false, row, column));  // Between this cell and the one below.
                }
            }
        }

        return borders;
    }

    private static int[][] BedsOf(Puzzle puzzle) => Enumerable.Range(0, puzzle.Size)
        .Select(row => Enumerable.Range(0, puzzle.Size).Select(column => puzzle.RegionAt(new Cell(row, column))).ToArray())
        .ToArray();
}
