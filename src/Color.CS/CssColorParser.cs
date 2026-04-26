using System.Globalization;

namespace Color.CS;

/// <summary>
/// CSS Color Level 4 string parser. Supports hex, rgb/rgba, hsl/hsla, hwb,
/// lab, lch, oklab, oklch, and the generic <c>color()</c> function.
/// </summary>
internal static class CssColorParser
{
    // ── Public entry point ───────────────────────────────────────────────

    /// <summary>
    /// Attempts to parse <paramref name="css"/> as a CSS Color Level 4 string.
    /// Returns a <see cref="ParseResult.Success"/> or <see cref="ParseResult.Failure"/>
    /// without throwing.
    /// </summary>
    internal static ParseResult TryParse(string css)
    {
        var s = css.AsSpan().Trim();

        if (s.StartsWith("#"))
            return ParseHex(s);

        var parenIdx = s.IndexOf('(');
        if (parenIdx > 0 && s.EndsWith(")"))
        {
            var funcName = s[..parenIdx].Trim().ToString().ToLowerInvariant();
            var inner    = s[(parenIdx + 1)..^1];

            return funcName switch
            {
                "rgb"  or "rgba" => ParseRgb(inner, funcName),
                "hsl"  or "hsla" => ParseHslOrHwb(inner, funcName, ColorSpace.Hsl),
                "hwb"            => ParseHslOrHwb(inner, funcName, ColorSpace.Hwb),
                "lab"            => ParseLabLike(inner, funcName, ColorSpace.Lab),
                "lch"            => ParseLchLike(inner, funcName, ColorSpace.Lch),
                "oklab"          => ParseLabLike(inner, funcName, ColorSpace.Oklab),
                "oklch"          => ParseLchLike(inner, funcName, ColorSpace.Oklch),
                "color"          => ParseColorFunction(inner),
                _                => ParseResult.Fail($"Unknown CSS color function '{funcName}'.")
            };
        }

        // Named color keywords (e.g. "red", "transparent")
        var keyword = s.ToString();
        if (keyword.Equals("currentcolor", StringComparison.OrdinalIgnoreCase))
            return ParseResult.Fail("'currentcolor' is context-dependent and cannot be resolved to fixed coordinates.");

        if (ColorKeywords.TryGetColor(keyword, out var namedColor))
            return ParseResult.Ok(ColorSpace.Srgb, namedColor.Coords.AsSpan(), namedColor.Alpha, "keyword");

        return ParseResult.Fail($"Unsupported CSS color format: '{css}'.");
    }

    // ── Hex ──────────────────────────────────────────────────────────────

    private static ParseResult ParseHex(ReadOnlySpan<char> s)
    {
        var hex = s[1..]; // strip '#'
        var len = hex.Length;

        // Expand shorthand: #rgb → rrggbb, #rgba → rrggbbaa
        string? full = len switch
        {
            3 or 4 => ExpandShortHex(hex),
            6 or 8 => hex.ToString(),
            _ => null,
        };
        if (full is null)
            return ParseResult.Fail($"Invalid hex color '{s}': expected 3, 4, 6, or 8 hex digits.");

        if (!TryParseHexByte(full, 0, out var r) ||
            !TryParseHexByte(full, 2, out var g) ||
            !TryParseHexByte(full, 4, out var b))
            return ParseResult.Fail($"Invalid hex color '{s}': non-hex digit found.");

        var alpha = 1.0;
        if (full.Length == 8)
        {
            if (!TryParseHexByte(full, 6, out var av))
                return ParseResult.Fail($"Invalid hex color '{s}': non-hex digit in alpha.");
            alpha = av / 255.0;
        }

        Span<double> coords = [r / 255.0, g / 255.0, b / 255.0];
        return ParseResult.Ok(ColorSpace.Srgb, coords, alpha, "hex");
    }

