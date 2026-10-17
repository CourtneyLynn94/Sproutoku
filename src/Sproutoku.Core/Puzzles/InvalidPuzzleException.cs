namespace Sproutoku.Core.Puzzles;

/// <summary>
/// Thrown by Puzzle.
/// Create when the data breaks a rule.
/// Errors lists every problem found.
/// </summary>
public sealed class InvalidPuzzleException(IReadOnlyList<PuzzleError> errors)
    : Exception("Invalid puzzle: " + string.Join(" ", errors.Select(e => e.Message)))
{
    public IReadOnlyList<PuzzleError> Errors { get; } = errors;
}
