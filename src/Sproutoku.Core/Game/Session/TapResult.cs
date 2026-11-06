namespace Sproutoku.Core.Play;

public enum TapOutcome {
    /// <summary>
    /// Nothing changed: a locked cell, or the garden is finished.
    /// </summary>
    Ignored,

    /// <summary>
    /// A ✕ was placed or cleared.
    /// </summary>
    MarkToggled,

    /// <summary>
    /// A double tap grew the correct plant.
    /// </summary>
    Grew,

    /// <summary>
    /// A double tap wilted a wrong cell and spent a sun.
    /// </summary>
    Wilted,
}

/// <summary>
/// What a tap did, and whether it ended the garden (solved, or out of sun).
/// </summary>
public sealed record TapResult(TapOutcome Outcome, bool EndedGarden);
