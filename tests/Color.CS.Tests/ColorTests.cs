using System.Collections.Immutable;
using System.Globalization;

namespace Color.CS.Tests;

public sealed class ColorTests
{
    // ──────────────────────────────────────────────────────────────────────
    // Existing baseline tests
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Color_HasCorrectAlphaDefault()
    {
        var color = new Color(ColorSpace.Srgb, [1.0, 0.0, 0.0]);
        Assert.Equal(1.0, color.Alpha);
    }

    [Fact]
    public void Color_With_UpdatesAlpha()
    {
        var color = new Color(ColorSpace.Srgb, [1.0, 0.0, 0.0]);
        var updated = color.With(alpha: 0.5);
        Assert.Equal(0.5, updated.Alpha);
    }

    [Fact]
    public void Color_With_UpdatesCoords()
    {
        var color = new Color(ColorSpace.Srgb, [1.0, 0.0, 0.0]);
        var updated = color.With(coords: [0.0, 1.0, 0.0]);
        Assert.Equal([0.0, 1.0, 0.0], (IEnumerable<double>)updated.Coords);
    }

    [Fact]
    public void ColorSpace_Srgb_HasCorrectId()
    {
        Assert.Equal("srgb", ColorSpace.Srgb.Id);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Alpha clamping
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(-0.5, 0.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(0.5, 0.5)]
    [InlineData(1.0, 1.0)]
    [InlineData(1.5, 1.0)]
    public void Color_Alpha_IsClamped(double input, double expected)
    {
        var color = new Color(ColorSpace.Srgb, [1.0, 0.0, 0.0], input);
        Assert.Equal(expected, color.Alpha);
    }

    [Fact]
    public void Color_Alpha_NaN_IsPreserved()
    {
        var color = new Color(ColorSpace.Srgb, [1.0, 0.0, 0.0], double.NaN);
        Assert.True(double.IsNaN(color.Alpha));
    }

    [Fact]
    public void Color_With_Alpha_ClampsNewValue()
    {
        var color = new Color(ColorSpace.Srgb, [1.0, 0.0, 0.0]);
        var updated = color.With(alpha: 2.0);
        Assert.Equal(1.0, updated.Alpha);
    }

    // ──────────────────────────────────────────────────────────────────────
    // double[] constructor
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Color_DoubleArrayCtor_StoresCoords()
    {
        double[] arr = [0.2, 0.4, 0.6];
        var color = new Color(ColorSpace.Srgb, arr);
        Assert.Equal([0.2, 0.4, 0.6], (IEnumerable<double>)color.Coords);
    }

    [Fact]
    public void Color_DoubleArrayCtor_MutatingOriginalDoesNotAffectColor()
    {
        double[] arr = [0.2, 0.4, 0.6];
        var color = new Color(ColorSpace.Srgb, arr);
        arr[0] = 0.9;
        Assert.Equal(0.2, color.Coords[0]);
    }

    // ──────────────────────────────────────────────────────────────────────
    // ReadOnlySpan<double> constructor
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Color_SpanCtor_StoresCoords()
    {
        ReadOnlySpan<double> span = [0.1, 0.5, 0.9];
        var color = new Color(ColorSpace.Srgb, span);
        Assert.Equal([0.1, 0.5, 0.9], (IEnumerable<double>)color.Coords);
    }

    // ──────────────────────────────────────────────────────────────────────
    // CSS string constructor
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Color_CssCtor_ParsesBasicColorFunction()
    {
        var color = new Color("color(srgb 1 0 0)");
        Assert.Equal(ColorSpace.Srgb, color.Space);
        Assert.Equal([1.0, 0.0, 0.0], (IEnumerable<double>)color.Coords);
        Assert.Equal(1.0, color.Alpha);
    }

    [Fact]
    public void Color_CssCtor_ParsesColorFunctionWithAlpha()
    {
        var color = new Color("color(srgb 1 0 0 / 0.5)");
        Assert.Equal(0.5, color.Alpha);
    }

    [Fact]
    public void Color_CssCtor_ParsesNoneKeyword()
    {
        var color = new Color("color(srgb none 0 0)");
        Assert.True(double.IsNaN(color.Coords[0]));
    }

    [Fact]
    public void Color_CssCtor_ThrowsOnUnsupportedFormat()
    {
        Assert.Throws<FormatException>(() => new Color("rgb(255, 0, 0)"));
    }

    [Fact]
    public void Color_CssCtor_ThrowsOnUnknownSpace()
    {
        Assert.Throws<FormatException>(() => new Color("color(xyz-unknown 1 0 0)"));
    }

    // ──────────────────────────────────────────────────────────────────────
    // PlainColorObject constructor
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Color_PlainColorObjectCtor_StoresAllFields()
    {
        var pco = new PlainColorObject("srgb", [0.3, 0.6, 0.9], 0.8);
        var color = new Color(pco);
        Assert.Equal(ColorSpace.Srgb, color.Space);
        Assert.Equal([0.3, 0.6, 0.9], (IEnumerable<double>)color.Coords);
        Assert.Equal(0.8, color.Alpha);
    }

    [Fact]
    public void Color_PlainColorObjectCtor_ThrowsOnUnknownSpace()
    {
        var pco = new PlainColorObject("not-a-space", [1.0, 0.0, 0.0]);
        Assert.Throws<ArgumentException>(() => new Color(pco));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Clone
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Color_Clone_ReturnsEqualButDistinctInstance()
    {
        var color = new Color(ColorSpace.Srgb, [1.0, 0.0, 0.0], 0.7);
        var clone = color.Clone();
        Assert.Equal(color, clone);
        Assert.False(ReferenceEquals(color, clone));
    }

    // ──────────────────────────────────────────────────────────────────────
    // ToJson / ToString
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Color_ToJson_ProducesExpectedString()
    {
        var color = new Color(ColorSpace.Srgb, [1.0, 0.0, 0.0], 1.0);
        Assert.Equal("""{"spaceId":"srgb","coords":[1,0,0],"alpha":1}""", color.ToJson());
    }

    [Fact]
    public void Color_ToJson_RepresentsNoneAsNull()
    {
        var color = new Color(ColorSpace.Srgb, [double.NaN, 0.0, 0.0], double.NaN);
        Assert.Equal("""{"spaceId":"srgb","coords":[null,0,0],"alpha":null}""", color.ToJson());
    }

    [Fact]
    public void Color_ToString_DelegatesToToJson()
    {
        var color = new Color(ColorSpace.Srgb, [0.5, 0.5, 0.5], 0.5);
        Assert.Equal(color.ToJson(), color.ToString());
    }

    // ──────────────────────────────────────────────────────────────────────
    // Named coordinate indexer
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Color_Indexer_ReturnsCorrectChannelValue()
    {
        var color = new Color(ColorSpace.Srgb, [1.0, 0.5, 0.25]);
        Assert.Equal(1.0, color["red"]);
        Assert.Equal(0.5, color["green"]);
        Assert.Equal(0.25, color["blue"]);
    }

    [Fact]
    public void Color_Indexer_ThrowsOnUnknownChannel()
    {
        var color = new Color(ColorSpace.Srgb, [1.0, 0.0, 0.0]);
        Assert.Throws<ArgumentException>(() => color["lightness"]);
    }

    // ──────────────────────────────────────────────────────────────────────
    // ColorSpace helpers
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ColorSpace_FromId_ReturnsSrgb()
    {
        Assert.Equal(ColorSpace.Srgb, ColorSpace.FromId("srgb"));
    }

    [Fact]
    public void ColorSpace_FromId_ReturnsNullForUnknown()
    {
        Assert.Null(ColorSpace.FromId("unknown-space"));
    }

    [Fact]
    public void ColorSpace_CoordIndex_ReturnsCorrectIndices()
    {
        Assert.Equal(0, ColorSpace.Srgb.CoordIndex("red"));
        Assert.Equal(1, ColorSpace.Srgb.CoordIndex("green"));
        Assert.Equal(2, ColorSpace.Srgb.CoordIndex("blue"));
        Assert.Equal(-1, ColorSpace.Srgb.CoordIndex("lightness"));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Static Color.Get / Color.Try
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Color_Get_ReturnsSameInstanceForColor()
    {
        var color = new Color(ColorSpace.Srgb, [1.0, 0.0, 0.0]);
        Assert.Same(color, Color.Get(color));
    }

    [Fact]
    public void Color_Get_ConstructsFromString()
    {
        var color = Color.Get("color(srgb 0 1 0)");
        Assert.Equal(ColorSpace.Srgb, color.Space);
        Assert.Equal(0.0, color.Coords[0]);
        Assert.Equal(1.0, color.Coords[1]);
    }

    [Fact]
    public void Color_Get_ConstructsFromPlainColorObject()
    {
        var pco = new PlainColorObject("srgb", [0.0, 0.0, 1.0]);
        var color = Color.Get(pco);
        Assert.Equal(ColorSpace.Srgb, color.Space);
        Assert.Equal(1.0, color.Coords[2]);
    }

    [Fact]
    public void Color_Get_ThrowsOnUnsupportedType()
    {
        Assert.Throws<ArgumentException>(() => Color.Get(42));
    }

    [Fact]
    public void Color_Try_ReturnsNullOnFailure()
    {
        Assert.Null(Color.Try("not a color string"));
    }

    [Fact]
    public void Color_Try_ReturnsColorOnSuccess()
    {
        var color = Color.Try("color(srgb 1 0 0)");
        Assert.NotNull(color);
        Assert.Equal(ColorSpace.Srgb, color.Space);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Ported from color.js — construct.js
    // https://github.com/color-js/color.js/blob/main/test/construct.js
    // ──────────────────────────────────────────────────────────────────────

    // new Color({spaceId, coords}) — tests the PlainColorObject overload
    [Fact]
    public void Ported_Construct_PlainObject_SpaceAndCoords()
    {
        var color = new Color(new PlainColorObject("srgb", [0.0, 1.0, 0.0]));
        Assert.Equal("""{"spaceId":"srgb","coords":[0,1,0],"alpha":1}""", color.ToJson());
    }

    // new Color({spaceId, coords, alpha})
    [Fact]
    public void Ported_Construct_PlainObject_WithAlpha()
    {
        var color = new Color(new PlainColorObject("srgb", [0.0, 1.0, 0.0], 0.5));
        Assert.Equal("""{"spaceId":"srgb","coords":[0,1,0],"alpha":0.5}""", color.ToJson());
    }

    // new Color({spaceId, coords, alpha}), clamp alpha > 1 → 1
    [Fact]
    public void Ported_Construct_PlainObject_ClampAlphaAboveOne()
    {
        var color = new Color(new PlainColorObject("srgb", [0.0, 1.0, 0.0], 1000.0));
        Assert.Equal(1.0, color.Alpha);
    }

    // new Color({spaceId, coords, alpha: NaN}) — NaN is the "none" sentinel
    [Fact]
    public void Ported_Construct_PlainObject_NanAlpha()
    {
        var color = new Color(new PlainColorObject("srgb", [0.0, 1.0, 0.0], double.NaN));
        Assert.True(double.IsNaN(color.Alpha));
        Assert.Contains("\"alpha\":null", color.ToJson());
    }

    // ──────────────────────────────────────────────────────────────────────
    // Ported from color.js — coords.js
    // https://github.com/color-js/color.js/blob/main/test/coords.js
    // ──────────────────────────────────────────────────────────────────────

    // color.coords → [1, 0, 0]
    [Fact]
    public void Ported_Coords_CoordsProperty()
    {
        var red = new Color(ColorSpace.Srgb, [1.0, 0.0, 0.0]);
        Assert.Equal([1.0, 0.0, 0.0], (IEnumerable<double>)red.Coords);
    }

    // color.alpha → 0.5
    [Fact]
    public void Ported_Coords_AlphaProperty()
    {
        var red50 = new Color(ColorSpace.Srgb, [1.0, 0.0, 0.0], 0.5);
        Assert.Equal(0.5, red50.Alpha);
    }

    // color.coords[index]
    [Fact]
    public void Ported_Coords_IndexAccess()
    {
        var red = new Color(ColorSpace.Srgb, [1.0, 0.0, 0.0]);
        Assert.Equal(0.0, red.Coords[1]);
    }

    // color.coordId — named channel accessor (maps to our string indexer)
    [Fact]
    public void Ported_Coords_NamedChannelAccess()
    {
        // In color.js: color.h for oklch; we use color["red"] for srgb
        var red = new Color(ColorSpace.Srgb, [1.0, 0.5, 0.25]);
        Assert.Equal(1.0, red["red"]);
        Assert.Equal(0.5, red["green"]);
        Assert.Equal(0.25, red["blue"]);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Ported from color.js — parse.js → color() function tests
    // https://github.com/color-js/color.js/blob/main/test/parse.js
    // ──────────────────────────────────────────────────────────────────────

    // color(srgb 0 1 .5) — decimal without leading zero
    [Fact]
    public void Ported_Parse_ColorFunction_DecimalWithoutLeadingZero()
    {
        var color = new Color("color(srgb 0 1 .5)");
        Assert.Equal("""{"spaceId":"srgb","coords":[0,1,0.5],"alpha":1}""", color.ToJson());
    }

    // color(srgb none 0 0) — none coordinate (maps to null in JSON, NaN in C#)
    [Fact]
    public void Ported_Parse_ColorFunction_NoneCoordinate()
    {
        var color = new Color("color(srgb none 0 0)");
        Assert.True(double.IsNaN(color.Coords[0]));
        Assert.Contains("\"coords\":[null,", color.ToJson());
    }

    // color(srgb 0 1 0 / 0.5) — with transparency
    [Fact]
    public void Ported_Parse_ColorFunction_WithTransparency()
    {
        var color = new Color("color(srgb 0 1 0 / .5)");
        Assert.Equal("""{"spaceId":"srgb","coords":[0,1,0],"alpha":0.5}""", color.ToJson());
    }

    // color(srgb) — no arguments (spaceId only) → throws
    [Fact]
    public void Ported_Parse_ColorFunction_NoArguments_Throws()
    {
        Assert.Throws<FormatException>(() => new Color("color(srgb)"));
    }

    // color(srgb / .5) — spaceId + alpha only, no coords → throws
    [Fact]
    public void Ported_Parse_ColorFunction_AlphaOnlyNoCoords_Throws()
    {
        Assert.Throws<FormatException>(() => new Color("color(srgb / .5)"));
    }

    // color(srgb 1) — fewer arguments than channels → throws
    [Fact]
    public void Ported_Parse_ColorFunction_FewerArguments_Throws()
    {
        Assert.Throws<FormatException>(() => new Color("color(srgb 1)"));
    }

    // color(srgb 1 / .5) — fewer arguments + alpha → throws
    [Fact]
    public void Ported_Parse_ColorFunction_FewerArgumentsWithAlpha_Throws()
    {
        Assert.Throws<FormatException>(() => new Color("color(srgb 1 / .5)"));
    }

    // color(srgb 1 1 1 1) — more arguments than channels → throws
    [Fact]
    public void Ported_Parse_ColorFunction_MoreArguments_Throws()
    {
        Assert.Throws<FormatException>(() => new Color("color(srgb 1 1 1 1)"));
    }

    // color(srgb 1 1 1 1 / .5) — more arguments + alpha → throws
    [Fact]
    public void Ported_Parse_ColorFunction_MoreArgumentsWithAlpha_Throws()
    {
        Assert.Throws<FormatException>(() => new Color("color(srgb 1 1 1 1 / .5)"));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Ported from color.js — parse.js → "Different number formats"
    // https://github.com/color-js/color.js/blob/main/test/parse.js
    // ──────────────────────────────────────────────────────────────────────

    // color(srgb +0.9 0 0) — explicit leading plus sign
    [Fact]
    public void Ported_Parse_NumberFormat_LeadingPlus()
    {
        var color = new Color("color(srgb +0.9 0 0)");
        Assert.Equal(0.9, color.Coords[0], precision: 10);
    }

    // color(srgb .9 0 0) — no leading zero before decimal point
    [Fact]
    public void Ported_Parse_NumberFormat_NoLeadingZero()
    {
        var color = new Color("color(srgb .9 0 0)");
        Assert.Equal(0.9, color.Coords[0], precision: 10);
    }

    // color(srgb 9e-1 0 0) — scientific notation, lowercase 'e'
    [Fact]
    public void Ported_Parse_NumberFormat_ScientificLowercase()
    {
        var color = new Color("color(srgb 9e-1 0 0)");
        Assert.Equal(0.9, color.Coords[0], precision: 10);
    }

    // color(srgb 9E-1 0 0) — scientific notation, uppercase 'E'
    [Fact]
    public void Ported_Parse_NumberFormat_ScientificUppercase()
    {
        var color = new Color("color(srgb 9E-1 0 0)");
        Assert.Equal(0.9, color.Coords[0], precision: 10);
    }

    // color(srgb 0.09e+1 0 0) — scientific notation with explicit positive exponent
    [Fact]
    public void Ported_Parse_NumberFormat_ScientificExplicitPositiveExponent()
    {
        var color = new Color("color(srgb 0.09e+1 0 0)");
        Assert.Equal(0.9, color.Coords[0], precision: 10);
    }

    // color(srgb 0.09e1 0 0) — scientific notation without exponent sign
    [Fact]
    public void Ported_Parse_NumberFormat_ScientificNoExponentSign()
    {
        var color = new Color("color(srgb 0.09e1 0 0)");
        Assert.Equal(0.9, color.Coords[0], precision: 10);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Ported from color.js — parse.js → color() percentage notation
    // https://github.com/color-js/color.js/blob/main/test/parse.js
    // ──────────────────────────────────────────────────────────────────────

    // color(srgb 0 100% 50%) — percentage coords: 100% → 1.0, 50% → 0.5
    [Fact]
    public void Ported_Parse_ColorFunction_PercentageCoords()
    {
        var color = new Color("color(srgb 0 100% 50%)");
        Assert.Equal("""{"spaceId":"srgb","coords":[0,1,0.5],"alpha":1}""", color.ToJson());
    }

    // color(srgb 0 100% 50% / 0.5) — percentage coords with alpha
    [Fact]
    public void Ported_Parse_ColorFunction_PercentageCoordsWithAlpha()
    {
        var color = new Color("color(srgb 0 100% 50% / 0.5)");
        Assert.Equal(0.0, color.Coords[0], precision: 10);
        Assert.Equal(1.0, color.Coords[1], precision: 10);
        Assert.Equal(0.5, color.Coords[2], precision: 10);
        Assert.Equal(0.5, color.Alpha);
    }

    // color(srgb 0 1 0 / none) — none alpha in color() maps to NaN / null in JSON
    [Fact]
    public void Ported_Parse_ColorFunction_NoneAlpha()
    {
        var color = new Color("color(srgb 0 1 0 / none)");
        Assert.True(double.IsNaN(color.Alpha));
        Assert.Contains("\"alpha\":null", color.ToJson());
    }
}
