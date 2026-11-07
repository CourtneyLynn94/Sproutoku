using Sproutoku.Core.Game.Boards;
using Sproutoku.Core.Gardens.Puzzles;
using Sproutoku.Core.Gardens.Solving;

namespace Sproutoku.Core.Game.PowerUps;

/// <summary>
/// One Worm hint: the next step, shown more than told.
/// <see cref="Message"/> is one short line;
/// <see cref="Focus"/> are the cells the step is about (the rest of the board is dimmed);
/// <see cref="Plants"/> and <see cref="RuleOuts"/> are what Apply changes;
/// <see cref="WrongMarks"/> are ✕ marks this step proves wrong.
/// </summary>
public sealed record WormHint(
    LogicStep Step,
    string Message,
    IReadOnlyList<Cell> Focus,
    IReadOnlyList<Cell> Plants,
    IReadOnlyList<Cell> RuleOuts,
    IReadOnlyList<Cell> WrongMarks);

/// <summary>
/// The Worm's thinking. It only uses what the player can see: the bed shapes and the board's facts.
/// It never peeks at the answer and never trusts ✕ marks; a ✕ the next step proves wrong is called out in that hint.
/// </summary>
public static class WormHelper {
    /// <summary>
    /// The next step for this board, or null if the garden is over or there's nothing left to work out.
    /// </summary>
    public static WormHint? HintFor(Board board) {
        ArgumentNullException.ThrowIfNull(board);
        if (board.IsFinished) return null;

        foreach (var step in LogicSolver.Solve(GardenFacts.From(board)).Steps) {
            // What the step would change on the player's board: plants not yet grown, and rule-outs not yet marked.
            var plants = step.Plants.Where(cell => board[cell] != CellState.Planted).ToList();
            var ruleOuts = step.RuledOut.Where(cell => board[cell] == CellState.Empty).ToList();
            if (plants.Count == 0 && ruleOuts.Count == 0) continue;

            // A plant this step proves, sitting under a ✕: that ✕ is a mistake.
            var wrongMarks = plants.Where(cell => board[cell] == CellState.Marked).ToList();

            // The cells the step is about: the area for "only one cell left", otherwise the cells that explain it; plus what changes.
            var why = step.Technique == Technique.OnlyCellLeft
                ? step.Areas.SelectMany(area => CellsOf(board.Puzzle, area))
                : step.Reasons;
            var focus = why.Concat(plants).Concat(ruleOuts).Distinct().ToList();

            return new WormHint(step, Describe(board.Puzzle, step, ruleOuts, wrongMarks), focus, plants, ruleOuts, wrongMarks);
        }

        return null;
    }

    // One short line per kind of step.
    private static string Describe(Puzzle puzzle, LogicStep step, List<Cell> ruleOuts, List<Cell> wrongMarks) {
        var beds = step.Areas.Where(area => area.Kind == AreaKind.Bed).ToList();
        var lines = step.Areas.Where(area => area.Kind != AreaKind.Bed).ToList();

        switch (step.Technique) {
            case Technique.PlantRulesOut:
                return "This plant's row, column, bed and neighbors can't hold another plant. Rule them out.";

            case Technique.OnlyCellLeft: {
                var area = step.Areas[0];
                string message = area.Kind == AreaKind.Bed
                    ? $"{Name(area)} has only one cell left for a plant."
                    : $"Only one cell left in {Name(area)} for a plant.";
                return wrongMarks.Count > 0 ? $"{message} Your ✕ there is a mistake." : message;
            }

            case Technique.BedsFillLines:
                return $"{Beds(beds, "and")} only {(beds.Count == 1 ? "has" : "have")} open cells in {Lines(lines)}. Rule out the other beds there.";

            case Technique.LinesFillBeds:
                return beds.Count == 1
                    ? $"Every open cell in {Lines(lines)} is {Name(beds[0])}. Rule out {Name(beds[0])}'s other cells."
                    : $"Every open cell in {Lines(lines)} is {Beds(beds, "or")}. Rule out their other cells.";

            default: {
                // WouldStarve: the area that would be left with nowhere to grow, and the rules that would do it.
                string area = Name(step.Areas[0]);
                return $"A plant here would block all of {area}'s open cells ({WhyBlocked(puzzle, ruleOuts, step.Reasons)}). Rule these out.";
            }
        }
    }

    // Which rules let a plant in a ruled-out cell block the area's open cells. Only the ones that apply are listed.
    private static string WhyBlocked(Puzzle puzzle, IReadOnlyList<Cell> blockers, IReadOnlyList<Cell> openCells) {
        bool sameBed = false, sameLine = false, touching = false;

        foreach (var blocker in blockers) {
            foreach (var cell in openCells) {
                if (puzzle.RegionAt(cell) == puzzle.RegionAt(blocker)) {
                    sameBed = true;
                } else if (cell.Row == blocker.Row || cell.Column == blocker.Column) {
                    sameLine = true;
                } else {
                    touching = true; // The solver only rules a cell out if one of the three applies, so this one does.
                }
            }
        }

        var rules = new List<string>();
        if (sameBed) rules.Add("same bed");
        if (sameLine) rules.Add("same row or column");
        if (touching) rules.Add("touching");

        return rules.Count == 1 ? rules[0] : $"{string.Join(", ", rules.Take(rules.Count - 1))}, or {rules[^1]}";
    }

    private static IEnumerable<Cell> CellsOf(Puzzle puzzle, Area area) {
        for (int row = 0; row < puzzle.Size; row++) {
            for (int column = 0; column < puzzle.Size; column++) {
                var cell = new Cell(row, column);
                bool inside = area.Kind switch {
                    AreaKind.Row => row == area.Index,
                    AreaKind.Column => column == area.Index,
                    _ => puzzle.RegionAt(cell) == area.Index,
                };

                if (inside) yield return cell;
            }
        }
    }

    // "Sand", "row 3", "column 10". Players count from 1.
    private static string Name(Area area) => area.Kind switch {
        AreaKind.Row => $"row {area.Index + 1}",
        AreaKind.Column => $"column {area.Index + 1}",
        _ => BedNames.For(area.Index),
    };

    // "Sand", "Sand and Moss", "Sand, Moss and Clay" (or "or").
    private static string Beds(List<Area> beds, string joiner) {
        var names = beds.Select(Name).ToList();
        return names.Count == 1 ? names[0] : $"{string.Join(", ", names.Take(names.Count - 1))} {joiner} {names[^1]}";
    }

    // "row 3", "rows 2 and 4", "columns 1, 3 and 5".
    private static string Lines(List<Area> lines) {
        string kind = lines[0].Kind == AreaKind.Row ? "row" : "column";
        var numbers = lines.Select(line => (line.Index + 1).ToString()).ToList();

        return numbers.Count == 1
            ? $"{kind} {numbers[0]}"
            : $"{kind}s {string.Join(", ", numbers.Take(numbers.Count - 1))} and {numbers[^1]}";
    }
}
