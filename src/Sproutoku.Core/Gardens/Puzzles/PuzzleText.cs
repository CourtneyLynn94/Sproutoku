namespace Sproutoku.Core.Puzzles;

/// <summary>
/// Reads gardens written as text: one string per row, one letter per cell.
/// The letter is the region (a = region 0, b = region 1, ...), and an uppercase letter
/// marks that region's plant in the solution. Spaces are ignored.
/// </summary>
public static class PuzzleText {
    public static Puzzle Parse(params string[] rows) {
        ArgumentNullException.ThrowIfNull(rows);

        var regions = new int[rows.Length][];
        var plants = new List<(int Region, Cell Cell)>();

        for (int row = 0; row < rows.Length; row++) {
            string letters = rows[row].Replace(" ", string.Empty);
            regions[row] = new int[letters.Length];

            for (int column = 0; column < letters.Length; column++) {
                char letter = letters[column];
                if (!char.IsAsciiLetter(letter)) {
                    throw new FormatException($"Row {row} contains '{letter}'; use only the letters a to j.");
                }

                int region = char.ToLowerInvariant(letter) - 'a';
                regions[row][column] = region;

                if (char.IsUpper(letter)) {
                    plants.Add((region, new Cell(row, column)));
                }
            }
        }

        // Solution[r] must be region r's plant, so sort the plants by region.
        // A missing or extra plant is caught by the validator inside Puzzle.Create.
        var solution = plants.OrderBy(p => p.Region).Select(p => p.Cell).ToArray();
        return Puzzle.Create(regions, solution);
    }
}
