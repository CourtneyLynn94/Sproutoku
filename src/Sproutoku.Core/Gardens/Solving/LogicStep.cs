using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Solving;

public enum AreaKind {
    Bed,
    Row,
    Column,
}

/// <summary>
/// A bed, row or column: each holds exactly one plant.
/// </summary>
public readonly record struct Area(AreaKind Kind, int Index);

public enum Technique {
    /// <summary>
    /// A plant rules out the rest of its row, column and bed, and the cells touching it.
    /// </summary>
    PlantRulesOut,

    /// <summary>
    /// A bed, row or column with only one possible cell left must hold its plant there.
    /// </summary>
    OnlyCellLeft,

    /// <summary>
    /// k beds whose open cells all lie in the same k rows (or columns) own those rows' plants,
    /// so other beds' cells in those rows are ruled out (k = 1 is a bed squeezed into one row).
    /// </summary>
    BedsFillLines,

    /// <summary>
    /// k rows (or columns) whose open cells all belong to the same k beds hold those beds' plants,
    /// so the beds' cells outside those rows are ruled out (k = 1 is a row with only one bed left).
    /// </summary>
    LinesFillBeds,

    /// <summary>
    /// A cell whose plant would rule out every remaining cell of another bed, row or column
    /// can't hold a plant, or that area would have nowhere left to grow.
    /// </summary>
    WouldStarve,
}

/// <summary>
/// One deduction: the technique, the beds, rows and columns it's about, what it proves, and the cells that explain why.
/// The Worm's hints are written from these.
/// </summary>
public sealed record LogicStep(
    Technique Technique,
    IReadOnlyList<Area> Areas,
    IReadOnlyList<Cell> Plants,
    IReadOnlyList<Cell> RuledOut,
    IReadOnlyList<Cell> Reasons);

/// <summary>
/// The result of solving: whether every bed got its plant, the steps taken in order, and every plant known at the end.
/// </summary>
public sealed record SolveResult(bool IsSolved, IReadOnlyList<LogicStep> Steps, IReadOnlyList<Cell> Plants);
