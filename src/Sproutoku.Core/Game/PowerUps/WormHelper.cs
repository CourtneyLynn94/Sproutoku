using Sproutoku.Core.Game.Boards;
using Sproutoku.Core.Gardens.Puzzles;
using Sproutoku.Core.Gardens.Solving;

namespace Sproutoku.Core.Game.PowerUps;

/// <summary>
/// One Worm hint: the next step, all at once.
/// <see cref="Message"/> says where to look and why;
/// <see cref="Area"/> is outlined;
/// <see cref="Targets"/> are the cells the step plants or rules out;
/// <see cref="WrongMarks"/> are ✕ marks this step shows are mistakes.
/// </summary>
public sealed record WormHint(LogicStep Step,
                              string Message,
                              IReadOnlyList<Cell> Area,
                              IReadOnlyList<Cell> Targets,
                              IReadOnlyList<Cell> WrongMarks);
/// <summary>
/// The Worm's thinking. It only uses what the player can see: the bed shapes and the board's facts.
/// It never peeks at the answer and never trusts ✕ marks; a ✕ that the next step proves wrong is called out in that hint.
/// </summary>
public static class WormHelper {
    /// <summary>
    /// The next step for this board, or null if the garden is over or there's nothing left to work out.
    /// </summary>
    public static WormHint? HintFor(Board board) {
        ArgumentNullException.ThrowIfNull(board);
        if (board.IsFinished) return null;

        foreach (var step in LogicSolver.Solve(GardenFacts.From(board)).Steps) {
            var targets = NewTargets(board, step);
            if (targets.Count == 0) continue;

            // A plant this step proves, sitting under a ✕: that ✕ is a mistake.
            var wrongMarks = step.Plants.Where(cell => board[cell] == CellState.Marked).ToList();

            var area = step.Technique == Technique.PlantRulesOut
                ? step.Reasons.ToList() // The plant itself.
                : step.Areas.SelectMany(each => CellsOf(board.Puzzle, each)).Distinct().ToList();

            return new WormHint(step, Describe(step, wrongMarks), area, targets, wrongMarks);
        }

        return null;
    }

    // Cells the step would change on the player's board: plants not yet grown, and rule-outs not yet marked.
    private static List<Cell> NewTargets(Board board, LogicStep step) =>
        step.Plants.Where(cell => board[cell] != CellState.Planted)
            .Concat(step.RuledOut.Where(cell => board[cell] == CellState.Empty))
            .ToList();

    // What the worm says: where to look, then why, in one go.
    private static string Describe(LogicStep step, List<Cell> wrongMarks) {
        const string MarkThem = "mark the circled cells with ✕.";

        var beds = step.Areas.Where(area => area.Kind == AreaKind.Bed).ToList();
        var lines = step.Areas.Where(area => area.Kind != AreaKind.Bed).ToList();

        switch (step.Technique) {
            case Technique.PlantRulesOut: {
                string plant = Place(step.Reasons[0]);
                return $"Look around your plant at {plant}. It's the only plant its row, its column and its bed can have, and plants never touch. "
                    + $"So nothing else can grow in its row, column or bed, or in the cells around it: {MarkThem}";
            }

            case Technique.OnlyCellLeft: {
                string area = Name(step.Areas[0]);
                string message = $"Look at {area}. It has only one cell left where a plant could grow, so its plant must grow in the circled cell.";
                return wrongMarks.Count > 0
                    ? $"{message} Your ✕ there is a mistake: plant there instead."
                    : message;
            }

            case Technique.BedsFillLines when beds.Count == 1: {
                string where = Lines(lines);
                return $"Look at this bed and {where}. Every open cell in this bed is in {where}, so its plant will grow there, "
                    + $"and that's the only plant {where} can have. Nothing from another bed can grow in {where}: {MarkThem}";
            }

            case Technique.BedsFillLines: {
                string where = Lines(lines);
                return $"Look at these {beds.Count} beds and {where}. They only have open cells in {where}, so their {beds.Count} plants fill {where}. "
                    + $"Nothing from any other bed can grow there: {MarkThem}";
            }

            case Technique.LinesFillBeds when beds.Count == 1: {
                string where = Lines(lines);
                return $"Look at {where}. Every open cell in {where} is in this bed, so {where}'s plant is this bed's plant. "
                    + $"Nothing in this bed can grow outside {where}: {MarkThem}";
            }

            case Technique.LinesFillBeds: {
                string where = Lines(lines);
                return $"Look at {where}. Every open cell in {where} is in these {beds.Count} beds, so the plants of {where} are these beds' plants. "
                    + $"Nothing in these beds can grow outside {where}: {MarkThem}";
            }

            default: {
                // WouldStarve: the area that would be left with nowhere to grow.
                string area = Name(step.Areas[0]);
                return $"Look at {area}. A plant in any circled cell would rule out every open cell left in {area}, leaving it nowhere to grow. "
                    + "So none of those cells can hold a plant: mark them with ✕.";
            }
        }
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

    // Players count from 1.
    private static string Place(Cell cell) => $"row {cell.Row + 1}, column {cell.Column + 1}";

    private static string Name(Area area) => area.Kind switch {
        AreaKind.Row => $"row {area.Index + 1}",
        AreaKind.Column => $"column {area.Index + 1}",
        _ => "this bed",
    };

    // "row 3", "rows 2 and 4", "columns 1, 3 and 5".
    private static string Lines(List<Area> lines) {
        string kind = lines[0].Kind == AreaKind.Row ? "row" : "column";
        var numbers = lines.Select(line => (line.Index + 1).ToString()).ToList();

        return numbers.Count == 1
            ? $"{kind} {numbers[0]}"
            : $"{kind}s {string.Join(", ", numbers.Take(numbers.Count - 1))} and {numbers[^1]}";
    }
}
