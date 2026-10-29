using System.Diagnostics;
using Sproutoku.Core.Saving;

namespace Sproutoku.App.Saving;

/// <summary>
/// Keeps the player's progress on the device.
/// Every call is wrapped, so a storage problem never stops the game; progress just isn't saved.
/// </summary>
public sealed class GameStorage {
    private const string SaveKey = "sproutoku.save";

    public GameSave? Load() {
        try {
            return GameSave.FromJson(Preferences.Default.Get<string?>(SaveKey, null));
        }
        catch (Exception ex) {
            Debug.WriteLine($"Couldn't load the saved game: {ex.Message}");
            return null;
        }
    }

    public void Save(GameSave save) {
        try {
            Preferences.Default.Set(SaveKey, save.ToJson());
        }
        catch (Exception ex) {
            Debug.WriteLine($"Couldn't save the game: {ex.Message}");
        }
    }

    /// <summary>
    /// A complete reset, like a fresh install:
    /// Clears everything this app keeps in Preferences.
    /// Power-ups, collections and settings stored there later are cleared too.
    /// If anything is ever stored outside Preferences (files, SecureStorage), clear it here as well.
    /// </summary>
    public void ResetEverything() {
        try {
            Preferences.Default.Clear();
        }
        catch (Exception ex) {
            Debug.WriteLine($"Couldn't reset saved data: {ex.Message}");
        }
    }
}
