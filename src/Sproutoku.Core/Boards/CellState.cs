namespace Sproutoku.Core.Play;

/// <summary>
/// What the player currently sees in a cell. Empty must stay first: new boards start as all zeros.
/// </summary>
public enum CellState {
    Empty,
    Marked,
    Planted,
    Wilted,
    RevealedEmpty,
}

public static class CellStateExtensions {
    /// <summary>
    /// Locked cells can't be marked, cleared or planted.
    /// </summary>
    public static bool IsLocked(this CellState state) =>
        state is CellState.Planted or CellState.Wilted or CellState.RevealedEmpty;
}
