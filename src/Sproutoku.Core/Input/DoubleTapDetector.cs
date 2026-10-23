using Sproutoku.Core.Puzzles;

namespace Sproutoku.Core.Input;

/// <summary>
/// Decides whether a tap completes a double tap: the same cell, soon enough after the last tap.
/// The time is passed in (milliseconds) so tests can control it.
/// </summary>
public sealed class DoubleTapDetector(long maxGapMs = 250) {
    private Cell? _lastCell;
    private long _lastTimeMs;

    public long MaxGapMs { get; } = maxGapMs;

    /// <summary>
    /// Records a tap. Returns true if it is the second tap of a double tap.
    /// </summary>
    public bool RegisterTap(Cell cell, long timeMs) {
        bool isDoubleTap = _lastCell == cell && timeMs - _lastTimeMs <= MaxGapMs;

        // After a double tap, start over, so a quick third tap counts as a new first tap.
        _lastCell = isDoubleTap ? null : cell;
        _lastTimeMs = timeMs;
        return isDoubleTap;
    }

    /// <summary>
    /// Forgets the last tap, e.g. when a new garden starts.
    /// </summary>
    public void Reset() {
        _lastCell = null;
    }
}
