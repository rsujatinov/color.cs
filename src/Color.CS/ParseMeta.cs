namespace Color.CS;

/// <summary>
/// Metadata captured when a <see cref="Color"/> is parsed from a CSS string.
/// Used to round-trip the color back to its original CSS format.
/// </summary>
/// <param name="FormatId">
/// The identifier of the CSS format that was recognised during parsing
/// (e.g. <c>"hex"</c>, <c>"rgb"</c>, <c>"hsl"</c>, <c>"srgb"</c>).
/// </param>
public sealed record ParseMeta(string FormatId);
