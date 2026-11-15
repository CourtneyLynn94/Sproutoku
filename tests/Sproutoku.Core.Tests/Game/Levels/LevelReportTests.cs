using System.Diagnostics;
using Sproutoku.Core.Game.Levels;
using Xunit.Abstractions;

namespace Sproutoku.Core.Tests.Game.Levels;

/// <summary>
/// Not a check: prints how levels really turn out (grades, one-cell beds, sizes, attempts and time), for tuning the level plan.
/// Slow, so CI skips it. Run it by hand:
/// dotnet test tests/Sproutoku.Core.Tests --filter "FullyQualifiedName~LevelReportTests" --logger "console;verbosity=detailed"
/// </summary>
public class LevelReportTests(ITestOutputHelper output) {
    private const int LastLevel = 300;

    private static readonly (string Name, int From, int To)[] Bands = [
        ("Levels 1–20", 1, 20),
        ("Levels 21–50", 21, 50),
        ("Levels 51–150", 51, 150),
        ("Levels 151–300", 151, LastLevel),
    ];

    [Fact]
    [Trait("Speed", "Slow")]
    public void PrintLevelReport() {
        var levels = new List<Entry>();
        for (int level = 1; level <= LastLevel; level++) {
            var watch = Stopwatch.StartNew();
            var garden = GardenLevels.ForLevel(level);
            levels.Add(new Entry(garden, watch.Elapsed));
        }

        foreach (var (name, from, to) in Bands) {
            var band = levels.Where(entry => entry.Garden.Level >= from && entry.Garden.Level <= to).ToList();
            var regular = band.Where(entry => entry.Garden.Recipe.Tier == GardenTier.Normal).ToList();
            var time = TimeSpan.FromTicks(band.Sum(entry => entry.Time.Ticks));

            output.WriteLine($"{name}: {band.Count} levels, {regular.Count} regular");
            output.WriteLine($"  Grades (regular): {Counts(regular, entry => entry.Garden.Garden.Grade)}");
            output.WriteLine($"  One-cell beds (regular): {Counts(regular, entry => OneCellLabel(entry.Garden))}");
            output.WriteLine($"  Sizes (regular): {Counts(regular, entry => entry.Garden.Puzzle.Size)}");
            output.WriteLine($"  One sun: {band.Count(entry => entry.Garden.Recipe.IsOneSun)}");
            output.WriteLine($"  Attempts: average {band.Average(entry => entry.Garden.Garden.Attempts):0}, most {band.Max(entry => entry.Garden.Garden.Attempts)}");
            output.WriteLine($"  Time: {time.TotalSeconds:0.0} s in all, slowest level {band.Max(entry => entry.Time).TotalSeconds:0.00} s");
        }

        // A level outside its recipe fell back to the last resort (any grade), which should never happen.
        var offRecipe = levels.Where(entry => !FollowsRecipe(entry.Garden)).Select(entry => entry.Garden.Level).ToList();
        output.WriteLine($"Off recipe: {(offRecipe.Count == 0 ? "none" : string.Join(", ", offRecipe))}");

        output.WriteLine("Slowest 10 levels:");
        foreach (var entry in levels.OrderByDescending(entry => entry.Time).Take(10)) {
            var recipe = entry.Garden.Recipe;
            output.WriteLine($"  {entry.Garden.Level}: {entry.Time.TotalSeconds:0.00} s, {recipe.Size}×{recipe.Size} {recipe.Easiest}–{recipe.Hardest} {recipe.Tier}, {entry.Garden.Garden.Attempts} attempts");
        }
    }

    private static bool FollowsRecipe(LevelGarden garden) =>
        garden.Garden.Grade >= garden.Recipe.Easiest
        && garden.Garden.Grade <= garden.Recipe.Hardest
        && garden.Puzzle.OneCellBeds() <= garden.Recipe.MaxOneCellBeds;

    private static string OneCellLabel(LevelGarden garden) {
        int count = garden.Puzzle.OneCellBeds();
        return count >= 3 ? "3+" : count.ToString();
    }

    private static string Counts<T>(List<Entry> entries, Func<Entry, T> key) =>
        string.Join(", ", entries.GroupBy(key).OrderBy(group => group.Key).Select(group => $"{group.Key}: {group.Count()}"));

    private sealed record Entry(LevelGarden Garden, TimeSpan Time);
}
