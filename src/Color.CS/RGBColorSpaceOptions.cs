namespace Color.CS;

/// <summary>
/// Configuration bag for <see cref="RGBColorSpace"/>, extending <see cref="ColorSpaceOptions"/>
/// with per-channel gamma transfer functions.
/// </summary>
public sealed record RGBColorSpaceOptions : ColorSpaceOptions
{
    /// <summary>
    /// Linearise a single gamma-encoded channel value (remove gamma).
    /// Applied to every channel when converting to the base space.
    /// </summary>
    public Func<double, double>? Lin { get; init; }

    /// <summary>
    /// Apply gamma encoding to a single linearised channel value.
    /// Applied to every channel when converting from the base space.
    /// </summary>
    public Func<double, double>? Gam { get; init; }
}
