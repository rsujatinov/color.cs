using System.Collections.Immutable;

namespace Color.CS;

/// <summary>
/// Discriminated union returned by the CSS Color 4 parser.
/// Callers should match on <see cref="Success"/> or <see cref="Failure"/> to inspect the result.
/// </summary>
public abstract record ParseResult
{
    private ParseResult() { }

    /// <summary>The parser succeeded; contains the resolved <see cref="ColorSpace"/>, coordinates, alpha, and format metadata.</summary>
    public sealed record Success(
        ColorSpace Space,
        ImmutableArray<double> Coords,
        double Alpha,
        ParseMeta? Meta) : ParseResult;

    /// <summary>The parser failed; contains a human-readable <see cref="Error"/> message.</summary>
    public sealed record Failure(string Error) : ParseResult;

    internal static Success Ok(
        ColorSpace space,
        ReadOnlySpan<double> coords,
        double alpha,
        string? formatId = null,
        IReadOnlyList<string?>? coordTypes = null,
        string? alphaType = null) =>
        new(space, ImmutableArray.Create(coords), alpha,
            formatId is not null ? new ParseMeta(formatId, coordTypes, alphaType) : null);

    internal static Failure Fail(string error) => new(error);
}
