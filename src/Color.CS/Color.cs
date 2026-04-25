using System.Collections.Immutable;

namespace Color.CS;

/// <summary>
/// Represents a color value defined by its coordinates in a given color space.
/// </summary>
/// <param name="Space">The color space this color belongs to.</param>
/// <param name="Coords">The channel coordinates (e.g. R, G, B or L, a, b).</param>
/// <param name="Alpha">The alpha/opacity channel, in the range [0, 1].</param>
public sealed record Color(ColorSpace Space, ImmutableArray<double> Coords, double Alpha = 1.0)
{
    /// <summary>Creates a copy of this color with updated coordinates or alpha.</summary>
    public Color With(ImmutableArray<double>? coords = null, double? alpha = null)
        => this with { Coords = coords ?? Coords, Alpha = alpha ?? Alpha };
}
