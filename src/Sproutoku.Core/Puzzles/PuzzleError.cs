namespace Sproutoku.Core.Puzzles;

/// <summary>
/// The kinds of problem the validator can find.
/// </summary>
public enum PuzzleErrorCode {
    NotSquare,
    SizeOutOfRange,
    InvalidRegionId,
    WrongRegionCount,
    DisconnectedRegion,
    WrongSolutionCount,
    SolutionOutsideRegion,
    SolutionRowConflict,
    SolutionColumnConflict,
    SolutionPlantsTouch,
}

/// <summary>
/// One problem found by the validator: a code for tests and code, a message for people.
/// </summary>
public sealed record PuzzleError(PuzzleErrorCode Code, string Message);
