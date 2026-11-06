using Sproutoku.Core.Game.Boards;
using Sproutoku.Core.Gardens.Puzzles;
using Sproutoku.Core.Gardens.Solving;

namespace Sproutoku.Core.Game.PowerUps;

public enum WormHintKind {
    /// <summary>
    /// A ✕ sits where logic proves a plant must grow. It's pointed out before anything else.
    /// </summary>
    WrongMark,

    /// <summary>
    /// The next deduction the player can make from what's on the board.
    /// </summary>
    NextStep,
}

/// <summary>
/// One Worm hint.
/// First it shows where to look: <see cref="LookHere"/> and the highlighted <see cref="Area"/>.
/// Asked why, it explains: <see cref="Explanation"/> and the <see cref="Targets"/> the step plants or rules out.
/// <see cref="Step"/> is the logic step behind it.
/// </summary>
public sealed record WormHint(
    WormHintKind Kind,
    LogicStep Step,
    string LookHere,
    string Explanation,
    IReadOnlyList<Cell> Area,
    IReadOnlyList<Cell> Targets);

/// <summary>
/// The Worm's thinking. It only uses what the player can see: the bed shapes and the board's facts.
/// It never peeks at the answer, and it checks the player's ✕ marks rather than trusting them.
/// </summary>
public static class WormHelper {
    /// <summary>
    /// The hint for this board, or null if the garden is over or there's nothing left to work out.
    /// </summary>
    public static WormHint? HintFor(Board board) {
        ArgumentNullException.ThrowIfNull(board);
        if (board.IsFinished) return null;

        var steps = LogicSolver.Solve(GardenFacts.From(board)).Steps;

        // A ✕ where logic proves a plant must grow comes first: every later hint would build on it.
        foreach (var step in steps) {
            foreach (var plant in step.Plants) {
                if (board[plant] == CellState.Marked) {
                    return WrongMarkHint(step, plant);
                }
            }
        }

        // Otherwise, the first step that shows the player something new.
        foreach (var step in steps) {
            var targets = NewTargets(board, step);
            if (targets.Count > 0) {
                return NextStepHint(board.Puzzle, step, targets);
            }
        }

        return null;
    }

    // Cells the step would change on the player's board: plants not yet grown, and rule-outs not yet marked.
    private static List<Cell> NewTargets(Board board, LogicStep step) =>
        step.Plants.Where(cell => board[cell] != CellState.Planted)
            .Concat(step.RuledOut.Where(cell => board[cell] == CellState.Empty))
            .ToList();

    private static WormHint WrongMarkHint(LogicStep step, Cell mark) => new(
        WormHintKind.WrongMark,
        step,
        LookHere: $"One of your ✕ marks is in the way, at {Place(mark)}.",
        Explanation: $"A plant has to grow at {Place(mark)}, so that ✕ is wrong. Take it off, then ask me again for the next step.",
        Area: [mark],
        Targets: [mark]);

    private static WormHint NextStepHint(Puzzle puzzle, LogicStep step, List<Cell> targets) {
        var area = step.Technique == Technique.PlantRulesOut
            ? step.Reasons.ToList() // The plant itself.
            : step.Areas.SelectMany(each => CellsOf(puzzle, each)).Distinct().ToList();

        var (lookHere, explanation) = Describe(step);
        return new WormHint(WormHintKind.NextStep, step, lookHere, explanation, area, targets);
    }

    // What the worm says for each kind of step: where to look, then why.
    private static (string LookHere, string Explanation) Describe(LogicStep step) {
        const string MarkThem = "Mark the ringed cells with ✕.";

        var beds = step.Areas.Where(area => area.Kind == AreaKind.Bed).ToList();
        var lines = step.Areas.Where(area => area.Kind != AreaKind.Bed).ToList();

        switch (step.Technique) {
            case Technique.PlantRulesOut: {
                string plant = Place(step.Reasons[0]);
                return (
                    $"Look around your plant at {plant}.",
                    $"Your plant at {plant} is the only plant its row, its column and its bed can have, and plants never touch. "
                    + $"So nothing else can grow in its row, column or bed, or in the cells around it. {MarkThem}");
            }

            case Technique.OnlyCellLeft: {
                string area = Name(step.Areas[0]);
                return (
                    $"Look at {area}.",
                    $"{Capitalized(area)} has only one cell left where a plant could grow, so its plant must grow there. Plant it in the ringed cell.");
            }

            case Technique.BedsFillLines when beds.Count == 1: {
                string where = Lines(lines);
                return (
                    $"Look at this bed and {where}.",
                    $"Every open cell in this bed is in {where}, so this bed's plant will grow in {where}. "
                    + $"That's the only plant {where} can have, so nothing from another bed can grow there. {MarkThem}");
            }

            case Technique.BedsFillLines: {
                string where = Lines(lines);
                return (
                    $"Look at these {beds.Count} beds and {where}.",
                    $"These {beds.Count} beds only have open cells in {where}, so their {beds.Count} plants fill {where}. "
                    + $"Nothing from any other bed can grow there. {MarkThem}");
            }

            case Technique.LinesFillBeds when beds.Count == 1: {
                string where = Lines(lines);
                return (
                    $"Look at {where}.",
                    $"Every open cell in {where} is in this bed, so {where}'s plant is this bed's plant. "
                    + $"Nothing in this bed can grow outside {where}. {MarkThem}");
            }

            case Technique.LinesFillBeds: {
                string where = Lines(lines);
                return (
                    $"Look at {where}.",
                    $"Every open cell in {where} is in these {beds.Count} beds, so the plants of {where} are these beds' plants. "
                    + $"Nothing in these beds can grow outside {where}. {MarkThem}");
            }

            default: {
                // WouldStarve: the area that would be left with nowhere to grow.
                string area = Name(step.Areas[0]);
                return (
                    $"Look at {area}.",
                    $"A plant in any ringed cell would rule out every open cell left in {area}, leaving it nowhere to grow. "
                    + $"So none of those cells can hold a plant. Mark them with ✕.");
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

    private static string Capitalized(string text) => char.ToUpperInvariant(text[0]) + text[1..];
}
