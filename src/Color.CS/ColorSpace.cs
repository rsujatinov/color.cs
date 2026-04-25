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

    /// <summary>
    /// Returns the zero-based index of <paramref name="channelName"/> in <see cref="Channels"/>,
    /// or <c>-1</c> if the channel is not found.
    /// </summary>
    public int CoordIndex(string channelName) => Channels.IndexOf(channelName);

    /// <summary>
    /// Returns the <see cref="ColorSpace"/> whose <see cref="Id"/> matches <paramref name="id"/>,
    /// or <c>null</c> if no built-in space is registered with that identifier.
    /// </summary>
    public static ColorSpace? FromId(string id) => id switch
    {
        "srgb" => Srgb,
        _ => null
    };
}
