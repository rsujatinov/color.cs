namespace Color.CS;

/// <summary>
/// A <see cref="ColorSpace"/> that adds per-channel gamma transfer function helpers,
/// mirroring <c>RGBColorSpace.js</c> in color.js.
/// </summary>
/// <param name="options">RGB-specific configuration.</param>
public sealed class RGBColorSpace(RGBColorSpaceOptions options) : ColorSpace(options)
{
    /// <summary>
    /// Linearize (remove gamma from) a single channel value.
    /// <c>null</c> when the space is already linear.
    /// </summary>
    public Func<double, double>? Lin { get; } = options.Lin;

    /// <summary>
    /// Apply gamma encoding to a single linearized channel value.
    /// <c>null</c> when the space is already gamma-encoded relative to its base.
    /// </summary>
    public Func<double, double>? Gam { get; } = options.Gam;

    /// <summary>
    /// Returns a new array with each coordinate linearized by <see cref="Lin"/>.
    /// If <see cref="Lin"/> is <c>null</c>, returns a copy of <paramref name="coords"/> unchanged.
    /// </summary>
    public double[] Linearize(double[] coords)
    {
        if (Lin is null)
            return (double[])coords.Clone();

        var result = new double[coords.Length];
        for (var i = 0; i < coords.Length; i++)
            result[i] = Lin(coords[i]);
        return result;
    }

    /// <summary>
    /// Returns a new array with gamma encoding applied to each coordinate via <see cref="Gam"/>.
    /// If <see cref="Gam"/> is <c>null</c>, returns a copy of <paramref name="coords"/> unchanged.
    /// </summary>
    public double[] GammaEncode(double[] coords)
    {
        if (Gam is null)
            return (double[])coords.Clone();

        var result = new double[coords.Length];
        for (var i = 0; i < coords.Length; i++)
            result[i] = Gam(coords[i]);
        return result;
    }
}
