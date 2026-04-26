namespace Color.CS;

/// <summary>Describes a serialisation format for a <see cref="ColorSpace"/> (e.g. CSS <c>color()</c> notation).</summary>
public sealed record ColorFormat
{
    /// <summary>Machine-readable format identifier (e.g. <c>"rgb"</c>, <c>"hsl"</c>, <c>"srgb"</c>).</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable display name. Defaults to <see cref="Id"/> when <c>null</c>.</summary>
    public string? Name { get; init; }

    /// <summary>Format type keyword (e.g. <c>"function"</c>, <c>"custom"</c>).</summary>
    public string? Type { get; init; }

    /// <summary>
    /// Per-coordinate CSS type hints following the CSS Color 4 grammar
    /// (e.g. <c>"&lt;number&gt; | &lt;angle&gt;"</c>, <c>"&lt;percentage&gt;"</c>).
    /// <c>null</c> when no special hint is needed.
    /// </summary>
    public IReadOnlyList<string>? Coords { get; init; }

    /// <summary>
    /// CSS type hint for the alpha channel (e.g. <c>"&lt;number&gt; | &lt;percentage&gt;"</c>).
    /// <c>null</c> means the format does not include alpha or uses the default syntax.
    /// </summary>
    public string? Alpha { get; init; }

    /// <summary>
    /// <c>true</c> for legacy CSS Level 3 formats that separate values with commas
    /// (e.g. <c>rgba(255, 0, 0, 0.5)</c>) rather than spaces and <c>/</c>.
    /// </summary>
    public bool UseCommas { get; init; }

    /// <summary>
    /// <c>true</c> when this format serializes using the CSS <c>color(spaceId …)</c> function
    /// (e.g. <c>color(srgb 1 0 0)</c>).  <c>false</c> (the default) means the format's
    /// <see cref="Id"/> is used as the function name directly (e.g. <c>rgb()</c>, <c>hsl()</c>).
    /// </summary>
    public bool UseColorFunction { get; init; }
}
