namespace Sproutoku.Core.Generation;

/// <summary>
/// How a garden's beds grow. Each generator attempt picks one, so gardens vary in look.
/// </summary>
public enum BedStyle {
    /// <summary>
    /// Big beds grow faster: a few large beds and some tiny ones (the original style).
    /// </summary>
    Sprawling,

    /// <summary>
    /// A cell joins a bed it touches on only one side more often, so beds wind and interlock.
    /// </summary>
    Winding,

    /// <summary>
    /// Small beds grow faster, so bed sizes even out.
    /// </summary>
    Even,
}
