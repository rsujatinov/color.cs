namespace Color.CS;

/// <summary>
/// Metadata captured when a <see cref="Color"/> is parsed from a CSS string.
/// Used to round-trip the color back to its original CSS format.
/// </summary>
/// <param name="FormatId">
/// The identifier of the CSS format that was recognised during parsing
/// (e.g. <c>"hex"</c>, <c>"rgb"</c>, <c>"hsl"</c>, <c>"srgb"</c>).
/// </param>
/// <param name="CoordTypes">
/// Per-coordinate CSS type hints recorded during parsing (e.g. <c>"&lt;percentage&gt;"</c>,
/// <c>"&lt;number&gt;[0,255]"</c>). <c>null</c> entries mean no special type was detected.
/// Used by the serialiser to restore the original coordinate format on round-trip.
/// </param>
/// <param name="AlphaType">
/// CSS type hint for the alpha channel as parsed (e.g. <c>"&lt;percentage&gt;"</c> when
/// the source string used <c>80%</c> for the alpha). <c>null</c> means the alpha was a
/// plain number or was absent.
/// </param>
public sealed record ParseMeta(
    string FormatId,
    IReadOnlyList<string?>? CoordTypes = null,
    string? AlphaType = null);
