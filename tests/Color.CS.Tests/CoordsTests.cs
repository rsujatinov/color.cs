namespace Color.CS.Tests;

/// <summary>
/// Coordinate reading / writing tests.
/// Ported from color.js <c>test/coords.js</c> and <c>tests/modifications.html</c>.
/// </summary>
/// <remarks>
/// Tests that require cross-space conversion (e.g. sRGB → LCH or sRGB → OKLCh)
/// are marked with <c>Skip</c> until built-in color-space conversions are implemented.
/// The constant <see cref="SkipConversions"/> documents the reason.
/// </remarks>
public sealed class CoordsTests
{
    // Cross-space tests are skipped until the built-in color spaces have
    // ToBase/FromBase conversion functions wired up.
    private const string SkipConversions =
        "Cross-space conversion not yet implemented for built-in spaces (sRGB↔LCH, sRGB↔OKLCh etc.)";

    private const double Epsilon = 0.005; // mirrors color.js epsilon

    // ── Test data (mirrors color.js coords.js data block) ─────────────────

    private static readonly Color Red      = new(ColorSpace.Srgb,  [1, 0, 0]);
    private static readonly Color Red50    = new(ColorSpace.Srgb,  [1, 0, 0], 0.5);
    private static readonly Color RedOklch = new(ColorSpace.Oklch, [0.6, 0.25, 30]);

    // ── Reading coordinates ────────────────────────────────────────────────

    /// <summary>color.coords</summary>
    [Fact]
    public void Read_Coords_ReturnsCurrentCoords()
    {
        Assert.Equal([1, 0, 0], (IEnumerable<double>)Red.Coords);
    }

    /// <summary>color.getAll() — same space</summary>
    [Fact]
    public void Read_GetAll_SameSpace_ReturnsCopy()
    {
        var result = Red.GetAll(ColorSpace.Srgb);
        Assert.Equal(new[] { 1.0, 0.0, 0.0 }, result);
    }

    /// <summary>color.getAll({space: 'oklch', precision: 1}) — cross-space, red → OKLCh ≈ [0.6, 0.3, 30]</summary>
    [Fact(Skip = SkipConversions)]
    public void Read_GetAll_OklchSpace_ReturnsConvertedCoords()
    {
        var result = Red.GetAll(ColorSpace.Oklch);
        Assert.Equal(0.6,  result[0], 1);
        Assert.Equal(0.3,  result[1], 1);
        Assert.Equal(30.0, result[2], 1);
    }

    /// <summary>color.alpha</summary>
    [Fact]
    public void Read_Alpha_ReturnsAlphaValue()
    {
        Assert.Equal(0.5, Red50.Alpha);
    }

    /// <summary>color.get('alpha')</summary>
    [Fact]
    public void Read_Get_Alpha_ReturnsAlpha()
    {
        Assert.Equal(0.5, Red50.Get("alpha"));
    }

    /// <summary>color.coords[1] == 0</summary>
    [Fact]
    public void Read_CoordsIndex_ReturnsCorrectCoord()
    {
        Assert.Equal(0.0, Red.Coords[1]);
    }

    /// <summary>color.get(coordId) — bare coord name on same space (oklch "h" → 30)</summary>
    [Fact]
    public void Read_Get_BareCoordName_SameSpace()
    {
        // color.js uses short ids ("h"); C# spaces use full names ("hue")
        Assert.Equal(30.0, RedOklch.Get("hue"), Epsilon);
    }

    /// <summary>color.get("oklch.hue") on sRGB red → 29.23°</summary>
    [Fact(Skip = SkipConversions)]
    public void Read_Get_AbsoluteRef_CrossSpace_ReturnsHue()
    {
        Assert.Equal(29.23, Red.Get("oklch.hue"), Epsilon);
    }

    // ── Writing coordinates ────────────────────────────────────────────────

