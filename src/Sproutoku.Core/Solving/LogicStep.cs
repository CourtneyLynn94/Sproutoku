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
}

/// <summary>
/// One deduction: the technique, the area it's about (if any), what it proves, and the cells that explain why.
/// The Worm's hints are written from these.
/// </summary>
public sealed record LogicStep(
    Technique Technique,
    Area? Area,
    IReadOnlyList<Cell> Plants,
    IReadOnlyList<Cell> RuledOut,
    IReadOnlyList<Cell> Reasons);

/// <summary>
/// The result of solving: whether every bed got its plant, the steps taken in order, and every plant known at the end.
/// </summary>
public sealed record SolveResult(bool IsSolved, IReadOnlyList<LogicStep> Steps, IReadOnlyList<Cell> Plants);
