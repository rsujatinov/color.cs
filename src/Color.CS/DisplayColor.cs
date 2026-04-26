namespace Color.CS;

/// <summary>
/// The result of <see cref="Color.Display"/>: combines a CSS string representation with the
/// <see cref="Color"/> that was actually rendered (which may be clamped to gamut).
/// </summary>
public sealed class DisplayColor(string css, Color color)
{
    /// <summary>The CSS string representation of the color.</summary>
    public string Css { get; } = css;

    /// <summary>
    /// The color that was rendered.  When <see cref="SerializeOptions.InGamut"/> is <c>true</c>
    /// this is a gamut-clamped copy; otherwise it is the original color.
    /// </summary>
    public Color Color { get; } = color;

    /// <inheritdoc/>
    public override string ToString() => Css;
}
