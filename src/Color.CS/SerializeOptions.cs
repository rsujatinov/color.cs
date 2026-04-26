namespace Color.CS;

/// <summary>Controls how a <see cref="Color"/> is serialized to a CSS Color 4 string.</summary>
public sealed record SerializeOptions
{
    /// <summary>
    /// Number of significant digits used when formatting coordinate and alpha values.
    /// <c>null</c> means no rounding is applied. Defaults to <c>5</c>.
    /// </summary>
    public int? Precision { get; init; } = 5;

    /// <summary>
    /// When <c>true</c> (default), each bounded coordinate is clamped to the color space's
    /// declared <see cref="CoordMetadata.Range"/> before serialization.
    /// </summary>
    public bool InGamut { get; init; } = true;

    /// <summary>
    /// Override the output format identifier (e.g. <c>"hex"</c>, <c>"rgb"</c>, <c>"hsl"</c>,
    /// <c>"srgb"</c>).  When <c>null</c>, the format recorded in <see cref="Color.ParseMeta"/>
    /// is used; if that is also <c>null</c>, the color space's first registered format is used.
    /// </summary>
    public string? Format { get; init; }
}
