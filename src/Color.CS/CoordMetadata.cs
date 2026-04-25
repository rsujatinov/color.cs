namespace Color.CS;

/// <summary>Describes a single coordinate (channel) within a <see cref="ColorSpace"/>.</summary>
public sealed record CoordMetadata
{
    /// <summary>Human-readable display name (e.g. "Red", "Hue").</summary>
    public required string Name { get; init; }

    /// <summary>Mathematical type of the coordinate. Defaults to <see cref="CoordType.Linear"/>.</summary>
    public CoordType Type { get; init; } = CoordType.Linear;

    /// <summary>
    /// Gamut range for this coordinate (inclusive). <c>null</c> means the coordinate is unbounded.
    /// </summary>
    public CoordRange? Range { get; init; }

    /// <summary>
    /// Reference / display range used for percentage mapping.
    /// Falls back to <see cref="Range"/> when <c>null</c>.
    /// </summary>
    public CoordRange? RefRange { get; init; }
}
