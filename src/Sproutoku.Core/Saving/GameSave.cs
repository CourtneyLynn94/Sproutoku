using System.Text.Json;
using Sproutoku.Core.Play;

namespace Sproutoku.Core.Saving;

/// <summary>
/// Everything needed to put the player back where they left off: the level and that garden's board.
/// The garden itself isn't saved; the level number finds it again.
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
    /// A lost garden starts fresh (Try again was the only choice), and a solved one moves on to the next garden when there is one.
    /// </summary>
    public static (int Level, Board Board) Resume(GameSave? save) {
        if (save is not null
            && GardenLevels.ForLevel(save.Level) is { } puzzle
            && Board.TryRestore(puzzle, save.Cells, save.StartingSuns, save.SunsLeft) is { } board) {
            if (board.IsOutOfSun) {
                return (save.Level, board.Restart());
            }

            if (board.IsSolved) {
                // Move on, or replay the last garden until more arrive (S43).
                return GardenLevels.ForLevel(save.Level + 1) is { } next
                    ? (save.Level + 1, Board.Start(next))
                    : (save.Level, board.Restart());
            }

            return (save.Level, board);
        }

        // Level 1 always exists, so the ! is safe.
        return (1, Board.Start(GardenLevels.ForLevel(1)!));
    }
}
