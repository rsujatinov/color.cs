namespace Color.CS;

/// <summary>
/// The resolved result of a coordinate reference (e.g. <c>"lch.l"</c> or a bare <c>"lightness"</c>).
/// Returned by <see cref="ColorSpace.ResolveCoord"/>.
/// </summary>
/// <param name="Space">The color space in which the coordinate was resolved.</param>
/// <param name="Id">The canonical channel identifier (e.g. <c>"lightness"</c>).</param>
/// <param name="Index">Zero-based index of the channel in <see cref="Space"/>.</param>
public readonly record struct CoordRef(ColorSpace Space, string Id, int Index);