    private static string ExpandShortHex(ReadOnlySpan<char> hex)
    {
        Span<char> buf = stackalloc char[hex.Length * 2];
        for (var i = 0; i < hex.Length; i++) buf[i * 2] = buf[i * 2 + 1] = hex[i];
        return new string(buf);
    }

    private static bool TryParseHexByte(string s, int offset, out double value)
    {
        if (!int.TryParse(s.AsSpan(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
        {
            value = 0;
            return false;
        }
        value = v;
        return true;
    }

    // ── rgb / rgba ────────────────────────────────────────────────────────

    private static ParseResult ParseRgb(ReadOnlySpan<char> inner, string funcName)
    {
        var innerStr = inner.ToString();
        var hasCommas = innerStr.Contains(',');

        var (tokens, alpha, alphaType) = SplitArgs(innerStr);
        if (tokens.Length != 3)
            return ParseResult.Fail($"{funcName}() expects 3 coordinate values, got {tokens.Length}.");

        Span<double> coords = stackalloc double[3];
        var coordTypes = new string?[3];

        for (var i = 0; i < 3; i++)
        {
            var t = tokens[i].AsSpan().Trim();
            if (t.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                coords[i] = double.NaN;
                continue;
            }

            var isPercent = t.EndsWith("%");
            if (!double.TryParse(
                    isPercent ? t[..^1] : t,
                    NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                return ParseResult.Fail($"Invalid coordinate '{tokens[i]}' in {funcName}().");

            if (isPercent)
            {
                coords[i]    = v / 100.0;
                coordTypes[i] = "<percentage>";
            }
            else
            {
                coords[i]    = v / 255.0;
                coordTypes[i] = "<number>[0,255]";
            }
        }

        // When rgb() is comma-separated with all-number coords, use the "rgb_number" format id
        // so that round-trip serialisation reproduces commas and [0,255] numbers.
        var allNumberCoords = Array.TrueForAll(coordTypes, t => t == "<number>[0,255]");
        var formatId = funcName is "rgb" && hasCommas && allNumberCoords
            ? "rgb_number"
            : funcName;

        return ParseResult.Ok(ColorSpace.Srgb, coords, alpha, formatId, coordTypes, alphaType);
    }

    // ── hsl / hsla / hwb ─────────────────────────────────────────────────

    private static ParseResult ParseHslOrHwb(
        ReadOnlySpan<char> inner, string funcName, ColorSpace space)
    {
        var (tokens, alpha, alphaType) = SplitArgs(inner.ToString());
        if (tokens.Length != 3)
            return ParseResult.Fail($"{funcName}() expects 3 coordinate values, got {tokens.Length}.");

        Span<double> coords = stackalloc double[3];
        var coordTypes = new string?[3];

        // coord[0] is the hue (angle) — track whether an explicit angle unit was used
        var (hue, hueHadUnit) = ParseAngleWithInfo(tokens[0].AsSpan().Trim());
        if (!hue.HasValue)
            return ParseResult.Fail($"Invalid hue '{tokens[0]}' in {funcName}().");
        coords[0]    = hue.Value;
        coordTypes[0] = hueHadUnit ? "<angle>" : null;

        // coords[1] and [2] are percentages or raw numbers in [0, 100]
        for (var i = 1; i <= 2; i++)
        {
            var t = tokens[i].AsSpan().Trim();
            if (t.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                coords[i] = double.NaN;
                continue;
            }

            var isPercent = t.EndsWith("%");
            if (!double.TryParse(
                    isPercent ? t[..^1] : t,
                    NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                return ParseResult.Fail($"Invalid coordinate '{tokens[i]}' in {funcName}().");

            // Both "50%" and "50" represent 50 in the [0, 100] range
            coords[i]    = v;
            coordTypes[i] = isPercent ? "<percentage>" : "<number>";
        }

        return ParseResult.Ok(space, coords, alpha, funcName, coordTypes, alphaType);
    }

    // ── lab / oklab ───────────────────────────────────────────────────────

    private static ParseResult ParseLabLike(
        ReadOnlySpan<char> inner, string funcName, ColorSpace space)
    {
        var (tokens, alpha, alphaType) = SplitArgs(inner.ToString());
        if (tokens.Length != 3)
            return ParseResult.Fail($"{funcName}() expects 3 coordinate values, got {tokens.Length}.");

        Span<double> coords = stackalloc double[3];
        var coordTypes = new string?[3];
        var channels = space.Channels;

        for (var i = 0; i < 3; i++)
        {
            var meta  = space.Coords.TryGetValue(channels[i], out var m) ? m : null;
            var scale = (meta?.Range ?? meta?.RefRange) is { } r ? Math.Abs(r.Max) : 1.0;

            var (value, type) = ParseScaledCoordWithType(tokens[i].AsSpan().Trim(), scale);
            if (!value.HasValue)
                return ParseResult.Fail($"Invalid coordinate '{tokens[i]}' in {funcName}().");
            coords[i]    = value.Value;
            coordTypes[i] = type;
        }

        return ParseResult.Ok(space, coords, alpha, funcName, coordTypes, alphaType);
    }

    // ── lch / oklch ───────────────────────────────────────────────────────

    private static ParseResult ParseLchLike(
        ReadOnlySpan<char> inner, string funcName, ColorSpace space)
    {
        var (tokens, alpha, alphaType) = SplitArgs(inner.ToString());
        if (tokens.Length != 3)
            return ParseResult.Fail($"{funcName}() expects 3 coordinate values, got {tokens.Length}.");

        Span<double> coords = stackalloc double[3];
        var coordTypes = new string?[3];
        var channels = space.Channels;

        for (var i = 0; i < 3; i++)
        {
            var t = tokens[i].AsSpan().Trim();

            // coord[2] (hue) is an angle — track whether an explicit angle unit was used
            if (i == 2)
            {
                var (h, hadUnit) = ParseAngleWithInfo(t);
                if (!h.HasValue)
                    return ParseResult.Fail($"Invalid hue '{tokens[i]}' in {funcName}().");
                coords[i]    = h.Value;
                coordTypes[i] = hadUnit ? "<angle>" : null;
                continue;
            }

            var meta  = space.Coords.TryGetValue(channels[i], out var m) ? m : null;
            var scale = (meta?.Range ?? meta?.RefRange) is { } r ? r.Max : 1.0;

            var (value, type) = ParseScaledCoordWithType(t, scale);
            if (!value.HasValue)
                return ParseResult.Fail($"Invalid coordinate '{tokens[i]}' in {funcName}().");
            coords[i]    = value.Value;
            coordTypes[i] = type;
        }

        return ParseResult.Ok(space, coords, alpha, funcName, coordTypes, alphaType);
    }

    // ── color() ───────────────────────────────────────────────────────────

    private static ParseResult ParseColorFunction(ReadOnlySpan<char> inner)
    {
        inner = inner.Trim();

        // Split alpha
        double alpha;
        string? alphaType;
        if (inner.IndexOf('/') is var slashIdx and >= 0)
        {
            var (av, at) = ParseAlphaValueWithType(inner[(slashIdx + 1)..].Trim());
            if (!av.HasValue)
                return ParseResult.Fail("Invalid alpha value in color().");
            alpha     = av.Value;
            alphaType = at;
            inner = inner[..slashIdx].Trim();
        }
        else
        {
            alpha     = 1.0;
            alphaType = null;
        }

        // Extract space identifier
        var spaceEnd = inner.IndexOf(' ');
        if (spaceEnd < 0)
            return ParseResult.Fail("color() must specify at least a color-space identifier and coordinates.");

        var spaceId = inner[..spaceEnd].ToString();

        // FindFormat first (supports lookup by format id, e.g. "srgb"), then fall back to FromId
        ColorSpace space;
        if (ColorSpace.FindFormat(spaceId) is { } found)
            space = found.Space;
        else if (ColorSpace.FromId(spaceId) is { } byId)
            space = byId;
        else
            return ParseResult.Fail($"Unknown color space '{spaceId}' in color().");

        var coordSpan    = inner[(spaceEnd + 1)..].Trim();
        var channelCount = space.Channels.Length;
        Span<double> buffer = stackalloc double[channelCount];
        var i = 0;

        foreach (var part in new SpaceSplitEnumerator(coordSpan))
        {
            if (i >= channelCount)
                return ParseResult.Fail(
                    $"Too many coordinates for '{spaceId}' (expected {channelCount}).");

            var channelId = i < space.Channels.Length ? space.Channels[i] : null;
            var meta      = channelId is not null && space.Coords.TryGetValue(channelId, out var m) ? m : null;
            var range     = meta?.Range ?? meta?.RefRange;
            // For angle coords (hue) the CSS spec uses 360° as 100 % reference
            var scale     = range is { } r
                ? (meta!.Type == CoordType.Angle ? 360.0 : r.Max)
                : 1.0;

            var result = ParseScaledCoord(part, scale);
            if (!result.HasValue)
                return ParseResult.Fail($"Invalid coordinate '{part.ToString()}' in color({spaceId}).");

            buffer[i++] = result.Value;
        }

        if (i < channelCount)
            return ParseResult.Fail(
                $"Too few coordinates for '{spaceId}' (expected {channelCount}, got {i}).");

        return ParseResult.Ok(space, buffer, alpha, spaceId, alphaType: alphaType);
    }

    // ── Shared helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Splits the arguments of a CSS function into coordinate tokens and an alpha value.
    /// Supports both comma-separated (legacy) and modern space + <c>/</c> syntax.
    /// Returns the alpha type hint (<c>"&lt;percentage&gt;"</c> when the alpha used <c>%</c>, else <c>null</c>).
    /// </summary>
    private static (string[] coordTokens, double alpha, string? alphaType) SplitArgs(string inner)
    {
        // Legacy comma-separated: rgb(r, g, b) or rgba(r, g, b, a)
        if (inner.Contains(','))
        {
            var parts = inner.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length == 4)
            {
                var (av, at) = ParseAlphaValueWithType(parts[3].AsSpan());
                return (parts[..3], av ?? 1.0, at);
            }
            return (parts, 1.0, null);
        }

        // Modern space syntax: fn(c1 c2 c3 / a)
        double alpha;
        string? alphaType;
        string coordPart;
        var slashIdx = inner.IndexOf('/');
        if (slashIdx >= 0)
        {
            var (av, at) = ParseAlphaValueWithType(inner[(slashIdx + 1)..].AsSpan().Trim());
            alpha     = av ?? 1.0;
            alphaType = at;
            coordPart = inner[..slashIdx].Trim();
        }
        else
        {
            alpha     = 1.0;
            alphaType = null;
            coordPart = inner.Trim();
        }

        var span = coordPart.AsSpan();
        var tokens = new List<string>(4);
        foreach (var part in new SpaceSplitEnumerator(span))
            tokens.Add(part.ToString());

        return ([.. tokens], alpha, alphaType);
    }

    /// <summary>
    /// Parses a coordinate that may be <c>none</c> (→ <see cref="double.NaN"/>),
    /// a raw number (used as-is), or a percentage (<c>value / 100 × scale</c>).
    /// Returns <c>null</c> on failure.
    /// </summary>
    private static double? ParseScaledCoord(ReadOnlySpan<char> t, double scale = 1.0)
        => ParseScaledCoordWithType(t, scale).value;

    /// <summary>
    /// Like <see cref="ParseScaledCoord"/> but also returns the detected CSS type hint:
    /// <c>"&lt;percentage&gt;"</c> when a <c>%</c> suffix was present, <c>"&lt;number&gt;"</c>
    /// for a raw number, or <c>null</c> for <c>none</c>/<c>NaN</c>.
    /// </summary>
    private static (double? value, string? type) ParseScaledCoordWithType(
        ReadOnlySpan<char> t, double scale = 1.0)
    {
        t = t.Trim();
        if (t.Equals("none", StringComparison.OrdinalIgnoreCase))
            return (double.NaN, null);

        if (t.EndsWith("%"))
            return double.TryParse(t[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var pct)
                ? (pct / 100.0 * scale, "<percentage>")
                : (null, null);

        return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
            ? (v, "<number>")
            : (null, null);
    }

    /// <summary>
    /// Parses a hue angle token with optional unit suffix (deg, rad, turn, grad)
    /// or plain number (degrees). Returns <c>null</c> on failure.
    /// </summary>
    private static double? ParseAngle(ReadOnlySpan<char> t) => ParseAngleWithInfo(t).degrees;

    /// <summary>
    /// Parses a hue angle token and also returns whether an explicit angle unit was present.
    /// </summary>
    private static (double? degrees, bool hadUnit) ParseAngleWithInfo(ReadOnlySpan<char> t)
    {
        t = t.Trim();
        if (t.Equals("none", StringComparison.OrdinalIgnoreCase))
            return (double.NaN, false);

        if (t.EndsWith("deg", StringComparison.OrdinalIgnoreCase))
            return double.TryParse(t[..^3], NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
                ? (v, true) : (null, false);

        if (t.EndsWith("grad", StringComparison.OrdinalIgnoreCase))
            return double.TryParse(t[..^4], NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
                ? (v * 360.0 / 400.0, true) : (null, false);

        if (t.EndsWith("turn", StringComparison.OrdinalIgnoreCase))
            return double.TryParse(t[..^4], NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
                ? (v * 360.0, true) : (null, false);

        if (t.EndsWith("rad", StringComparison.OrdinalIgnoreCase))
            return double.TryParse(t[..^3], NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
                ? (v * (180.0 / Math.PI), true) : (null, false);

        // Plain number → treat as degrees (no explicit unit)
        return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var deg)
            ? (deg, false) : (null, false);
    }

    /// <summary>
    /// Parses an alpha token (plain number 0–1 or percentage 100% = 1.0).
    /// Returns <c>null</c> on failure.
    /// </summary>
    private static double? ParseAlphaValue(ReadOnlySpan<char> t)
        => ParseAlphaValueWithType(t).value;

    /// <summary>
    /// Like <see cref="ParseAlphaValue"/> but also returns the detected CSS type hint:
    /// <c>"&lt;percentage&gt;"</c> when a <c>%</c> suffix was present, else <c>null</c>.
    /// </summary>
    private static (double? value, string? type) ParseAlphaValueWithType(ReadOnlySpan<char> t)
    {
        t = t.Trim();
        if (t.Equals("none", StringComparison.OrdinalIgnoreCase))
            return (double.NaN, null);

        if (t.EndsWith("%"))
            return double.TryParse(t[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var pct)
                ? (pct / 100.0, "<percentage>")
                : (null, null);

        return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
            ? (v, null)
            : (null, null);
    }

    // ── Space-split enumerator ────────────────────────────────────────────

    /// <summary>Ref-struct enumerator that splits a <see cref="ReadOnlySpan{char}"/> on whitespace.</summary>
    private ref struct SpaceSplitEnumerator(ReadOnlySpan<char> span)
    {
        private ReadOnlySpan<char> _remaining = span;

        public ReadOnlySpan<char> Current { get; private set; }

        public readonly SpaceSplitEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            while (!_remaining.IsEmpty)
            {
                var idx = _remaining.IndexOf(' ');
                if (idx < 0)
                {
                    Current    = _remaining;
                    _remaining = [];
                    return !Current.IsEmpty;
                }

                Current    = _remaining[..idx];
                _remaining = _remaining[(idx + 1)..].TrimStart();
                if (!Current.IsEmpty)
                    return true;
            }
            return false;
        }
    }
}
