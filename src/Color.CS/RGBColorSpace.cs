namespace Color.CS;

/// <summary>
/// A <see cref="ColorSpace"/> that adds per-channel gamma transfer function helpers,
/// mirroring <c>RGBColorSpace.js</c> in color.js.
/// </summary>
/// <param name="options">RGB-specific configuration.</param>
public sealed class RGBColorSpace(RGBColorSpaceOptions options) : ColorSpace(options)
{
    /// <summary>
    /// Linearise (remove gamma from) a single channel value.
    /// <c>null</c> when the space is already linear.
    /// </summary>
    public Func<double, double>? Lin { get; } = options.Lin;

    /// <summary>
    /// Apply gamma encoding to a single linearised channel value.
    /// <c>null</c> when the space is already gamma-encoded relative to its base.
    /// </summary>
    public Func<double, double>? Gam { get; } = options.Gam;

    /// <summary>
    /// Returns a new array with each coordinate linearised by <see cref="Lin"/>.
    /// If <see cref="Lin"/> is <c>null</c>, returns a copy of <paramref name="coords"/> unchanged.
    /// </summary>
    public double[] Linearize(double[] coords) =>
        Lin is null
            ? (double[])coords.Clone()
            : coords.Select(Lin).ToArray();

    /// <summary>
    /// Returns a new array with gamma encoding applied to each coordinate via <see cref="Gam"/>.
    /// If <see cref="Gam"/> is <c>null</c>, returns a copy of <paramref name="coords"/> unchanged.
    /// </summary>
    public double[] GammaEncode(double[] coords) =>
        Gam is null
            ? (double[])coords.Clone()
            : coords.Select(Gam).ToArray();
}
