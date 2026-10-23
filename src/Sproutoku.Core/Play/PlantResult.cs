namespace Sproutoku.Core.Play;

public enum PlantOutcome {
    Ignored,
    Grew,
    Wilted,
}

/// <summary>
/// The board after a plant attempt, and what happened.
/// </summary>
public sealed record PlantResult(Board Board, PlantOutcome Outcome);
