namespace Color.CS;

/// <summary>Describes a serialisation format for a <see cref="ColorSpace"/> (e.g. CSS <c>color()</c> notation).</summary>
public sealed record ColorFormat
{
    /// <summary>Machine-readable format identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable display name. Defaults to <see cref="Id"/> when <c>null</c>.</summary>
    public string? Name { get; init; }

    /// <summary>Format type keyword (e.g. <c>"function"</c>, <c>"custom"</c>).</summary>
    public string? Type { get; init; }
}
