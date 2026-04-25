using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Color.CS;

/// <summary>
/// Represents a color space definition and drives coordinate conversion between spaces.
/// </summary>
/// <param name="options">Configuration for this space.</param>
public class ColorSpace(ColorSpaceOptions options) : IEquatable<ColorSpace>
{
    // ── Static registry ──────────────────────────────────────────────────────

    private static readonly ConcurrentDictionary<string, ColorSpace> s_registry =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Thread-safe dictionary of all registered color spaces keyed by id or alias.</summary>
    public static IReadOnlyDictionary<string, ColorSpace> Registry => s_registry;

    /// <summary>All unique registered color spaces (aliases deduplicated).</summary>
    public static IEnumerable<ColorSpace> All
    {
        get
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (_, space) in s_registry)
            {
                if (seen.Add(space.Id))
                    yield return space;
            }
        }
    }

    /// <summary>
    /// Registers <paramref name="space"/> and all its aliases in the global registry.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// A space with the same id or an alias is already registered.
    /// </exception>
    public static void Register(ColorSpace space)
    {
        if (!s_registry.TryAdd(space.Id, space))
            throw new InvalidOperationException(
                $"A color space with id '{space.Id}' is already registered.");

        foreach (var alias in space.Aliases)
        {
            if (!s_registry.TryAdd(alias, space))
                throw new InvalidOperationException(
                    $"A color space with alias '{alias}' is already registered.");
        }
    }

    /// <summary>
    /// Returns <paramref name="spaceOrId"/> unchanged if it is already a <see cref="ColorSpace"/>;
    /// otherwise looks up the string id in the registry.
    /// </summary>
    /// <exception cref="ArgumentException">The id is not registered.</exception>
    public static ColorSpace Get(object spaceOrId) => spaceOrId switch
    {
        ColorSpace cs => cs,
        string id => s_registry.TryGetValue(id, out var found)
            ? found
            : throw new ArgumentException($"Unknown color space: '{id}'.", nameof(spaceOrId)),
        _ => throw new ArgumentException(
            $"Cannot resolve a ColorSpace from type '{spaceOrId?.GetType().FullName}'.",
            nameof(spaceOrId))
    };

    /// <summary>
    /// Returns the registered <see cref="ColorSpace"/> for <paramref name="id"/>,
    /// or <c>null</c> if no space is registered with that identifier.
    /// </summary>
    public static ColorSpace? FromId(string id) =>
        s_registry.TryGetValue(id, out var space) ? space : null;

    /// <summary>
    /// Searches all registered spaces for the first format whose id or name matches
    /// <paramref name="nameOrId"/>.
    /// </summary>
    public static (ColorSpace Space, ColorFormat Format)? FindFormat(string nameOrId)
    {
        foreach (var space in All)
        {
            foreach (var fmt in space.Formats)
            {
                if (string.Equals(fmt.Id, nameOrId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fmt.Name, nameOrId, StringComparison.OrdinalIgnoreCase))
                    return (space, fmt);
            }
        }
        return null;
    }

    // ── Built-in spaces ───────────────────────────────────────────────────

    /// <summary>Standard sRGB color space (D65 white point, gamma-encoded).</summary>
    public static readonly ColorSpace Srgb;

    static ColorSpace()
    {
        Srgb = new ColorSpace(new ColorSpaceOptions
        {
            Id = "srgb",
            Name = "sRGB",
            White = "D65",
            Coords = new Dictionary<string, CoordMetadata>
            {
                ["red"]   = new() { Name = "Red",   Range = new CoordRange(0, 1) },
                ["green"] = new() { Name = "Green", Range = new CoordRange(0, 1) },
                ["blue"]  = new() { Name = "Blue",  Range = new CoordRange(0, 1) },
            },
        });
        Register(Srgb);
    }

    // ── Instance properties ───────────────────────────────────────────────

    /// <summary>Machine-readable identifier (e.g. <c>"srgb"</c>).</summary>
    public string Id { get; } = options.Id;

    /// <summary>Human-readable name (e.g. <c>"sRGB"</c>).</summary>
    public string Name { get; } = options.Name;

    /// <summary>Parent / base color space. <c>null</c> for root spaces (e.g. XYZ).</summary>
    public ColorSpace? Base { get; } = options.Base;

    /// <summary>
    /// Ordered dictionary of coordinate metadata keyed by channel identifier
    /// (e.g. <c>"red"</c>, <c>"hue"</c>).
    /// </summary>
    public IReadOnlyDictionary<string, CoordMetadata> Coords { get; } = options.Coords;

    /// <summary>White-point reference (e.g. <c>"D65"</c>).</summary>
    public string White { get; } = options.White;

    /// <summary>Registered serialisation formats for this space.</summary>
    public IReadOnlyList<ColorFormat> Formats { get; } = options.Formats;

    /// <summary>Alternative identifiers also registered in the global registry.</summary>
    public IReadOnlyList<string> Aliases { get; } = options.Aliases;

    /// <summary>
    /// Function that converts coords from this space to <see cref="Base"/>.
    /// <c>null</c> for root spaces or identity conversions.
    /// </summary>
    public Func<double[], double[]>? ToBase { get; } = options.ToBase;

    /// <summary>
    /// Function that converts coords from <see cref="Base"/> into this space.
    /// <c>null</c> for root spaces or identity conversions.
    /// </summary>
    public Func<double[], double[]>? FromBase { get; } = options.FromBase;

    /// <summary>
    /// Ordered channel identifiers derived from <see cref="Coords"/> keys
    /// (e.g. <c>["red", "green", "blue"]</c>).
    /// </summary>
    public ImmutableArray<string> Channels
    {
        get
        {
            if (field.IsDefault)
                field = [.. Coords.Keys];
            return field;
        }
    }

    /// <summary>
    /// <c>true</c> if any coordinate has type <see cref="CoordType.Angle"/> (i.e. this is a polar space).
    /// Computed once from the immutable <see cref="Coords"/> dictionary.
    /// </summary>
    public bool IsPolar { get; } = options.Coords.Values.Any(static c => c.Type == CoordType.Angle);

    /// <summary>
    /// <c>true</c> if no coordinate has a <see cref="CoordMetadata.Range"/> (all coords are unbounded).
    /// Computed once from the immutable <see cref="Coords"/> dictionary.
    /// </summary>
    public bool IsUnbounded { get; } = options.Coords.Values.All(static c => c.Range is null);

    /// <summary>
    /// The space used for gamut checking. For polar spaces this is <see cref="Base"/> (or self when
    /// there is no base); for all other spaces it is <c>this</c>.
    /// Computed once from the immutable <see cref="IsPolar"/> and <see cref="Base"/> values.
    /// </summary>
    public ColorSpace GamutSpace => field ??= IsPolar ? (Base ?? this) : this;

    /// <summary>
    /// Precomputed ancestry chain starting from <c>this</c> up to the root space
    /// (i.e. <c>[this, base, base.base, …, root]</c>).
    /// </summary>
    public IReadOnlyList<ColorSpace> Path => field ??= BuildPath();

    private IReadOnlyList<ColorSpace> BuildPath()
    {
        var list = new List<ColorSpace>();
        ColorSpace? current = this;
        while (current is not null)
        {
            list.Add(current);
            current = current.Base;
        }
        return list.AsReadOnly();
    }

    // ── Coordinate helpers ────────────────────────────────────────────────

    /// <summary>
    /// Returns the zero-based index of <paramref name="channelName"/> in <see cref="Channels"/>,
    /// or <c>-1</c> if the channel is not defined in this space.
    /// </summary>
    public int CoordIndex(string channelName) => Channels.IndexOf(channelName);

    // ── Conversion ────────────────────────────────────────────────────────

    /// <summary>
    /// Converts <paramref name="coords"/> from this space into <paramref name="target"/> using
    /// the lowest-common-ancestor path algorithm.
    /// </summary>
    /// <exception cref="InvalidOperationException">No shared ancestor exists.</exception>
    public double[] To(ColorSpace target, double[] coords)
    {
        if (Equals(target))
            return (double[])coords.Clone();

        var srcPath = Path;
        var dstPath = target.Path;

        // Find the lowest common ancestor (LCA): first element of dstPath present in srcPath.
        var srcIds = new HashSet<string>(srcPath.Select(static s => s.Id),
            StringComparer.OrdinalIgnoreCase);

        int lcaInDst = -1;
        for (var i = 0; i < dstPath.Count; i++)
        {
            if (srcIds.Contains(dstPath[i].Id))
            {
                lcaInDst = i;
                break;
            }
        }

        if (lcaInDst < 0)
            throw new InvalidOperationException(
                $"No shared ancestor found between '{Id}' and '{target.Id}'.");

        var lca = dstPath[lcaInDst];
        var current = (double[])coords.Clone();

        // Ascend: this → LCA, applying each space's ToBase.
        foreach (var space in srcPath)
        {
            if (ReferenceEquals(space, lca)) break;
            if (space.ToBase is { } toBase)
                current = toBase(current);
        }

        // Descend: LCA → target, applying each space's FromBase in reverse order.
        // dstPath[0..lcaInDst-1] = [target, Q1, ..., Q(k-1)]; iterate in reverse (Q(k-1) → target).
        for (var i = lcaInDst - 1; i >= 0; i--)
        {
            if (dstPath[i].FromBase is { } fromBase)
                current = fromBase(current);
        }

        return current;
    }

    /// <summary>Inverse of <see cref="To"/>: converts from <paramref name="source"/> into this space.</summary>
    public double[] From(ColorSpace source, double[] coords) => source.To(this, coords);

    // ── Gamut ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> if all bounded coordinates of <paramref name="coords"/> fall within
    /// their defined <see cref="CoordMetadata.Range"/> (± <paramref name="epsilon"/>).
    /// Unbounded coordinates and <see cref="double.NaN"/> (<c>none</c>) values are always in-gamut.
    /// </summary>
    public bool InGamut(double[] coords, double epsilon = 1e-6)
    {
        var channels = Channels;
        for (var i = 0; i < channels.Length && i < coords.Length; i++)
        {
            if (!Coords.TryGetValue(channels[i], out var meta) || meta.Range is not { } range)
                continue;

            var v = coords[i];
            if (!double.IsNaN(v) && (v < range.Min - epsilon || v > range.Max + epsilon))
                return false;
        }
        return true;
    }

    // ── Equality ──────────────────────────────────────────────────────────

    /// <summary>Two spaces are equal when they share the same <see cref="Id"/> (case-insensitive).</summary>
    public bool Equals(ColorSpace? other) =>
        other is not null &&
        (ReferenceEquals(this, other) ||
         string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase));

    /// <summary>Returns <c>true</c> when <paramref name="id"/> matches this space's <see cref="Id"/>.</summary>
    public bool Equals(string? id) => string.Equals(Id, id, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj switch
    {
        ColorSpace cs => Equals(cs),
        string s      => Equals(s),
        _             => false,
    };

    /// <inheritdoc/>
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Id);

    /// <inheritdoc/>
    public override string ToString() => Id;
}
