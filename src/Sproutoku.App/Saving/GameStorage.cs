using System.Diagnostics;
using Sproutoku.Core.Saving;

namespace Sproutoku.App.Saving;

/// <summary>
/// Keeps what the player has earned by playing: the current garden and level now, power-ups and collections later.
/// It all lives in its own Preferences container, so a progress reset never touches the player's settings, which live in the default one.
/// Every call is wrapped, so a storage problem never stops the game; progress just isn't saved.
/// </summary>
public sealed class GameStorage {
    private const string ProgressContainer = "sproutoku.progress";
    private const string SaveKey = "save";

    public GameSave? Load() {
        try {
            return GameSave.FromJson(Preferences.Default.Get<string?>(SaveKey, null, ProgressContainer));
        }
        catch (Exception ex) {
            Debug.WriteLine($"Couldn't load the saved game: {ex.Message}");
            return null;
        }
    }

    public void Save(GameSave save) {
        try {
            Preferences.Default.Set(SaveKey, save.ToJson(), ProgressContainer);
        }
        catch (Exception ex) {
            Debug.WriteLine($"Couldn't save the game: {ex.Message}");
        }
    }

    /// <summary>
    /// Erases everything earned by playing, so the player starts again at Garden 1.
    /// Settings (sound, vibration, patterns...) are kept: they aren't in this container.
    /// </summary>
    public void ResetProgress() {
        try {
            Preferences.Default.Clear(ProgressContainer);
        }
        catch (Exception ex) {
            Debug.WriteLine($"Couldn't reset progress: {ex.Message}");
        }
    }
}
