using Sproutoku.Core.Boards;
using Sproutoku.Core.Generation;
using Sproutoku.Core.Levels;
using System.Text.Json;

namespace Sproutoku.Core.Saving;

/// <summary>
/// Everything needed to put the player back where they left off: the level, that garden's board, and the garden's fingerprint.
/// The garden itself isn't saved; the level number builds it again, and the fingerprint checks it's still the same garden.
/// </summary>
public sealed record GameSave(int Version, int Level, int StartingSuns, int SunsLeft, string Cells, string? GardenFingerprint = null) {
    /// <summary>
    /// Bump this when the save format changes.
    /// 3: the save holds its garden's fingerprint, so a board only restores onto the garden it was played on.
    /// Version 1 and 2 saves keep their level but not their board; other versions are ignored.
    /// </summary>
    public const int CurrentVersion = 3;

    public static GameSave From(int level, Board board) {
        ArgumentNullException.ThrowIfNull(board);
        return new GameSave(CurrentVersion, level, board.StartingSuns, board.SunsLeft, board.CellsToText(), GardenLikeness.Fingerprint(board.Puzzle));
    }

    public string ToJson() => JsonSerializer.Serialize(this);

    /// <summary>
    /// Reads a save, or returns null if there is none, it can't be read, or it's a version this game doesn't know.
    /// A version 1 or 2 save keeps its level but loses its board: previously a level number meant a different garden, so Resume starts that level fresh.
    /// </summary>
    public static GameSave? FromJson(string? json) {
        if (string.IsNullOrWhiteSpace(json)) {
            return null;
        }

        try {
            var save = JsonSerializer.Deserialize<GameSave>(json);
            return save?.Version switch {
                CurrentVersion => save,
                // Saves from before fingerprints can't show whether their garden changed since,
                // so they keep their level but not their board, and Resume starts that level fresh.
                1 or 2 => save with { Version = CurrentVersion, Cells = "", GardenFingerprint = null },
                _ => null,
            };
        }
        catch (JsonException) {
            return null;
        }
    }

    /// <summary>
    /// Where to pick up. Without a usable save, Garden 1 from the start.
    /// A failed garden starts fresh (Try again was the only choice), and a solved one moves on to the next level.
    /// If the saved board no longer fits its garden (for example after an update changed it), the player keeps their level and it starts fresh.
    /// </summary>
    public static (LevelGarden Garden, Board Board) Resume(GameSave? save) {
        if (save is null || save.Level < 1) {
            var first = GardenLevels.ForLevel(1);
            return (first, first.Start());
        }

        var garden = GardenLevels.ForLevel(save.Level);

        // A board only restores onto the garden it was played on. If an update changed this level's garden, it starts fresh.
        if (save.GardenFingerprint != garden.Fingerprint || Board.TryRestore(garden.Puzzle, save.Cells, save.StartingSuns, save.SunsLeft) is not { } board) {
            return (garden, garden.Start());
        }

        if (board.IsOutOfSun) {
            return (garden, board.Restart());
        }

        if (board.IsSolved) {
            var next = GardenLevels.ForLevel(save.Level + 1);
            return (next, next.Start());
        }

        return (garden, board);
    }
}
