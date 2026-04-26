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

    /// <summary>
    /// Per-coordinate CSS type hint overrides (e.g. <c>"&lt;percentage&gt;"</c>,
    /// <c>"&lt;number&gt;[0,255]"</c>, <c>"&lt;angle&gt;"</c>). Entries may be <c>null</c>
    /// to fall back to the format's default. When <c>null</c>, format defaults are used.
    /// </summary>
    public IReadOnlyList<string?>? Coords { get; init; }

    /// <summary>
    /// Override alpha inclusion behaviour.
    /// <c>true</c> = always include alpha; <c>false</c> = never include alpha;
    /// <c>null</c> (default) = defer to the format's <see cref="ColorFormat.ForceAlpha"/> setting
    /// and the standard rule (include when alpha &lt; 1 or is <c>NaN</c>).
    /// </summary>
    public bool? ForceAlpha { get; init; }

    /// <summary>
    /// Override comma-separated output. <c>true</c> = always use commas;
    /// <c>false</c> = always use spaces; <c>null</c> (default) = defer to the format's
    /// <see cref="ColorFormat.UseCommas"/> setting.
    /// </summary>
    public bool? Commas { get; init; }

    /// <summary>
    /// Override the alpha channel output format. <c>"&lt;percentage&gt;"</c> serializes alpha as
    /// a CSS percentage (e.g. <c>0.8</c> → <c>80%</c>). <c>null</c> (default) outputs alpha as
    /// a plain number. Only takes effect when alpha is actually included in the output.
    /// </summary>
    public string? AlphaFormat { get; init; }
}
