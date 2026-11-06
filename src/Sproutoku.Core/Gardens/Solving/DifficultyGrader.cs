namespace Sproutoku.Core.Solving;

/// <summary>
/// How hard a garden is, from the hardest deduction it needs. Easiest first, so grades compare with &lt; and &gt;.
/// </summary>
public enum DifficultyGrade {
    /// <summary>
    /// Plant rule-outs and "only one cell left".
    /// </summary>
    Gentle,

    /// <summary>
    /// A bed that fits in one row or column, or a row or column with only one bed left.
    /// </summary>
    Easy,

    /// <summary>
    /// A plant that would starve a bed, row or column.
    /// </summary>
    Medium,

    /// <summary>
    /// Two beds sharing the same two rows or columns.
    /// </summary>
    Tricky,

    /// <summary>
    /// Three or more beds sharing lines.
    /// </summary>
    Expert,
}

/// <summary>
/// Grades steps and whole gardens.
/// </summary>
public static class DifficultyGrader {
    /// <summary>
    /// The grade of one deduction.
    /// </summary>
    public static DifficultyGrade GradeOf(LogicStep step) {
        ArgumentNullException.ThrowIfNull(step);

        return step.Technique switch {
            Technique.PlantRulesOut or Technique.OnlyCellLeft => DifficultyGrade.Gentle,
            Technique.WouldStarve => DifficultyGrade.Medium,

            // A group of k beds and k lines lists 2k areas.
            _ => (step.Areas.Count / 2) switch {
                1 => DifficultyGrade.Easy,
                2 => DifficultyGrade.Tricky,
                _ => DifficultyGrade.Expert,
            },
        };
    }

    /// <summary>
    /// A garden's grade: its hardest step. Only a garden logic can finish has one.
    /// </summary>
    public static DifficultyGrade Grade(SolveResult result) {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsSolved) {
            throw new ArgumentException("Only a garden logic can finish has a grade.", nameof(result));
        }

        return result.Steps.Count == 0 ? DifficultyGrade.Gentle : result.Steps.Max(GradeOf);
    }
}