    /// <summary>color.setAll([1, 0, 1]) — same-space array replacement</summary>
    [Fact]
    public void Write_SetAll_SameSpaceCoords_UpdatesAll()
    {
        var color   = new Color(ColorSpace.Srgb, [0, 1, 0]);
        // C# SetAll takes a dictionary; replicate same-space bulk update
        var updated = color.SetAll(new Dictionary<string, object>
        {
            ["red"]  = 1.0,
            ["green"] = 0.0,
            ["blue"]  = 1.0,
        });
        Assert.Equal([1.0, 0.0, 1.0], (IEnumerable<double>)updated.Coords);
    }

    /// <summary>color.setAll("srgb", [1, 0, 1]) on oklch → OKLCh conversion of magenta
    /// expect ≈ [0.7017, 0.3225, 328.36]</summary>
    [Fact(Skip = SkipConversions)]
    public void Write_SetAll_CrossSpace_ConvertsBackToOriginalSpace()
    {
        // Start in OKLCh, set coords as if they were sRGB [1,0,1] (magenta)
        // Expected result: same color expressed in OKLCh
        var color = RedOklch.Clone();
        var updated = color.SetAll(new Dictionary<string, object>
        {
            // Setting all sRGB channels to [1,0,1] (magenta) then back to OKLCh
            ["srgb.red"]   = 1.0,
            ["srgb.green"] = 0.0,
            ["srgb.blue"]  = 1.0,
        });
        Assert.Equal(0.7017, updated.Coords[0], Epsilon);
        Assert.Equal(0.3225, updated.Coords[1], Epsilon);
        Assert.Equal(328.36, updated.Coords[2], Epsilon);
    }

    /// <summary>color.setAll(newCoords, alpha) — coords + alpha update</summary>
    [Fact]
    public void Write_SetAll_WithAlpha_UpdatesCoordsAndAlpha()
    {
        var color   = new Color(ColorSpace.Srgb, [0, 1, 0]);
        var updated = color
            .SetAll(new Dictionary<string, object>
            {
                ["red"]   = 1.0,
                ["green"] = 0.0,
                ["blue"]  = 1.0,
                ["alpha"] = 0.5,
            });
        Assert.Equal([1.0, 0.0, 1.0], (IEnumerable<double>)updated.Coords);
        Assert.Equal(0.5, updated.Alpha);
    }

    /// <summary>color.set('alpha', value)</summary>
    [Fact]
    public void Write_Set_Alpha_UpdatesAlpha()
    {
        var color   = Red.Clone();
        var updated = color.Set("alpha", 0.5);
        Assert.Equal(0.5, updated.Alpha);
        // original unchanged
        Assert.Equal(1.0, color.Alpha);
    }

    /// <summary>color.set("alpha", value) is immutable</summary>
    [Fact]
    public void Write_Set_Alpha_IsImmutable()
    {
        var color = Red.Clone();
        _ = color.Set("alpha", 0.5);
        Assert.Equal(1.0, color.Alpha);
    }

    /// <summary>color.set("lch.c", 13) on sRGB, then color.get("lch.c") == 13</summary>
    [Fact(Skip = SkipConversions)]
    public void Write_Set_CrossSpace_ByValue_RoundTrips()
    {
        var color   = Red.Clone();
        // C# uses "lch.chroma" not "lch.c"
        var updated = color.Set("lch.chroma", 13);
        Assert.Equal(13.0, updated.Get("lch.chroma"), Epsilon);
    }

    /// <summary>color.set("c", 13) on same-space (lch) color</summary>
    [Fact]
    public void Write_Set_SameSpace_BareCoordName_UpdatesCoord()
    {
        // color.js uses short ids ("c"); C# LCH uses "chroma"
        var color   = new Color(ColorSpace.Lch, [50.0, 20.0, 180.0]);
        var updated = color.Set("chroma", 13.0);
        Assert.Equal(13.0, updated.Coords[1]); // index 1 = chroma
    }

    /// <summary>color.set("otherSpace.coordId", value) — absolute cross-space reference</summary>
    [Fact(Skip = SkipConversions)]
    public void Write_Set_AbsoluteRef_CrossSpace_UpdatesCoord()
    {
        var color   = Red.Clone();
        var updated = color.Set("lch.chroma", 13);
        Assert.Equal(13.0, updated.Get("lch.chroma"), Epsilon);
    }

