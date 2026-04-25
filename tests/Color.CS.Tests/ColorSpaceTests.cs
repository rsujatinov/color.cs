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

    // ── Ported from color.js ─────────────────────────────────────────────────
    // Source: https://github.com/color-js/color.js/blob/main/test/in_gamut.js

    /// <summary>
    /// Creates an HSL-like polar space whose three channels are (hue, saturation, lightness).
    /// ToBase divides s and l by 100 so they map to [0,1]; FromBase multiplies back.
    /// The hue channel has type=Angle and no range.
    /// </summary>
    private static ColorSpace MakeHslLike(string id, ColorSpace baseSpace) =>
        new(new ColorSpaceOptions
        {
            Id = id,
            Name = id,
            Base = baseSpace,
            Coords = new Dictionary<string, CoordMetadata>
            {
                ["h"] = new() { Name = "Hue",        Type = CoordType.Angle },
                ["s"] = new() { Name = "Saturation", Range = new CoordRange(0, 100) },
                ["l"] = new() { Name = "Lightness",  Range = new CoordRange(0, 100) },
            },
            // Linear mapping for testing: [h, s, l] → [h/360, s/100, l/100]
            ToBase   = c => [c[0] / 360.0, c[1] / 100.0, c[2] / 100.0],
            FromBase = c => [c[0] * 360.0, c[1] * 100.0, c[2] * 100.0],
        });

    /// <summary>
    /// Creates a polar space whose hue channel has an EXPLICIT range [0, 360] AND type=Angle,
    /// to verify that angle coords are excluded from gamut checking even when a range is present.
    /// Mirrors the "Angle coordinates should not be gamut checked" test in color.js in_gamut.js.
    /// </summary>
    private static ColorSpace MakePolarWithBoundedAngle(string id) =>
        new(new ColorSpaceOptions
        {
            Id = id,
            Name = id,
            Coords = new Dictionary<string, CoordMetadata>
            {
                // Angle coord WITH an explicit range — should still be skipped by InGamut.
                ["h"] = new() { Name = "Hue",        Type = CoordType.Angle, Range = new CoordRange(0, 360) },
                ["s"] = new() { Name = "Saturation", Range = new CoordRange(0, 100) },
                ["l"] = new() { Name = "Lightness",  Range = new CoordRange(0, 100) },
            },
        });

    // Ported: in_gamut.js — "Lab (unbounded color space)"
    // Large coordinates in an unbounded space are always in gamut.
    [Fact]
    public void Ported_InGamut_UnboundedSpace_AlwaysInGamut()
    {
        var lab = MakeUnbounded(Uid("lab-like"));
        Assert.True(lab.InGamut([1000.0, 1000.0]));
    }

    // Ported: in_gamut.js — "Angle coordinates should not be gamut checked"
    // A hue value of 720 is outside [0, 360] but angle coords are never range-checked.
    [Fact]
    public void Ported_InGamut_AngleCoordWithRange_NotGamutChecked()
    {
        // hue=720 would be out of the explicit [0,360] range, but it's an angle → passes.
        var cs = MakePolarWithBoundedAngle(Uid("polar-bounded-angle"));
        Assert.True(cs.InGamut([720.0, 50.0, 25.0]));
    }

    // Ported: in_gamut.js — "Angle coordinates should not be gamut checked"
    // Saturation IS range-checked, so an out-of-range saturation must fail.
    [Fact]
    public void Ported_InGamut_NonAngleCoordWithRange_IsGamutChecked()
    {
        var cs = MakePolarWithBoundedAngle(Uid("polar-bounded-sat"));
        Assert.False(cs.InGamut([180.0, 110.0, 50.0]));
    }

    // Ported: in_gamut.js — "HSL (polar space, defaults to the base space)"
    // An in-gamut polar-space colour is also in gamut when checked via its base.
    [Fact]
    public void Ported_InGamut_PolarSpace_InGamutDelegatesConversionToBase()
    {
        // sRGB [0, 1.0, 0.5] ← ToBase([0, 100, 50]) → [0/360, 1.0, 0.5] ✓
        var srgbLike = MakeLinear(Uid("hsl-base-ok"));
        var hslLike  = MakeHslLike(Uid("hsl-like-ok"), srgbLike);

        Assert.True(hslLike.InGamut([0.0, 100.0, 50.0]));
    }

    // Ported: in_gamut.js — "HSL (polar space, defaults to the base space)" (out-of-gamut case)
    // Saturation 101 maps to 1.01 in the base space, which is out of [0,1].
    [Fact]
    public void Ported_InGamut_PolarSpace_OutOfGamutDelegatesConversionToBase()
    {
        // sRGB [0, 1.01, 0.5] ← ToBase([0, 101, 50]) → [0, 1.01, 0.5] ✗
        var srgbLike = MakeLinear(Uid("hsl-base-bad"));
        var hslLike  = MakeHslLike(Uid("hsl-like-bad"), srgbLike);

        Assert.False(hslLike.InGamut([0.0, 101.0, 50.0]));
    }

    // Ported: in_gamut.js — "HSL (polar space, defaults to the base space)"
    // A display-P3-like colour outside sRGB gamut is out of the HSL-like gamut too.
    [Fact]
    public void Ported_InGamut_PolarSpace_OutOfBaseGamutIsOutOfGamut()
    {
        var srgbLike = MakeLinear(Uid("hsl-base-p3"));
        var hslLike  = MakeHslLike(Uid("hsl-like-p3"), srgbLike);

        // Passing coords that, after ToBase, give a value > 1.0 in one channel.
        // [0, 110, 50] → [0, 1.1, 0.5] — out of [0,1] for the base space.
        Assert.False(hslLike.InGamut([0.0, 110.0, 50.0]));
    }

    // Ported: ColorSpace.js — InGamut default epsilon matches color.js ε = 0.000075.
    // A value just inside (1 - epsilon) should pass; just outside (1 + 2*epsilon) should fail.
    [Fact]
    public void Ported_InGamut_DefaultEpsilonMatchesColorJs()
    {
        // 1.000075 is on the boundary (equal to max + epsilon) → pass.
        Assert.True(ColorSpace.Srgb.InGamut([0.5, 1.000075, 0.5]));
        // 1.0001 > 1 + 0.000075 → fail.
        Assert.False(ColorSpace.Srgb.InGamut([0.5, 1.0001, 0.5]));
    }

    // Ported: conversions.js — identity conversion (same space) returns equivalent coords.
    [Fact]
    public void Ported_Conversions_SameSpaceReturnsCopy()
    {
        double[] coords = [1.0, 0.0, 0.0];
        var result = ColorSpace.Srgb.To(ColorSpace.Srgb, coords);
        Assert.Equal(coords, result);
    }

    // Ported: conversions.js — multi-hop conversion through a common ancestor.
    // Mirrors the general "go up from source, go down to target via LCA" algorithm.
    [Fact]
    public void Ported_Conversions_MultiHop_ViaCommonAncestor()
    {
        // Build a 2-space tree:  root ← a (+100),  root ← b (*2)
        // Converting a → b:  (coords + 100) * 2
        var root = MakeLinear(Uid("conv-root-mh"));
        var a    = MakeLinear(Uid("conv-a-mh"), baseSpace: root,
            toBase:   c => c.Select(v => v + 100).ToArray(),
            fromBase: c => c.Select(v => v - 100).ToArray());
        var b    = MakeLinear(Uid("conv-b-mh"), baseSpace: root,
            toBase:   c => c.Select(v => v / 2).ToArray(),
            fromBase: c => c.Select(v => v * 2).ToArray());

        // a.To(b): ascend a→root (+100), descend root→b (*2)
        var result = a.To(b, [1.0, 2.0, 3.0]);
        Assert.Equal([(1 + 100) * 2.0, (2 + 100) * 2.0, (3 + 100) * 2.0], result);
    }
}
