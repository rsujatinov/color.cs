using System.Collections.Immutable;

namespace Color.CS.Tests;

/// <summary>
/// Tests for the new ColorSpace infrastructure:
/// registry, coordinate metadata, conversion path algorithm, RGBColorSpace.
/// </summary>
public sealed class ColorSpaceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Creates a unique id so tests don't collide in the global registry.</summary>
    private static string Uid(string tag) => $"test-{tag}-{Guid.NewGuid():N}";

    private static ColorSpace MakeLinear(string id, ColorSpace? baseSpace = null,
        Func<double[], double[]>? toBase = null,
        Func<double[], double[]>? fromBase = null,
        IReadOnlyList<string>? aliases = null) =>
        new(new ColorSpaceOptions
        {
            Id = id,
            Name = id,
            Base = baseSpace,
            ToBase = toBase,
            FromBase = fromBase,
            Aliases = aliases ?? [],
            Coords = new Dictionary<string, CoordMetadata>
            {
                ["x"] = new() { Name = "X", Range = new CoordRange(0, 1) },
                ["y"] = new() { Name = "Y", Range = new CoordRange(0, 1) },
                ["z"] = new() { Name = "Z", Range = new CoordRange(0, 1) },
            },
        });

    private static ColorSpace MakePolar(string id, ColorSpace? baseSpace = null) =>
        new(new ColorSpaceOptions
        {
            Id = id,
            Name = id,
            Base = baseSpace,
            Coords = new Dictionary<string, CoordMetadata>
            {
                ["l"]   = new() { Name = "Lightness", Range = new CoordRange(0, 100) },
                ["c"]   = new() { Name = "Chroma",    Range = new CoordRange(0, 150) },
                ["h"]   = new() { Name = "Hue",       Type = CoordType.Angle },
            },
        });

    private static ColorSpace MakeUnbounded(string id) =>
        new(new ColorSpaceOptions
        {
            Id = id,
            Name = id,
            Coords = new Dictionary<string, CoordMetadata>
            {
                ["a"] = new() { Name = "A" },
                ["b"] = new() { Name = "B" },
            },
        });

    // ── Construction & basic properties ──────────────────────────────────────

    [Fact]
    public void ColorSpace_Id_MatchesOptions()
    {
        var id = Uid("id");
        var cs = MakeLinear(id);
        Assert.Equal(id, cs.Id);
    }

    [Fact]
    public void ColorSpace_Name_MatchesOptions()
    {
        var cs = new ColorSpace(new ColorSpaceOptions
        {
            Id = Uid("name"),
            Name = "My Space",
            Coords = new Dictionary<string, CoordMetadata>(),
        });
        Assert.Equal("My Space", cs.Name);
    }

    [Fact]
    public void ColorSpace_White_DefaultsToD65()
    {
        var cs = MakeLinear(Uid("white"));
        Assert.Equal("D65", cs.White);
    }

    [Fact]
    public void ColorSpace_White_RespectsOption()
    {
        var cs = new ColorSpace(new ColorSpaceOptions
        {
            Id = Uid("white-d50"),
            Name = "D50",
            White = "D50",
            Coords = new Dictionary<string, CoordMetadata>(),
        });
        Assert.Equal("D50", cs.White);
    }

    [Fact]
    public void ColorSpace_Channels_DerivedFromCoordsKeys()
    {
        var cs = MakeLinear(Uid("channels"));
        Assert.Equal(["x", "y", "z"], (IEnumerable<string>)cs.Channels);
    }

    [Fact]
    public void ColorSpace_Channels_PreservesInsertionOrder()
    {
        var cs = new ColorSpace(new ColorSpaceOptions
        {
            Id = Uid("order"),
            Name = "Order",
            Coords = new Dictionary<string, CoordMetadata>
            {
                ["c"] = new() { Name = "C" },
                ["a"] = new() { Name = "A" },
                ["b"] = new() { Name = "B" },
            },
        });
        Assert.Equal(["c", "a", "b"], (IEnumerable<string>)cs.Channels);
    }

    // ── Srgb built-in ────────────────────────────────────────────────────────

    [Fact]
    public void ColorSpace_Srgb_HasCorrectChannels()
    {
        Assert.Equal(["red", "green", "blue"], (IEnumerable<string>)ColorSpace.Srgb.Channels);
    }

    [Fact]
    public void ColorSpace_Srgb_CoordMetadataHasRange()
    {
        var meta = ColorSpace.Srgb.Coords["red"];
        Assert.Equal(new CoordRange(0, 1), meta.Range);
        Assert.Equal(CoordType.Linear, meta.Type);
    }

    [Fact]
    public void ColorSpace_Srgb_IsNotPolar()
    {
        Assert.False(ColorSpace.Srgb.IsPolar);
    }

    [Fact]
    public void ColorSpace_Srgb_IsNotUnbounded()
    {
        Assert.False(ColorSpace.Srgb.IsUnbounded);
    }

    [Fact]
    public void ColorSpace_Srgb_GamutSpaceIsSelf()
    {
        Assert.Same(ColorSpace.Srgb, ColorSpace.Srgb.GamutSpace);
    }

    // ── IsPolar / IsUnbounded / GamutSpace ──────────────────────────────────

    [Fact]
    public void ColorSpace_IsPolar_TrueWhenAngleCoordPresent()
    {
        var cs = MakePolar(Uid("polar"));
        Assert.True(cs.IsPolar);
    }

    [Fact]
    public void ColorSpace_IsPolar_FalseWhenNoAngleCoord()
    {
        var cs = MakeLinear(Uid("notpolar"));
        Assert.False(cs.IsPolar);
    }

    [Fact]
    public void ColorSpace_IsUnbounded_TrueWhenNoCoordsHaveRange()
    {
        var cs = MakeUnbounded(Uid("unbounded"));
        Assert.True(cs.IsUnbounded);
    }

    [Fact]
    public void ColorSpace_IsUnbounded_FalseWhenAnyCoordsHaveRange()
    {
        var cs = MakeLinear(Uid("bounded"));
        Assert.False(cs.IsUnbounded);
    }

    [Fact]
    public void ColorSpace_GamutSpace_PolarUsesBase()
    {
        var baseCs = MakeLinear(Uid("polar-base"));
        var polar = MakePolar(Uid("polar-cs"), baseCs);
        Assert.Same(baseCs, polar.GamutSpace);
    }

    [Fact]
    public void ColorSpace_GamutSpace_PolarWithNoBaseUsesSelf()
    {
        var polar = MakePolar(Uid("polar-nobase"));
        Assert.Same(polar, polar.GamutSpace);
    }

    [Fact]
    public void ColorSpace_GamutSpace_NonPolarUsesSelf()
    {
        var cs = MakeLinear(Uid("nonpolar-gamut"));
        Assert.Same(cs, cs.GamutSpace);
    }

    // ── Path ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ColorSpace_Path_RootSpaceContainsOnlySelf()
    {
        var root = MakeLinear(Uid("root-only"));
        Assert.Equal([root], (IEnumerable<ColorSpace>)root.Path);
    }

    [Fact]
    public void ColorSpace_Path_SingleLevelAncestry()
    {
        var root = MakeLinear(Uid("path-root"));
        var child = MakeLinear(Uid("path-child"), baseSpace: root);
        Assert.Equal([child, root], (IEnumerable<ColorSpace>)child.Path);
    }

    [Fact]
    public void ColorSpace_Path_TwoLevelAncestry()
    {
        var root  = MakeLinear(Uid("p2-root"));
        var mid   = MakeLinear(Uid("p2-mid"),   baseSpace: root);
        var leaf  = MakeLinear(Uid("p2-leaf"),  baseSpace: mid);
        Assert.Equal([leaf, mid, root], (IEnumerable<ColorSpace>)leaf.Path);
    }

    // ── To / From (conversion) ────────────────────────────────────────────────

    [Fact]
    public void ColorSpace_To_SameSpaceReturnsClone()
    {
        double[] coords = [0.1, 0.5, 0.9];
        var result = ColorSpace.Srgb.To(ColorSpace.Srgb, coords);
        Assert.Equal(coords, result);
        Assert.False(ReferenceEquals(coords, result));
    }

    [Fact]
    public void ColorSpace_To_SingleStepAscent()
    {
        // root has no toBase; child.toBase doubles each coord.
        var root  = MakeLinear(Uid("conv-root"));
        var child = MakeLinear(Uid("conv-child"), baseSpace: root,
            toBase: c => c.Select(v => v * 2).ToArray(),
            fromBase: c => c.Select(v => v / 2).ToArray());

        double[] coords = [1.0, 2.0, 3.0];
        var result = child.To(root, coords);
        Assert.Equal([2.0, 4.0, 6.0], result);
    }

    [Fact]
    public void ColorSpace_To_SingleStepDescent()
    {
        var root  = MakeLinear(Uid("desc-root"));
        var child = MakeLinear(Uid("desc-child"), baseSpace: root,
            toBase: c => c.Select(v => v * 2).ToArray(),
            fromBase: c => c.Select(v => v / 2).ToArray());

        double[] coords = [4.0, 8.0, 2.0];
        var result = root.To(child, coords);
        Assert.Equal([2.0, 4.0, 1.0], result);
    }

    [Fact]
    public void ColorSpace_To_MultiStepConversion()
    {
        // A → B → root; convert A to B via root as LCA.
        var root  = MakeLinear(Uid("ms-root"));
        var a     = MakeLinear(Uid("ms-a"), baseSpace: root,
            toBase:   c => c.Select(v => v + 10).ToArray(),
            fromBase: c => c.Select(v => v - 10).ToArray());
        var b     = MakeLinear(Uid("ms-b"), baseSpace: root,
            toBase:   c => c.Select(v => v * 3).ToArray(),
            fromBase: c => c.Select(v => v / 3).ToArray());

        // a.To(b): a → root (+ 10), root → b (/ 3)
        double[] coords = [1.0, 2.0, 3.0];
        var result = a.To(b, coords);
        Assert.Equal([(1 + 10) / 3.0, (2 + 10) / 3.0, (3 + 10) / 3.0], result);
    }

    [Fact]
    public void ColorSpace_From_IsInverseOfTo()
    {
        var root  = MakeLinear(Uid("inv-root"));
        var child = MakeLinear(Uid("inv-child"), baseSpace: root,
            toBase:   c => c.Select(v => v * 2).ToArray(),
            fromBase: c => c.Select(v => v / 2).ToArray());

        double[] coords = [1.0, 0.5, 0.25];
        var toResult   = child.To(root, coords);
        var fromResult = root.From(child, coords);
        Assert.Equal(toResult, fromResult);
    }

    [Fact]
    public void ColorSpace_To_ThrowsWhenNoSharedAncestor()
    {
        var orphanA = MakeLinear(Uid("orphan-a"));
        var orphanB = MakeLinear(Uid("orphan-b"));
        Assert.Throws<InvalidOperationException>(() => orphanA.To(orphanB, [1.0, 0.0, 0.0]));
    }

    // ── InGamut ───────────────────────────────────────────────────────────────

    [Fact]
    public void ColorSpace_InGamut_TrueForCoordsWithinRange()
    {
        Assert.True(ColorSpace.Srgb.InGamut([0.5, 0.5, 0.5]));
    }

    [Fact]
    public void ColorSpace_InGamut_TrueForBoundaryCoordsWithEpsilon()
    {
        Assert.True(ColorSpace.Srgb.InGamut([0.0, 1.0, 0.5]));
    }

    [Fact]
    public void ColorSpace_InGamut_FalseWhenCoordBelowMin()
    {
        Assert.False(ColorSpace.Srgb.InGamut([-0.1, 0.5, 0.5]));
    }

    [Fact]
    public void ColorSpace_InGamut_FalseWhenCoordAboveMax()
    {
        Assert.False(ColorSpace.Srgb.InGamut([0.5, 1.1, 0.5]));
    }

    [Fact]
    public void ColorSpace_InGamut_NaNConsideredInGamut()
    {
        Assert.True(ColorSpace.Srgb.InGamut([double.NaN, 0.5, 0.5]));
    }

    [Fact]
    public void ColorSpace_InGamut_TrueForUnboundedSpace()
    {
        var cs = MakeUnbounded(Uid("gamut-unb"));
        Assert.True(cs.InGamut([999.0, -999.0]));
    }

    [Fact]
    public void ColorSpace_InGamut_SlightlyOutsidePassesWithEpsilon()
    {
        Assert.True(ColorSpace.Srgb.InGamut([1.000001, 0.5, 0.5], epsilon: 0.01));
    }

    // ── Equals ────────────────────────────────────────────────────────────────

    [Fact]
    public void ColorSpace_Equals_SameInstanceIsEqual()
    {
        Assert.True(ColorSpace.Srgb.Equals(ColorSpace.Srgb));
    }

    [Fact]
    public void ColorSpace_Equals_SameIdIsEqual()
    {
        // Build a second instance with the same id — it is equal by id.
        var duplicate = new ColorSpace(new ColorSpaceOptions
        {
            Id = "srgb",
            Name = "Different name",
            Coords = new Dictionary<string, CoordMetadata>(),
        });
        Assert.True(ColorSpace.Srgb.Equals(duplicate));
    }

    [Fact]
    public void ColorSpace_Equals_DifferentIdIsNotEqual()
    {
        var other = MakeLinear(Uid("neq"));
        Assert.False(ColorSpace.Srgb.Equals(other));
    }

    [Fact]
    public void ColorSpace_Equals_StringId()
    {
        Assert.True(ColorSpace.Srgb.Equals("srgb"));
        Assert.True(ColorSpace.Srgb.Equals("SRGB"));
    }

    [Fact]
    public void ColorSpace_Equals_StringId_FalseForMismatch()
    {
        Assert.False(ColorSpace.Srgb.Equals("display-p3"));
    }

    [Fact]
    public void ColorSpace_GetHashCode_SameForEqualSpaces()
    {
        var a = new ColorSpace(new ColorSpaceOptions
        {
            Id = "srgb",
            Name = "X",
            Coords = new Dictionary<string, CoordMetadata>(),
        });
        Assert.Equal(ColorSpace.Srgb.GetHashCode(), a.GetHashCode());
    }

    [Fact]
    public void ColorSpace_ToString_ReturnsId()
    {
        Assert.Equal("srgb", ColorSpace.Srgb.ToString());
    }

    // ── Registry ──────────────────────────────────────────────────────────────

    [Fact]
    public void ColorSpace_Registry_ContainsSrgb()
    {
        Assert.True(ColorSpace.Registry.ContainsKey("srgb"));
        Assert.Same(ColorSpace.Srgb, ColorSpace.Registry["srgb"]);
    }

    [Fact]
    public void ColorSpace_Register_AddsSpaceToRegistry()
    {
        var id = Uid("reg-add");
        var cs = MakeLinear(id);
        ColorSpace.Register(cs);
        Assert.True(ColorSpace.Registry.ContainsKey(id));
        Assert.Same(cs, ColorSpace.Registry[id]);
    }

    [Fact]
    public void ColorSpace_Register_ThrowsOnDuplicateId()
    {
        var id = Uid("reg-dup");
        var cs = MakeLinear(id);
        ColorSpace.Register(cs);
        Assert.Throws<InvalidOperationException>(() => ColorSpace.Register(cs));
    }

    [Fact]
    public void ColorSpace_Register_RegistersAliases()
    {
        var id    = Uid("reg-alias");
        var alias = Uid("alias-val");
        var cs    = MakeLinear(id, aliases: [alias]);
        ColorSpace.Register(cs);
        Assert.True(ColorSpace.Registry.ContainsKey(alias));
        Assert.Same(cs, ColorSpace.Registry[alias]);
    }

    [Fact]
    public void ColorSpace_Register_ThrowsOnDuplicateAlias()
    {
        var alias  = Uid("dup-alias");
        var cs1    = MakeLinear(Uid("alias-cs1"), aliases: [alias]);
        var cs2    = MakeLinear(Uid("alias-cs2"), aliases: [alias]);
        ColorSpace.Register(cs1);
        Assert.Throws<InvalidOperationException>(() => ColorSpace.Register(cs2));
    }

    // ── Get ───────────────────────────────────────────────────────────────────

    [Fact]
    public void ColorSpace_Get_ReturnsSpaceForStringId()
    {
        Assert.Same(ColorSpace.Srgb, ColorSpace.Get("srgb"));
    }

    [Fact]
    public void ColorSpace_Get_ReturnsSpaceAsIs()
    {
        Assert.Same(ColorSpace.Srgb, ColorSpace.Get(ColorSpace.Srgb));
    }

    [Fact]
    public void ColorSpace_Get_ThrowsOnUnknownId()
    {
        Assert.Throws<ArgumentException>(() => ColorSpace.Get("totally-unknown-xyz-abc"));
    }

    [Fact]
    public void ColorSpace_Get_ThrowsOnUnsupportedType()
    {
        Assert.Throws<ArgumentException>(() => ColorSpace.Get(42));
    }

    // ── FromId ────────────────────────────────────────────────────────────────

    [Fact]
    public void ColorSpace_FromId_ReturnsSrgb()
    {
        Assert.Same(ColorSpace.Srgb, ColorSpace.FromId("srgb"));
    }

    [Fact]
    public void ColorSpace_FromId_NullForUnknown()
    {
        Assert.Null(ColorSpace.FromId("not-registered-ever-xyz"));
    }

    // ── All ───────────────────────────────────────────────────────────────────

    [Fact]
    public void ColorSpace_All_ContainsSrgb()
    {
        Assert.Contains(ColorSpace.Srgb, ColorSpace.All);
    }

    [Fact]
    public void ColorSpace_All_DeduplicatesAliases()
    {
        var id    = Uid("all-alias");
        var alias = Uid("all-alias-val");
        var cs    = MakeLinear(id, aliases: [alias]);
        ColorSpace.Register(cs);

        var allIds = ColorSpace.All.Select(s => s.Id).ToList();
        Assert.Single(allIds, id);
        Assert.DoesNotContain(alias, allIds);
    }

    // ── FindFormat ────────────────────────────────────────────────────────────

    [Fact]
    public void ColorSpace_FindFormat_ReturnsMatchByFormatId()
    {
        var fmtId = Uid("fmt");
        var fmt   = new ColorFormat { Id = fmtId, Name = "My Format" };
        var cs    = new ColorSpace(new ColorSpaceOptions
        {
            Id      = Uid("cs-fmt"),
            Name    = "Format Space",
            Coords  = new Dictionary<string, CoordMetadata>(),
            Formats = [fmt],
        });
        ColorSpace.Register(cs);

        var result = ColorSpace.FindFormat(fmtId);
        Assert.NotNull(result);
        Assert.Same(cs, result.Value.Space);
        Assert.Same(fmt, result.Value.Format);
    }

    [Fact]
    public void ColorSpace_FindFormat_ReturnsMatchByFormatName()
    {
        var fmtName = Uid("fmt-name");
        var fmt     = new ColorFormat { Id = Uid("fmt-id2"), Name = fmtName };
        var cs      = new ColorSpace(new ColorSpaceOptions
        {
            Id      = Uid("cs-fmt2"),
            Name    = "Format Space 2",
            Coords  = new Dictionary<string, CoordMetadata>(),
            Formats = [fmt],
        });
        ColorSpace.Register(cs);

        var result = ColorSpace.FindFormat(fmtName);
        Assert.NotNull(result);
        Assert.Same(fmt, result.Value.Format);
    }

    [Fact]
    public void ColorSpace_FindFormat_NullForMissingFormat()
    {
        Assert.Null(ColorSpace.FindFormat("no-such-format-xyz-abc-123"));
    }

    // ── CoordIndex ────────────────────────────────────────────────────────────

    [Fact]
    public void ColorSpace_CoordIndex_CorrectForNewSpace()
    {
        var cs = MakeLinear(Uid("idx"));
        Assert.Equal(0, cs.CoordIndex("x"));
        Assert.Equal(1, cs.CoordIndex("y"));
        Assert.Equal(2, cs.CoordIndex("z"));
        Assert.Equal(-1, cs.CoordIndex("missing"));
    }

    // ── RGBColorSpace ─────────────────────────────────────────────────────────

    [Fact]
    public void RGBColorSpace_Lin_Null_LinearizeReturnsClone()
    {
        var cs = new RGBColorSpace(new RGBColorSpaceOptions
        {
            Id     = Uid("rgb-nolin"),
            Name   = "RGB No Lin",
            Coords = new Dictionary<string, CoordMetadata>
            {
                ["r"] = new() { Name = "R", Range = new CoordRange(0, 1) },
                ["g"] = new() { Name = "G", Range = new CoordRange(0, 1) },
                ["b"] = new() { Name = "B", Range = new CoordRange(0, 1) },
            },
        });

        double[] coords = [0.5, 0.5, 0.5];
        var result = cs.Linearize(coords);
        Assert.Equal(coords, result);
        Assert.False(ReferenceEquals(coords, result));
    }

    [Fact]
    public void RGBColorSpace_Linearize_AppliesLinToEachChannel()
    {
        var cs = new RGBColorSpace(new RGBColorSpaceOptions
        {
            Id     = Uid("rgb-lin"),
            Name   = "RGB Lin",
            Lin    = v => v * v,   // simple squaring as gamma linearization
            Gam    = v => Math.Sqrt(v),
            Coords = new Dictionary<string, CoordMetadata>
            {
                ["r"] = new() { Name = "R", Range = new CoordRange(0, 1) },
                ["g"] = new() { Name = "G", Range = new CoordRange(0, 1) },
                ["b"] = new() { Name = "B", Range = new CoordRange(0, 1) },
            },
        });

        var result = cs.Linearize([0.5, 0.25, 1.0]);
        Assert.Equal([0.25, 0.0625, 1.0], result);
    }

    [Fact]
    public void RGBColorSpace_GammaEncode_AppliesGamToEachChannel()
    {
        var cs = new RGBColorSpace(new RGBColorSpaceOptions
        {
            Id     = Uid("rgb-gam"),
            Name   = "RGB Gam",
            Lin    = v => v * v,
            Gam    = v => Math.Sqrt(v),
            Coords = new Dictionary<string, CoordMetadata>
            {
                ["r"] = new() { Name = "R", Range = new CoordRange(0, 1) },
                ["g"] = new() { Name = "G", Range = new CoordRange(0, 1) },
                ["b"] = new() { Name = "B", Range = new CoordRange(0, 1) },
            },
        });

        var result = cs.GammaEncode([0.25, 1.0, 0.0]);
        Assert.Equal([0.5, 1.0, 0.0], result);
    }

    [Fact]
    public void RGBColorSpace_IsSubclassOfColorSpace()
    {
        var cs = new RGBColorSpace(new RGBColorSpaceOptions
        {
            Id     = Uid("rgb-sub"),
            Name   = "RGB Sub",
            Coords = new Dictionary<string, CoordMetadata>(),
        });
        Assert.IsAssignableFrom<ColorSpace>(cs);
    }

    [Fact]
    public void RGBColorSpace_CanBeRegistered()
    {
        var id = Uid("rgb-reg");
        var cs = new RGBColorSpace(new RGBColorSpaceOptions
        {
            Id     = id,
            Name   = "RGB Registered",
            Coords = new Dictionary<string, CoordMetadata>
            {
                ["r"] = new() { Name = "R", Range = new CoordRange(0, 1) },
            },
        });
        ColorSpace.Register(cs);
        Assert.Same(cs, ColorSpace.Get(id));
    }
}