    /// <summary>color.set("c", c => c * 1.2) on lch color (same space)</summary>
    [Fact]
    public void Write_Set_SameSpace_Transform_AppliesFunction()
    {
        // color.js: new Color("slategray").to("lch").set("c", c => c * 1.2) → 13.480970445148008
        // We don't have cross-space conversion, so we set up a LCH color with the
        // known slategray chroma (≈ 11.234…) and apply the same transform.
        const double slategrayChroma = 11.234142037623344; // known from color.js
        var color   = new Color(ColorSpace.Lch, [52.697, slategrayChroma, 253.0]);
        // color.js uses "c"; C# uses "chroma"
        var updated = color.Set("chroma", c => c * 1.2);
        Assert.Equal(slategrayChroma * 1.2, updated.Coords[1], Epsilon);
    }

    /// <summary>
    /// color.set(object_with_coords) — SetAll with multiple same-space refs
    /// Mirrors: color.set({"lch.c": 13, "lch.l": 40})
    /// Cross-space version is skipped; same-space version runs.
    /// </summary>
    [Fact]
    public void Write_SetAll_SameSpace_MultipleCoords()
    {
        // Use LCH color so refs are same-space; C# uses "lightness"/"chroma" not "l"/"c"
        var color   = new Color(ColorSpace.Lch, [80.0, 30.0, 180.0]);
        var updated = color.SetAll(new Dictionary<string, object>
        {
            ["lch.chroma"]    = 13.0,
            ["lch.lightness"] = 40.0,
        });
        Assert.Equal(13.0, updated.Get("lch.chroma"),    Epsilon);
        Assert.Equal(40.0, updated.Get("lch.lightness"), Epsilon);
    }

    /// <summary>
    /// color.set({"lch.c": 13, "lch.l": 40}) on sRGB (cross-space)
    /// </summary>
    [Fact(Skip = SkipConversions)]
    public void Write_SetAll_CrossSpace_MultipleCoords()
    {
        var color   = Red.Clone();
        var updated = color.SetAll(new Dictionary<string, object>
        {
            ["lch.chroma"]    = 13.0,
            ["lch.lightness"] = 40.0,
        });
        Assert.Equal(13.0, updated.Get("lch.chroma"),    Epsilon);
        Assert.Equal(40.0, updated.Get("lch.lightness"), Epsilon);
    }

    /// <summary>
    /// color.set(object_with_coords_and_alpha) — SetAll with coord + alpha
    /// Mirrors: color.set({"lch.c": 13, alpha: 0.5})
    /// Cross-space for the LCH coord, but alpha should work.
    /// </summary>
    [Fact(Skip = SkipConversions)]
    public void Write_SetAll_CrossSpace_CoordsAndAlpha()
    {
        var color   = Red.Clone();
        var updated = color.SetAll(new Dictionary<string, object>
        {
            ["lch.chroma"] = 13.0,
            ["alpha"]      = 0.5,
        });
        Assert.Equal(13.0, updated.Get("lch.chroma"), Epsilon);
        Assert.Equal(0.5,  updated.Alpha);
    }

    /// <summary>
    /// SetAll with alpha key only (same-space, no conversion needed).
    /// </summary>
    [Fact]
    public void Write_SetAll_AlphaKey_UpdatesAlpha()
    {
        var color   = Red.Clone();
        var updated = color.SetAll(new Dictionary<string, object>
        {
            ["alpha"] = 0.5,
        });
        Assert.Equal(0.5, updated.Alpha);
    }

    // ── Modifications (ported from tests/modifications.html) ──────────────

    /// <summary>color.set("lch.c", 13) on slategray, read back lch.c == 13</summary>
    [Fact(Skip = SkipConversions)]
    public void Modifications_Slategray_SetLchChroma_13()
    {
        var color   = new Color("slategray");
        var updated = color.Set("lch.chroma", 13);
        Assert.Equal(13.0, updated.Get("lch.chroma"), Epsilon);
    }

