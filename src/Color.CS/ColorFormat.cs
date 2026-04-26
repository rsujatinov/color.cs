namespace Color.CS;

/// <summary>Describes a serialisation format for a <see cref="ColorSpace"/> (e.g. CSS <c>color()</c> notation).</summary>
public sealed record ColorFormat
{
    /// <summary>Machine-readable format identifier (e.g. <c>"rgb"</c>, <c>"hsl"</c>, <c>"srgb"</c>).</summary>
    public required string Id { get; init; }

    /// <summary>
    /// Optional CSS function name used in serialisation output when it differs from
    /// <see cref="Id"/>. For example, the <c>"rgb_number"</c> format (legacy comma-separated
    /// <c>rgb()</c> with <c>[0, 255]</c> numbers) sets <c>Name = "rgb"</c> so that the output
    /// is still valid CSS <c>rgb(…)</c> while the unique ID allows round-trip format detection.
    /// When <c>null</c>, <see cref="Id"/> is used as the CSS function name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>Format type keyword (e.g. <c>"function"</c>, <c>"custom"</c>).</summary>
    public string? Type { get; init; }

    /// <summary>
    /// Per-coordinate CSS type hints for the default serialisation output, one entry per channel.
    /// Supported hints: <c>"&lt;percentage&gt;"</c> (scale by 100/rangeMax and append <c>%</c>),
    /// <c>"&lt;number&gt;[0,255]"</c> (scale by 255), <c>"&lt;angle&gt;"</c> (append <c>deg</c>).
    /// A <c>null</c> entry means no special hint: the coordinate is output as a plain number.
    /// <c>null</c> for the entire list means all coordinates use plain number output.
    /// </summary>
    public IReadOnlyList<string?>? Coords { get; init; }

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

    /// <summary>
    /// <c>true</c> when this format always includes the alpha channel in its output, even when
    /// alpha is exactly 1 (e.g. <c>rgba(…, 1)</c>, <c>hsla(…, 1)</c>).
    /// </summary>
    public bool ForceAlpha { get; init; }
}
