using System.Collections.Immutable;

namespace Color.CS;

/// <summary>
/// Identifies a color space (e.g., sRGB, Display-P3, OKLab).
/// </summary>
/// <param name="Id">Machine-readable identifier (e.g. "srgb").</param>
/// <param name="Name">Human-readable name (e.g. "sRGB").</param>
/// <param name="Channels">The ordered channel names for this space.</param>
public sealed record ColorSpace(string Id, string Name, ImmutableArray<string> Channels)
{
    /// <summary>Standard sRGB color space.</summary>
    public static readonly ColorSpace Srgb = new("srgb", "sRGB", ["red", "green", "blue"]);
}
