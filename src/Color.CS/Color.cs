using System.Collections.Immutable;
using System.Globalization;
using System.Text;

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
        var success = (ParseResult.Success)result;
        (Space, Coords, Alpha, ParseMeta) = (success.Space, success.Coords, success.Alpha, success.Meta);
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

    /// <summary>
    /// Serialises this color to a CSS Color Level 4 string.
    /// </summary>
    /// <param name="options">Serialisation options; <c>null</c> uses defaults.</param>
    public string ToCss(SerializeOptions? options = null)
    {
        options ??= new SerializeOptions();

        // Resolve which format to use ──────────────────────────────────────
        // When the caller explicitly picks a format, ParseMeta coord types don't apply
        // (they were recorded for the original input format, not the override).
        var isFormatOverridden = options.Format is not null;
        var formatIdHint = options.Format ?? ParseMeta?.FormatId;
        ColorFormat? format = null;
        var targetSpace = Space;

        if (formatIdHint is not null)
        {
            // Look inside the current space first
            format = Space.Formats.FirstOrDefault(f =>
                string.Equals(f.Id, formatIdHint, StringComparison.OrdinalIgnoreCase));

            // Fall back to a global search (format may belong to a different space)
            if (format is null && ColorSpace.FindFormat(formatIdHint) is { } found)
            {
                format = found.Format;
                targetSpace = found.Space;
            }

            // When the caller explicitly requested a format that doesn't exist, throw
            if (format is null && isFormatOverridden)
                throw new NotSupportedException(
                    $"Format '{formatIdHint}' is not registered for any color space.");
        }

        // Default: first registered format for the space
        if (format is null && Space.Formats.Count > 0)
            format = Space.Formats[0];

        // Convert coordinates to the target space when needed ────────────
        var coords = ReferenceEquals(targetSpace, Space) || targetSpace.Equals(Space)
            ? Coords.ToArray()
            : Space.To(targetSpace, Coords.ToArray());

        // Gamut clamping ──────────────────────────────────────────────────
        if (options.InGamut)
            ClampCoordsInPlace(targetSpace, coords);

        // Per-coord type hints: use ParseMeta types for round-trips (unless format overridden)
        var parsedCoordTypes = isFormatOverridden ? null : ParseMeta?.CoordTypes;
        var parsedAlphaType  = isFormatOverridden ? null : ParseMeta?.AlphaType;

        // Dispatch to the right serialiser ────────────────────────────────
        if (format is null)
            return SerializeColorFunctionSyntax(targetSpace.Id, coords, Alpha, options.Precision);

        if (string.Equals(format.Type, "custom", StringComparison.OrdinalIgnoreCase))
            return SerializeCustomFormat(format, coords, Alpha, options, parsedCoordTypes);

        return SerializeFunctionFormat(format, targetSpace, coords, Alpha, options, parsedCoordTypes, parsedAlphaType);
    }

    /// <summary>
    /// Returns a <see cref="DisplayColor"/> containing the CSS string representation and the
    /// color as it will actually be rendered (gamut-clamped when
    /// <see cref="SerializeOptions.InGamut"/> is <c>true</c>).
    /// </summary>
    /// <param name="options">Serialisation options; <c>null</c> uses defaults.</param>
    public DisplayColor Display(SerializeOptions? options = null)
    {
        options ??= new SerializeOptions();

        Color renderedColor;
        if (options.InGamut)
        {
            var clampedCoords = Coords.ToArray();
            ClampCoordsInPlace(Space, clampedCoords);
            renderedColor = this with { Coords = ImmutableArray.Create(clampedCoords) };
        }
        else
        {
            renderedColor = this;
        }

        return new DisplayColor(ToCss(options), renderedColor);
    }

    /// <inheritdoc/>
    public override string ToString() => ToCss();

    /// <summary>
    /// Serialises this color to a CSS Color Level 4 string using the given options.
    /// Equivalent to <see cref="ToCss(SerializeOptions?)"/>.
    /// </summary>
    public string ToString(SerializeOptions options) => ToCss(options);

    // ── Private serialisation helpers ─────────────────────────────────────

    private static void ClampCoordsInPlace(ColorSpace space, double[] coords)
    {
        var channels = space.Channels;
        for (var i = 0; i < channels.Length && i < coords.Length; i++)
        {
            if (double.IsNaN(coords[i])) continue;
            if (!space.Coords.TryGetValue(channels[i], out var meta)) continue;
            if (meta.Type == CoordType.Angle) continue;
            if (meta.Range is { } range)
                coords[i] = Math.Clamp(coords[i], range.Min, range.Max);
        }
    }

    private static string SerializeColorFunctionSyntax(
        string spaceId, double[] coords, double alpha, int? precision)
    {
        var sb = new StringBuilder();
        sb.Append("color(").Append(spaceId);
        foreach (var c in coords)
            sb.Append(' ').Append(SerializeCoordValue(c, null, precision));
        AppendAlpha(sb, alpha, precision, useCommas: false);
        sb.Append(')');
        return sb.ToString();
    }

    private static string SerializeCustomFormat(
        ColorFormat format, double[] coords, double alpha, SerializeOptions options,
        IReadOnlyList<string?>? parsedCoordTypes)
    {
        if (string.Equals(format.Id, "hex", StringComparison.OrdinalIgnoreCase))
        {
            // alpha override: false = suppress, true = force, null = auto (include when < 1 or NaN)
            var includeAlpha = options.ForceAlpha ?? (double.IsNaN(alpha) || alpha < 1.0);
            return SerializeHex(coords, alpha, includeAlpha);
        }

        throw new NotSupportedException(
            $"Custom format '{format.Id}' cannot be serialised — no serialize handler is registered.");
    }

    private static string SerializeHex(double[] coords, double alpha, bool? includeAlphaOverride = null)
    {
        static int ToByte(double v)
        {
            var clamped = double.IsNaN(v) ? 0.0 : Math.Clamp(v, 0.0, 1.0);
            return (int)Math.Round(clamped * 255.0);
        }

        var r = ToByte(coords.Length > 0 ? coords[0] : 0);
        var g = ToByte(coords.Length > 1 ? coords[1] : 0);
        var b = ToByte(coords.Length > 2 ? coords[2] : 0);

        // Determine whether to include alpha
        var includeAlpha = includeAlphaOverride
            ?? (double.IsNaN(alpha) || alpha < 1.0);

        if (includeAlpha)
        {
            var a = double.IsNaN(alpha) ? 0 : (int)Math.Round(Math.Clamp(alpha, 0.0, 1.0) * 255.0);
            return $"#{r:x2}{g:x2}{b:x2}{a:x2}";
        }

        return $"#{r:x2}{g:x2}{b:x2}";
    }

    private static string SerializeFunctionFormat(
        ColorFormat format, ColorSpace space, double[] coords, double alpha,
        SerializeOptions options, IReadOnlyList<string?>? parsedCoordTypes,
        string? parsedAlphaType = null)
    {
        var funcName   = format.UseColorFunction ? "color" : (format.Name ?? format.Id);
        var useCommas  = options.Commas ?? format.UseCommas;
        var separator  = useCommas ? ", " : " ";
        var channels   = space.Channels;

        var sb = new StringBuilder();
        sb.Append(funcName).Append('(');

        if (format.UseColorFunction)
            sb.Append(space.Id);

        for (var i = 0; i < coords.Length; i++)
        {
            // Need a separator before the first coordinate when the space id was already written,
            // or before every subsequent coordinate in legacy function syntax.
            var needsSeparator = i > 0 || format.UseColorFunction;
            if (needsSeparator)
                sb.Append(separator);

            // Resolve effective CSS type hint for this coordinate:
            //   1. SerializeOptions.Coords[i]  (caller override)
            //   2. ParseMeta.CoordTypes[i]      (round-trip from original parse)
            //   3. format.Coords[i]             (format default)
            var hint = (options.Coords is not null && i < options.Coords.Count
                            ? options.Coords[i] : null)
                       ?? (parsedCoordTypes is not null && i < parsedCoordTypes.Count
                            ? parsedCoordTypes[i] : null)
                       ?? (format.Coords is not null && i < format.Coords.Count
                            ? format.Coords[i] : null);

            // Compute the percentage scale for this coordinate (100 / rangeMax)
            var channelId      = i < channels.Length ? channels[i] : null;
            var meta           = channelId is not null && space.Coords.TryGetValue(channelId, out var m) ? m : null;
            var rangeMax       = (meta?.Range ?? meta?.RefRange)?.Max ?? 1.0;
            var percentageScale = 100.0 / rangeMax;

            sb.Append(SerializeCoordValue(coords[i], hint, options.Precision, percentageScale));
        }

        // Alpha inclusion: options override → format ForceAlpha → standard rule (include when < 1 or NaN)
        var forceAlpha  = options.ForceAlpha ?? format.ForceAlpha;
        // Alpha format: options.AlphaFormat overrides; else round-trip from parse
        var alphaFormat = options.AlphaFormat ?? parsedAlphaType;
        AppendAlpha(sb, alpha, options.Precision, useCommas, forceAlpha, alphaFormat);
        sb.Append(')');
        return sb.ToString();
    }

    private static string SerializeCoordValue(
        double value, string? hint, int? precision, double percentageScale = 100.0)
    {
        if (double.IsNaN(value))
            return "none";

        if (hint is not null)
        {
            // <number>[0,255]  →  multiply by 255, output as plain number
            if (hint.Contains("[0,255]", StringComparison.OrdinalIgnoreCase) ||
                hint.Contains("[0, 255]", StringComparison.OrdinalIgnoreCase))
                return FormatNumber(value * 255.0, precision);

            // <percentage>  →  scale to [0, 100] based on the coordinate's range max
            if (string.Equals(hint, "<percentage>", StringComparison.OrdinalIgnoreCase))
                return FormatNumber(value * percentageScale, precision) + "%";

            // <angle>  →  append "deg" suffix
            if (string.Equals(hint, "<angle>", StringComparison.OrdinalIgnoreCase))
                return FormatNumber(value, precision) + "deg";
        }

        return FormatNumber(value, precision);
    }

    private static void AppendAlpha(
        StringBuilder sb, double alpha, int? precision, bool useCommas,
        bool forceAlpha = false, string? alphaFormat = null)
    {
        // Skip alpha when it is exactly 1, not NaN, and not forced
        if (!forceAlpha && !double.IsNaN(alpha) && alpha >= 1.0)
            return;

        string alphaStr;
        if (double.IsNaN(alpha))
            alphaStr = "none";
        else if (string.Equals(alphaFormat, "<percentage>", StringComparison.OrdinalIgnoreCase))
            alphaStr = FormatNumber(alpha * 100.0, precision) + "%";
        else
            alphaStr = FormatNumber(alpha, precision);

        sb.Append(useCommas ? $", {alphaStr}" : $" / {alphaStr}");
    }

    private static string FormatNumber(double value, int? precision) =>
        precision is { } p
            ? value.ToString($"G{p}", CultureInfo.InvariantCulture)
            : value.ToString("G", CultureInfo.InvariantCulture);

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
