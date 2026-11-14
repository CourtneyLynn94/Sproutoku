namespace Sproutoku.Core.Game.PowerUps;

/// <summary>
/// The counted power-ups. The Garden rake isn't one: it's an unlimited tool.
/// </summary>
public enum PowerUp {
    /// <summary>
    /// Plants one correct seedling.
    /// </summary>
    Seedling,

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
public sealed record PowerUpSupply(int Seedling, int Worm, int Water) {
    /// <summary>
    /// What every garden starts with until the tool shed exists: two of each power-up.
    /// </summary>
    public static PowerUpSupply Starting { get; } = new(Seedling: 2, Worm: 2, Water: 2);

    public int this[PowerUp powerUp] => powerUp switch {
        PowerUp.Seedling => Seedling,
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
            PowerUp.Seedling => this with { Seedling = Seedling - 1 },
            PowerUp.Worm => this with { Worm = Worm - 1 },
            _ => this with { Water = Water - 1 },
        };
    }

    /// <summary>
    /// False for a supply that can't happen in play, such as a corrupted save with negative counts.
    /// </summary>
    public static bool IsValid(PowerUpSupply? supply) =>
        supply is { Seedling: >= 0, Worm: >= 0, Water: >= 0 };
}
