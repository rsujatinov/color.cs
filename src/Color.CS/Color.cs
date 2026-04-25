using System.Collections.Immutable;
using System.Globalization;

namespace Color.CS;

/// <summary>
/// Represents an immutable color value defined by its coordinates in a given color space.
/// </summary>
public sealed record Color
{
    /// <summary>The color space this color belongs to.</summary>
    public ColorSpace Space { get; init; }

    /// <summary>The channel coordinates (e.g. R, G, B or L, a, b).</summary>
    public ImmutableArray<double> Coords { get; init; }

    /// <summary>
    /// The alpha/opacity channel. Clamped to <c>[0, 1]</c>; <see cref="double.NaN"/> represents <c>none</c>.
    /// </summary>
    public double Alpha
    {
        get;
        init => field = double.IsNaN(value) ? value : Math.Clamp(value, 0.0, 1.0);
    }

    /// <summary>
    /// The metadata describing which CSS format was used to parse this color.
    /// <c>null</c> when the color was not constructed from a CSS string.
    /// </summary>
    public ParseMeta? ParseMeta { get; init; }

    // ──────────────────────────────────────────────────────────────────────
    // Constructors
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>Creates a <see cref="Color"/> from a <c>double[]</c> coordinate array.</summary>
    public Color(ColorSpace space, double[] coords, double alpha = 1.0)
        => (Space, Coords, Alpha) = (space, ImmutableArray.Create(coords), alpha);

    /// <summary>
    /// Creates a <see cref="Color"/> from a <see cref="ReadOnlySpan{T}"/> of coordinates.
    /// Collection expressions (e.g. <c>[1.0, 0.0, 0.0]</c>) resolve to this overload.
    /// </summary>
    public Color(ColorSpace space, ReadOnlySpan<double> coords, double alpha = 1.0)
        => (Space, Coords, Alpha) = (space, ImmutableArray.Create(coords), alpha);

    /// <summary>Parses a CSS Color Level 4 string.</summary>
    /// <exception cref="FormatException">The string is not a recognized CSS color format.</exception>
    public Color(string cssString)
    {
        var result = CssColorParser.TryParse(cssString);
        if (result is ParseResult.Failure failure)
            throw new FormatException(failure.Error);
        var s = (ParseResult.Success)result;
        (Space, Coords, Alpha, ParseMeta) = (s.Space, s.Coords, s.Alpha, s.Meta);
    }

    /// <summary>Creates a <see cref="Color"/> from a <see cref="PlainColorObject"/>.</summary>
    /// <exception cref="ArgumentException">The space identifier in <paramref name="obj"/> is unknown.</exception>
    public Color(PlainColorObject obj)
        => (Space, Coords, Alpha) = (
            ColorSpace.FromId(obj.SpaceId)
                ?? throw new ArgumentException($"Unknown color space: '{obj.SpaceId}'.", nameof(obj)),
            obj.Coords,
            obj.Alpha);

    // ──────────────────────────────────────────────────────────────────────
    // Coordinate accessors
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the coordinate value for the named channel (e.g. <c>"red"</c>, <c>"green"</c>, <c>"blue"</c>).
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The channel name is not defined in <see cref="Space"/>.
    /// </exception>
    public double this[string channelName] =>
        Space.CoordIndex(channelName) switch
        {
            var idx when (uint)idx < (uint)Coords.Length => Coords[idx],
            _ => throw new ArgumentException(
                $"Channel '{channelName}' not found in color space '{Space.Id}'.", nameof(channelName))
        };

    // ──────────────────────────────────────────────────────────────────────
    // Mutation helpers
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>Creates a copy of this color with updated coordinates and/or alpha.</summary>
    public Color With(ImmutableArray<double>? coords = null, double? alpha = null)
        => this with { Coords = coords ?? Coords, Alpha = alpha ?? Alpha };

    // ──────────────────────────────────────────────────────────────────────
    // Serialisation
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>Serialises this color to a JSON object <c>{ spaceId, coords, alpha }</c>.</summary>
    public string ToJson() =>
        $$"""{"spaceId":"{{Space.Id}}","coords":[{{string.Join(",", Coords.Select(static c => double.IsNaN(c) ? "null" : c.ToString("G", CultureInfo.InvariantCulture)))}}],"alpha":{{(double.IsNaN(Alpha) ? "null" : Alpha.ToString("G", CultureInfo.InvariantCulture))}}}""";

    /// <inheritdoc/>
    public override string ToString() => ToJson();

    // ──────────────────────────────────────────────────────────────────────
    // Static factory helpers
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to parse a CSS Color Level 4 string without throwing.
    /// Returns a <see cref="ParseResult.Success"/> on success or a
    /// <see cref="ParseResult.Failure"/> with an error message on failure.
    /// </summary>
    public static ParseResult TryParseCss(string css) => CssColorParser.TryParse(css);

    /// <summary>
    /// Returns <paramref name="colorLike"/> unchanged if it is already a <see cref="Color"/>;
    /// otherwise constructs a new <see cref="Color"/> from it.
    /// </summary>
    /// <param name="colorLike">
    /// A <see cref="Color"/>, <see cref="string"/> (CSS), or <see cref="PlainColorObject"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="colorLike"/> cannot be converted to a <see cref="Color"/>.
    /// </exception>
    public static Color Get(object colorLike) => colorLike switch
    {
        Color c => c,
        string s => new Color(s),
        PlainColorObject p => new Color(p),
        _ => throw new ArgumentException(
            $"Cannot create a Color from type '{colorLike?.GetType().FullName}'.", nameof(colorLike))
    };

    /// <summary>
    /// Like <see cref="Get"/>, but returns <c>null</c> on failure instead of throwing.
    /// </summary>
    public static Color? Try(object colorLike)
    {
        try { return Get(colorLike); }
        catch { return null; }
    }
}

/// <summary>Extension helpers for <see cref="Color"/>.</summary>
public static class ColorExtensions
{
    /// <summary>Returns an independent copy of <paramref name="color"/>.</summary>
    /// <remarks>
    /// Because <see cref="Color"/> is an immutable record whose <see cref="Color.Coords"/> use
    /// <see cref="ImmutableArray{T}"/>, the copy is structurally independent by default.
    /// This method is the C# equivalent of <c>color.clone()</c> in color.js.
    /// </remarks>
    public static Color Clone(this Color color) => color with { };
}
