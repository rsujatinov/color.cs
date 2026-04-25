namespace Color.CS;

/// <summary>
/// Configuration bag used to construct a <see cref="ColorSpace"/> instance.
/// </summary>
public record ColorSpaceOptions
{
    /// <summary>Machine-readable identifier (e.g. <c>"srgb"</c>).</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable name (e.g. <c>"sRGB"</c>).</summary>
    public required string Name { get; init; }

    /// <summary>Parent / base color space. <c>null</c> for root spaces (e.g. XYZ).</summary>
    public ColorSpace? Base { get; init; }

    /// <summary>
    /// Ordered dictionary of coordinate metadata keyed by channel identifier
    /// (e.g. <c>"red"</c>, <c>"green"</c>, <c>"blue"</c>).
    /// </summary>
    public IReadOnlyDictionary<string, CoordMetadata> Coords { get; init; } =
        new Dictionary<string, CoordMetadata>();

    /// <summary>White point reference (e.g. <c>"D50"</c> or <c>"D65"</c>). Defaults to <c>"D65"</c>.</summary>
    public string White { get; init; } = "D65";

    /// <summary>Registered serialisation formats for this space.</summary>
    public IReadOnlyList<ColorFormat> Formats { get; init; } = [];

    /// <summary>Alternative identifiers for this space (also registered in the global registry).</summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    /// <summary>
    /// Converts coordinates from this space to the <see cref="Base"/> space.
    /// <c>null</c> means this is the root (or the conversion is identity).
    /// </summary>
    public Func<double[], double[]>? ToBase { get; init; }

    /// <summary>
    /// Converts coordinates from the <see cref="Base"/> space into this space.
    /// <c>null</c> means this is the root (or the conversion is identity).
    /// </summary>
    public Func<double[], double[]>? FromBase { get; init; }
}
