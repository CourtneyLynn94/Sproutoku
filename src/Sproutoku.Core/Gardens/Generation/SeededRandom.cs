namespace Sproutoku.Core.Generation;

/// <summary>
/// Random numbers that are the same for a given seed on every device and every .NET version,
/// so a level's garden is the same for every player. All garden generation uses this, never System.Random.
/// The algorithm is SplitMix64. Changing it would change every generated garden, so tests pin its outputs.
/// </summary>
public sealed class SeededRandom {
    private const ulong Golden = 0x9E3779B97F4A7C15;

    private ulong _state;

    public SeededRandom(ulong seed) {
        _state = seed;
    }

    /// <summary>
    /// One seed made from several numbers, such as generator version, level and attempt.
    /// The same numbers always give the same seed; different numbers or a different order give unrelated seeds.
    /// </summary>
    public static ulong SeedFrom(params long[] parts) {
        ulong seed = 0;
        foreach (long part in parts) {
            seed = Scramble(unchecked(seed + Golden + (ulong)part));
        }

        return seed;
    }

    /// <summary>
    /// The next 64 random bits.
    /// </summary>
    public ulong NextUInt64() {
        _state = unchecked(_state + Golden);
        return Scramble(_state);
    }

    /// <summary>
    /// A whole number from 0 up to, but not including, maxExclusive. Every value is equally likely.
    /// </summary>
    public int Next(int maxExclusive) {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxExclusive, 1);
        return (int)NextBelow((ulong)maxExclusive);
    }

    /// <summary>
    /// A whole number from minInclusive up to, but not including, maxExclusive.
    /// </summary>
    public int Next(int minInclusive, int maxExclusive) {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minInclusive, maxExclusive);

        // The width is worked out as a long, so even int.MinValue to int.MaxValue doesn't overflow.
        ulong width = (ulong)((long)maxExclusive - minInclusive);
        return (int)(minInclusive + (long)NextBelow(width));
    }

    // A number from 0 up to, but not including, bound, every value equally likely.
    private ulong NextBelow(ulong bound) {
        ulong threshold = unchecked(0UL - bound) % bound;
        while (true) {
            ulong value = NextUInt64();
            if (value >= threshold) {
                return value % bound;
            }
        }
    }

    /// <summary>
    /// A number from 0.0 up to, but not including, 1.0.
    /// </summary>
    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));

    /// <summary>
    /// Shuffles a list in place (Fisher–Yates), so every order is equally likely.
    /// </summary>
    public void Shuffle<T>(IList<T> items) {
        ArgumentNullException.ThrowIfNull(items);

        for (int i = items.Count - 1; i > 0; i--) {
            int j = Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    /// <summary>
    /// One item from a list, each equally likely.
    /// </summary>
    public T Pick<T>(IReadOnlyList<T> items) {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfZero(items.Count);
        return items[Next(items.Count)];
    }

    // SplitMix64's mixing step: turns each state into well-spread bits.
    private static ulong Scramble(ulong z) {
        unchecked {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
            return z ^ (z >> 31);
        }
    }
}
