using System.Collections.Immutable;

namespace Color.CS;

/// <summary>
/// Plain data object used to construct or represent a <see cref="Color"/> without behavior.
/// </summary>
/// <param name="SpaceId">The machine-readable color-space identifier (e.g. "srgb").</param>
/// <param name="Coords">The channel coordinates.</param>
/// <param name="Alpha">The alpha/opacity channel, in the range [0, 1].</param>
public sealed record PlainColorObject(string SpaceId, ImmutableArray<double> Coords, double Alpha = 1.0);