    /// <summary>color.set("c", 13) on slategray.to("lch"), read back lch.c == 13</summary>
    [Fact(Skip = SkipConversions)]
    public void Modifications_SlategrayInLch_SetChroma_13()
    {
        // color.js: new Color("slategray").to("lch").set("c", 13) → lch.chroma == 13
        // Requires sRGB→LCH conversion (.to("lch") equivalent).
    }

    /// <summary>
    /// color.set("c", c => c * 1.2) on slategray in LCH → 13.480970445148008
    /// The exact value from color.js conversions.
    /// </summary>
    [Fact(Skip = SkipConversions)]
    public void Modifications_SlategrayLch_Chroma_Times_1_2()
    {
        var color   = new Color("slategray");
        var updated = color.Set("lch.chroma", c => c * 1.2);
        Assert.Equal(13.480970445148008, updated.Get("lch.chroma"), Epsilon);
    }

    // ── Equals (ported from color.js equals.js) ───────────────────────────

    /// <summary>Two identical colors are equal.</summary>
    [Fact]
    public void Equals_IdenticalColors_ReturnsTrue()
    {
        var a = new Color(ColorSpace.Srgb, [1, 0, 0]);
        var b = new Color(ColorSpace.Srgb, [1, 0, 0]);
        Assert.True(a.Equals(b));
    }

    /// <summary>Same space, different coords → not equal.</summary>
    [Fact]
    public void Equals_DifferentCoords_ReturnsFalse()
    {
        var a = new Color(ColorSpace.Srgb, [1, 0, 0]);
        var b = new Color(ColorSpace.Srgb, [0, 1, 0]);
        Assert.False(a.Equals(b));
    }

    /// <summary>Different spaces → not equal even if coord values happen to be the same.</summary>
    [Fact]
    public void Equals_DifferentSpaces_ReturnsFalse()
    {
        var a = new Color(ColorSpace.Srgb,  [0.6, 0.25, 30]);
        var b = new Color(ColorSpace.Oklch, [0.6, 0.25, 30]);
        Assert.False(a.Equals(b));
    }

    /// <summary>Different alpha → not equal.</summary>
    [Fact]
    public void Equals_DifferentAlpha_ReturnsFalse()
    {
        var a = new Color(ColorSpace.Srgb, [1, 0, 0], 1.0);
        var b = new Color(ColorSpace.Srgb, [1, 0, 0], 0.5);
        Assert.False(a.Equals(b));
    }

    /// <summary>Both coords are NaN (CSS none) → equal.</summary>
    [Fact]
    public void Equals_BothCoordsNaN_ReturnsTrue()
    {
        var a = new Color(ColorSpace.Oklch, [0.5, double.NaN, 30]);
        var b = new Color(ColorSpace.Oklch, [0.5, double.NaN, 30]);
        Assert.True(a.Equals(b));
    }

    /// <summary>One NaN, one real value → not equal.</summary>
    [Fact]
    public void Equals_OneNaNOneReal_ReturnsFalse()
    {
        var a = new Color(ColorSpace.Oklch, [0.5, double.NaN, 30]);
        var b = new Color(ColorSpace.Oklch, [0.5, 0.0,        30]);
        Assert.False(a.Equals(b));
    }

    /// <summary>Both alpha NaN → equal.</summary>
    [Fact]
    public void Equals_BothAlphaNaN_ReturnsTrue()
    {
        var a = new Color(ColorSpace.Srgb, [1, 0, 0], double.NaN);
        var b = new Color(ColorSpace.Srgb, [1, 0, 0], double.NaN);
        Assert.True(a.Equals(b));
    }

    /// <summary>ParseMeta is not considered in equality.</summary>
    [Fact]
    public void Equals_IgnoresParseMeta()
    {
        var a = new Color("rgb(100% 0% 0%)"); // has ParseMeta
        var b = new Color(ColorSpace.Srgb, [1, 0, 0]); // no ParseMeta
        Assert.True(a.Equals(b));
    }
}
