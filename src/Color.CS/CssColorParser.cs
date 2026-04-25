using System.Globalization;

namespace Color.CS;

/// <summary>
/// Minimal parser for the CSS Color Level 4 <c>color()</c> functional notation.
/// </summary>
/// <remarks>
/// Supported syntax: <c>color(&lt;space-id&gt; c1 c2 … [ / alpha ])</c>
/// where each component is a decimal number or the keyword <c>none</c> (mapped to <see cref="double.NaN"/>).
/// </remarks>
internal static class CssColorParser
{
    internal static (ColorSpace Space, double[] Coords, double Alpha) Parse(string css)
    {
        var s = css.AsSpan().Trim();

        if (s.StartsWith("color(", StringComparison.OrdinalIgnoreCase) && s.EndsWith(")"))
            return ParseColorFunction(s["color(".Length..^1]);

        throw new FormatException($"Unsupported CSS color format: '{css}'. Only the color() function is currently supported.");
    }

    private static (ColorSpace Space, double[] Coords, double Alpha) ParseColorFunction(ReadOnlySpan<char> inner)
    {
        inner = inner.Trim();

        double alpha = 1.0;
        var slashIdx = inner.IndexOf('/');
        if (slashIdx >= 0)
        {
            alpha = ParseComponent(inner[(slashIdx + 1)..].Trim());
            inner = inner[..slashIdx].Trim();
        }

        var spaceEnd = inner.IndexOf(' ');
        if (spaceEnd < 0)
            throw new FormatException("color() function must contain at least a space identifier.");

        var spaceId = inner[..spaceEnd].ToString();
        var space = ColorSpace.FromId(spaceId)
            ?? throw new FormatException($"Unknown color space identifier: '{spaceId}'.");

        var coordSpan = inner[(spaceEnd + 1)..].Trim();
        var builder = new System.Collections.Generic.List<double>();
        foreach (var part in new SpaceSplitEnumerator(coordSpan))
            builder.Add(ParseComponent(part));

        return (space, builder.ToArray(), alpha);
    }

    private static double ParseComponent(ReadOnlySpan<char> token)
    {
        token = token.Trim();
        return token.Equals("none", StringComparison.OrdinalIgnoreCase)
            ? double.NaN
            : double.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>Ref-struct enumerator that splits a <see cref="ReadOnlySpan{char}"/> on spaces.</summary>
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
                    Current = _remaining;
                    _remaining = [];
                    return !Current.IsEmpty;
                }

                Current = _remaining[..idx];
                _remaining = _remaining[(idx + 1)..].TrimStart();
                if (!Current.IsEmpty)
                    return true;
            }
            return false;
        }
    }
}
