using System.Collections.Concurrent;

namespace Sproutoku.Core.Levels;

/// <summary>
/// Grows level gardens on a background thread and keeps them until they're needed,
/// so moving to the next level is instant and the screen never freezes while a big garden grows.
/// Each level is grown at most once, however many times it's asked for.
/// </summary>
public sealed class LevelPreparer {
    private readonly Func<int, LevelGarden> _grow;
    private readonly ConcurrentDictionary<int, Lazy<Task<LevelGarden>>> _gardens = new();

    /// <param name="grow">How to grow a level: the real levels unless a test passes its own.</param>
    public LevelPreparer(Func<int, LevelGarden>? grow = null) {
        _grow = grow ?? GardenLevels.ForLevel;
    }

    /// <summary>
    /// Starts growing a level in the background, unless it's already grown or growing.
    /// </summary>
    public void Prepare(int level) => _ = GetAsync(level);

    /// <summary>
    /// The level's garden: at once if it's ready, otherwise when it finishes growing. Never blocks the caller.
    /// </summary>
    public Task<LevelGarden> GetAsync(int level) {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        return _gardens.GetOrAdd(level, key => new Lazy<Task<LevelGarden>>(() => Task.Run(() => _grow(key)))).Value;
    }

    /// <summary>
    /// True if the level is grown, so going to it needs no wait.
    /// </summary>
    public bool IsReady(int level) =>
        _gardens.TryGetValue(level, out var garden) && garden.IsValueCreated && garden.Value.IsCompletedSuccessfully;

    /// <summary>
    /// Lets go of levels below this one, so memory doesn't build up as the player climbs.
    /// </summary>
    public void ForgetBelow(int level) {
        foreach (int key in _gardens.Keys) {
            if (key < level) _gardens.TryRemove(key, out _);
        }
    }
}
