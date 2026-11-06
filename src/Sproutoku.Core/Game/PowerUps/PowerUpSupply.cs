namespace Sproutoku.Core.Play;

/// <summary>
/// The counted power-ups. The Garden rake isn't one: it's an unlimited tool.
/// </summary>
public enum PowerUp {
    /// <summary>
    /// Plants one correct seed.
    /// </summary>
    Seeds,

    /// <summary>
    /// The worm shows where to look next and explains why.
    /// </summary>
    Worm,

    /// <summary>
    /// Reveals a few cells where nothing can grow.
    /// </summary>
    Water,
}

/// <summary>
/// How many of each power-up are left for the current garden.
/// </summary>
public sealed record PowerUpSupply(int Seeds, int Worm, int Water) {
    /// <summary>
    /// What every garden starts with until rewards exist: two Worm hints, and no Seeds or Water yet.
    /// </summary>
    public static PowerUpSupply Starting { get; } = new(Seeds: 0, Worm: 2, Water: 0);

    public int this[PowerUp powerUp] => powerUp switch {
        PowerUp.Seeds => Seeds,
        PowerUp.Worm => Worm,
        PowerUp.Water => Water,
        _ => throw new ArgumentOutOfRangeException(nameof(powerUp), powerUp, "Not a power-up."),
    };

    public bool Has(PowerUp powerUp) => this[powerUp] > 0;

    /// <summary>
    /// The supply with one fewer of a power-up. Throws if there are none left.
    /// </summary>
    public PowerUpSupply Spend(PowerUp powerUp) {
        if (!Has(powerUp)) {
            throw new InvalidOperationException($"No {powerUp} left to spend.");
        }

        return powerUp switch {
            PowerUp.Seeds => this with { Seeds = Seeds - 1 },
            PowerUp.Worm => this with { Worm = Worm - 1 },
            _ => this with { Water = Water - 1 },
        };
    }

    /// <summary>
    /// False for a supply that can't happen in play, such as a corrupted save with negative counts.
    /// </summary>
    public static bool IsValid(PowerUpSupply? supply) =>
        supply is { Seeds: >= 0, Worm: >= 0, Water: >= 0 };
}
