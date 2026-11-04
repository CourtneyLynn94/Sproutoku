using System.Text.Json;
using Sproutoku.Core.Play;

namespace Sproutoku.Core.Saving;

/// <summary>
/// Everything needed to put the player back where they left off: the level and that garden's board.
/// The garden itself isn't saved; the level number builds it again.
/// </summary>
public sealed record GameSave(int Version, int Level, int StartingSuns, int SunsLeft, string Cells) {
    /// <summary>
    /// Bump this when the save format changes; saves with another version are ignored.
    /// </summary>
    public const int CurrentVersion = 1;

    public static GameSave From(int level, Board board) {
        ArgumentNullException.ThrowIfNull(board);
        return new GameSave(CurrentVersion, level, board.StartingSuns, board.SunsLeft, board.CellsToText());
    }

    public string ToJson() => JsonSerializer.Serialize(this);

    /// <summary>
    /// Reads a save, or returns null if there is none, it can't be read, or it's another version.
    /// </summary>
    public static GameSave? FromJson(string? json) {
        if (string.IsNullOrWhiteSpace(json)) {
            return null;
        }

        try {
            var save = JsonSerializer.Deserialize<GameSave>(json);
            return save?.Version == CurrentVersion ? save : null;
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

        if (Board.TryRestore(garden.Puzzle, save.Cells, save.StartingSuns, save.SunsLeft) is not { } board) {
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
