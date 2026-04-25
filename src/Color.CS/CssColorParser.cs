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

        return ParseResult.Fail($"Unsupported CSS color format: '{css}'.");
    }

    // ── Hex ──────────────────────────────────────────────────────────────

    private static ParseResult ParseHex(ReadOnlySpan<char> s)
    {
        var hex = s[1..]; // strip '#'
        var len = hex.Length;

        // Expand shorthand: #rgb → rrggbb, #rgba → rrggbbaa
        string full;
        if (len is 3 or 4)
        {
            Span<char> buf = stackalloc char[len * 2];
            for (var i = 0; i < len; i++) buf[i * 2] = buf[i * 2 + 1] = hex[i];
            full = new string(buf);
        }
        else if (len is 6 or 8)
        {
            full = hex.ToString();
        }
        else
        {
            return ParseResult.Fail($"Invalid hex color '{s}': expected 3, 4, 6, or 8 hex digits.");
        }

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
        var (tokens, alpha) = SplitArgs(inner.ToString());
        if (tokens.Length != 3)
            return ParseResult.Fail($"{funcName}() expects 3 coordinate values, got {tokens.Length}.");

        Span<double> coords = stackalloc double[3];
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

            coords[i] = isPercent ? v / 100.0 : v / 255.0;
        }

        return ParseResult.Ok(ColorSpace.Srgb, coords, alpha, funcName);
    }

    // ── hsl / hsla / hwb ─────────────────────────────────────────────────

    private static ParseResult ParseHslOrHwb(
        ReadOnlySpan<char> inner, string funcName, ColorSpace space)
    {
        var (tokens, alpha) = SplitArgs(inner.ToString());
        if (tokens.Length != 3)
            return ParseResult.Fail($"{funcName}() expects 3 coordinate values, got {tokens.Length}.");

        Span<double> coords = stackalloc double[3];

        // coord[0] is the hue (angle)
        var hue = ParseAngle(tokens[0].AsSpan().Trim());
        if (!hue.HasValue)
            return ParseResult.Fail($"Invalid hue '{tokens[0]}' in {funcName}().");
        coords[0] = hue.Value;

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
            coords[i] = v;
        }

        return ParseResult.Ok(space, coords, alpha, funcName);
    }

    // ── lab / oklab ───────────────────────────────────────────────────────

    private static ParseResult ParseLabLike(
        ReadOnlySpan<char> inner, string funcName, ColorSpace space)
    {
        var (tokens, alpha) = SplitArgs(inner.ToString());
        if (tokens.Length != 3)
            return ParseResult.Fail($"{funcName}() expects 3 coordinate values, got {tokens.Length}.");

        Span<double> coords = stackalloc double[3];
        var channels = space.Channels;

        for (var i = 0; i < 3; i++)
        {
            var meta  = space.Coords.TryGetValue(channels[i], out var m) ? m : null;
            var scale = (meta?.Range ?? meta?.RefRange) is { } r ? Math.Abs(r.Max) : 1.0;

            var result = ParseScaledCoord(tokens[i].AsSpan().Trim(), scale);
            if (!result.HasValue)
                return ParseResult.Fail($"Invalid coordinate '{tokens[i]}' in {funcName}().");
            coords[i] = result.Value;
        }

        return ParseResult.Ok(space, coords, alpha, funcName);
    }

    // ── lch / oklch ───────────────────────────────────────────────────────

    private static ParseResult ParseLchLike(
        ReadOnlySpan<char> inner, string funcName, ColorSpace space)
    {
        var (tokens, alpha) = SplitArgs(inner.ToString());
        if (tokens.Length != 3)
            return ParseResult.Fail($"{funcName}() expects 3 coordinate values, got {tokens.Length}.");

        Span<double> coords = stackalloc double[3];
        var channels = space.Channels;

        for (var i = 0; i < 3; i++)
        {
            var t = tokens[i].AsSpan().Trim();

            // coord[2] (hue) is an angle
            if (i == 2)
            {
                var h = ParseAngle(t);
                if (!h.HasValue)
                    return ParseResult.Fail($"Invalid hue '{tokens[i]}' in {funcName}().");
                coords[i] = h.Value;
                continue;
            }

            var meta  = space.Coords.TryGetValue(channels[i], out var m) ? m : null;
            var scale = (meta?.Range ?? meta?.RefRange) is { } r ? r.Max : 1.0;

            var result = ParseScaledCoord(t, scale);
            if (!result.HasValue)
                return ParseResult.Fail($"Invalid coordinate '{tokens[i]}' in {funcName}().");
            coords[i] = result.Value;
        }

        return ParseResult.Ok(space, coords, alpha, funcName);
    }

    // ── color() ───────────────────────────────────────────────────────────

    private static ParseResult ParseColorFunction(ReadOnlySpan<char> inner)
    {
        inner = inner.Trim();

        // Split alpha
        double alpha;
        if (inner.IndexOf('/') is var slashIdx and >= 0)
        {
            var av = ParseAlphaValue(inner[(slashIdx + 1)..].Trim());
            if (!av.HasValue)
                return ParseResult.Fail("Invalid alpha value in color().");
            alpha = av.Value;
            inner = inner[..slashIdx].Trim();
        }
        else
        {
            alpha = 1.0;
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

        return ParseResult.Ok(space, buffer, alpha, spaceId);
    }

    // ── Shared helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Splits the arguments of a CSS function into coordinate tokens and an alpha value.
    /// Supports both comma-separated (legacy) and modern space + <c>/</c> syntax.
    /// </summary>
    private static (string[] CoordTokens, double Alpha) SplitArgs(string inner)
    {
        // Legacy comma-separated: rgb(r, g, b) or rgba(r, g, b, a)
        if (inner.Contains(','))
        {
            var parts = inner.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length == 4)
            {
                var av = ParseAlphaValue(parts[3].AsSpan());
                return (parts[..3], av ?? 1.0);
            }
            return (parts, 1.0);
        }

        // Modern space syntax: fn(c1 c2 c3 / a)
        double alpha;
        string coordPart;
        var slashIdx = inner.IndexOf('/');
        if (slashIdx >= 0)
        {
            var av    = ParseAlphaValue(inner[(slashIdx + 1)..].AsSpan().Trim());
            alpha     = av ?? 1.0;
            coordPart = inner[..slashIdx].Trim();
        }
        else
        {
            alpha     = 1.0;
            coordPart = inner.Trim();
        }

        var tokens = new List<string>();
        foreach (var part in new SpaceSplitEnumerator(coordPart.AsSpan()))
            tokens.Add(part.ToString());

        return ([.. tokens], alpha);
    }

    /// <summary>
    /// Parses a coordinate that may be <c>none</c> (→ <see cref="double.NaN"/>),
    /// a raw number (used as-is), or a percentage (<c>value / 100 × scale</c>).
    /// Returns <c>null</c> on failure.
    /// </summary>
    private static double? ParseScaledCoord(ReadOnlySpan<char> t, double scale = 1.0)
    {
        t = t.Trim();
        if (t.Equals("none", StringComparison.OrdinalIgnoreCase))
            return double.NaN;

        if (t.EndsWith("%"))
            return double.TryParse(t[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var pct)
                ? pct / 100.0 * scale : null;

        return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    /// <summary>
    /// Parses a hue angle token with optional unit suffix (deg, rad, turn, grad)
    /// or plain number (degrees). Returns <c>null</c> on failure.
    /// </summary>
    private static double? ParseAngle(ReadOnlySpan<char> t)
    {
        t = t.Trim();
        if (t.Equals("none", StringComparison.OrdinalIgnoreCase))
            return double.NaN;

        if (t.EndsWith("deg", StringComparison.OrdinalIgnoreCase))
            return double.TryParse(t[..^3], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;

        if (t.EndsWith("grad", StringComparison.OrdinalIgnoreCase))
            return double.TryParse(t[..^4], NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
                ? v * 360.0 / 400.0 : null;

        if (t.EndsWith("turn", StringComparison.OrdinalIgnoreCase))
            return double.TryParse(t[..^4], NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
                ? v * 360.0 : null;

        if (t.EndsWith("rad", StringComparison.OrdinalIgnoreCase))
            return double.TryParse(t[..^3], NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
                ? v * (180.0 / Math.PI) : null;

        // Plain number → treat as degrees
        return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var deg) ? deg : null;
    }

    /// <summary>
    /// Parses an alpha token (plain number 0–1 or percentage 100% = 1.0).
    /// Returns <c>null</c> on failure.
    /// </summary>
    private static double? ParseAlphaValue(ReadOnlySpan<char> t)
    {
        t = t.Trim();
        if (t.Equals("none", StringComparison.OrdinalIgnoreCase))
            return double.NaN;

        if (t.EndsWith("%"))
            return double.TryParse(t[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var pct)
                ? pct / 100.0 : null;

        return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
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
