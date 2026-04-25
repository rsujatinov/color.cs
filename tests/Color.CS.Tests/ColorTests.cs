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
        Assert.Throws<FormatException>(() => new Color("notafunc(1 0 0)"));
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

    // ──────────────────────────────────────────────────────────────────────
    // Hex color parsing
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Hex6_ProducesCorrectSrgb()
    {
        var color = new Color("#ff0000");
        Assert.Equal(ColorSpace.Srgb, color.Space);
        Assert.Equal(1.0, color.Coords[0], precision: 10);
        Assert.Equal(0.0, color.Coords[1], precision: 10);
        Assert.Equal(0.0, color.Coords[2], precision: 10);
        Assert.Equal(1.0, color.Alpha);
    }

    [Fact]
    public void Parse_Hex3_ExpandsAndProducesCorrectSrgb()
    {
        var color = new Color("#f00");
        Assert.Equal(ColorSpace.Srgb, color.Space);
        Assert.Equal(1.0, color.Coords[0], precision: 10);
        Assert.Equal(0.0, color.Coords[1], precision: 10);
        Assert.Equal(0.0, color.Coords[2], precision: 10);
    }

    [Fact]
    public void Parse_Hex8_ParsesAlphaChannel()
    {
        var color = new Color("#ff000080");
        Assert.Equal(ColorSpace.Srgb, color.Space);
        Assert.Equal(1.0,  color.Coords[0], precision: 5);
        Assert.Equal(0.0,  color.Coords[1], precision: 5);
        Assert.Equal(0.0,  color.Coords[2], precision: 5);
        // 0x80 = 128, 128/255 ≈ 0.502
        Assert.Equal(128.0 / 255.0, color.Alpha, precision: 5);
    }

    [Fact]
    public void Parse_Hex4_ExpandsAndParsesAlpha()
    {
        // #f00f → #ff0000ff → fully opaque red
        var color = new Color("#f00f");
        Assert.Equal(1.0, color.Coords[0], precision: 10);
        Assert.Equal(0.0, color.Coords[1], precision: 10);
        Assert.Equal(0.0, color.Coords[2], precision: 10);
        Assert.Equal(1.0, color.Alpha,     precision: 10);
    }

    [Fact]
    public void Parse_Hex_StoresFormatId()
    {
        var color = new Color("#ff0000");
        Assert.Equal("hex", color.ParseMeta?.FormatId);
    }

    [Theory]
    [InlineData("#gg0000")]
    [InlineData("#12")]
    [InlineData("#123456789")]
    public void Parse_Hex_ThrowsOnInvalidInput(string input)
    {
        Assert.Throws<FormatException>(() => new Color(input));
    }

    // ──────────────────────────────────────────────────────────────────────
    // rgb() / rgba() parsing
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Rgb_CommaSeparated()
    {
        var color = new Color("rgb(255, 0, 0)");
        Assert.Equal(ColorSpace.Srgb, color.Space);
        Assert.Equal(1.0, color.Coords[0], precision: 10);
        Assert.Equal(0.0, color.Coords[1], precision: 10);
        Assert.Equal(0.0, color.Coords[2], precision: 10);
        Assert.Equal(1.0, color.Alpha);
    }

    [Fact]
    public void Parse_Rgb_SpaceSeparated()
    {
        var color = new Color("rgb(255 0 0)");
        Assert.Equal(1.0, color.Coords[0], precision: 10);
        Assert.Equal(0.0, color.Coords[1], precision: 10);
    }

    [Fact]
    public void Parse_Rgb_PercentageValues()
    {
        var color = new Color("rgb(100%, 0%, 50%)");
        Assert.Equal(1.0,  color.Coords[0], precision: 10);
        Assert.Equal(0.0,  color.Coords[1], precision: 10);
        Assert.Equal(0.5,  color.Coords[2], precision: 10);
    }

    [Fact]
    public void Parse_Rgb_SpaceSyntaxWithAlpha()
    {
        var color = new Color("rgb(255 0 0 / 0.5)");
        Assert.Equal(1.0, color.Coords[0], precision: 10);
        Assert.Equal(0.5, color.Alpha);
    }

    [Fact]
    public void Parse_Rgba_CommaSeparatedWithAlpha()
    {
        var color = new Color("rgba(0, 255, 0, 0.75)");
        Assert.Equal(0.0,  color.Coords[0], precision: 10);
        Assert.Equal(1.0,  color.Coords[1], precision: 10);
        Assert.Equal(0.0,  color.Coords[2], precision: 10);
        Assert.Equal(0.75, color.Alpha);
    }

    [Fact]
    public void Parse_Rgb_NoneCoordinate()
    {
        var color = new Color("rgb(none 0 0)");
        Assert.True(double.IsNaN(color.Coords[0]));
    }

    [Fact]
    public void Parse_Rgb_StoresFormatId()
    {
        Assert.Equal("rgb",  new Color("rgb(255 0 0)").ParseMeta?.FormatId);
        Assert.Equal("rgba", new Color("rgba(255, 0, 0, 1)").ParseMeta?.FormatId);
    }

    // ──────────────────────────────────────────────────────────────────────
    // hsl() / hsla() parsing
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Hsl_CommaSeparated()
    {
        var color = new Color("hsl(180, 100%, 50%)");
        Assert.Equal(ColorSpace.Hsl, color.Space);
        Assert.Equal(180.0, color.Coords[0], precision: 10);
        Assert.Equal(100.0, color.Coords[1], precision: 10);
        Assert.Equal(50.0,  color.Coords[2], precision: 10);
        Assert.Equal(1.0,   color.Alpha);
    }

    [Fact]
    public void Parse_Hsl_SpaceSeparated()
    {
        var color = new Color("hsl(180 100% 50%)");
        Assert.Equal(180.0, color.Coords[0], precision: 10);
        Assert.Equal(100.0, color.Coords[1], precision: 10);
        Assert.Equal(50.0,  color.Coords[2], precision: 10);
    }

    [Fact]
    public void Parse_Hsl_WithAlpha()
    {
        var color = new Color("hsl(180 100% 50% / 0.5)");
        Assert.Equal(0.5, color.Alpha);
    }

    [Fact]
    public void Parse_Hsl_AngleUnitDeg()
    {
        var color = new Color("hsl(180deg 100% 50%)");
        Assert.Equal(180.0, color.Coords[0], precision: 10);
    }

    [Fact]
    public void Parse_Hsl_AngleUnitRad()
    {
        var color = new Color("hsl(3.14159265rad 100% 50%)");
        Assert.Equal(180.0, color.Coords[0], precision: 3);
    }

    [Fact]
    public void Parse_Hsl_AngleUnitTurn()
    {
        var color = new Color("hsl(0.5turn 100% 50%)");
        Assert.Equal(180.0, color.Coords[0], precision: 10);
    }

    [Fact]
    public void Parse_Hsl_AngleUnitGrad()
    {
        var color = new Color("hsl(200grad 100% 50%)");
        Assert.Equal(180.0, color.Coords[0], precision: 10);
    }

    [Fact]
    public void Parse_Hsl_NoneHue()
    {
        var color = new Color("hsl(none 100% 50%)");
        Assert.True(double.IsNaN(color.Coords[0]));
    }

    [Fact]
    public void Parse_Hsl_RawNumbersForSaturationLightness()
    {
        // Without %, numbers are used directly in the [0, 100] range
        var color = new Color("hsl(180 100 50)");
        Assert.Equal(100.0, color.Coords[1], precision: 10);
        Assert.Equal(50.0,  color.Coords[2], precision: 10);
    }

    [Fact]
    public void Parse_Hsla_CommaSeparatedWithAlpha()
    {
        var color = new Color("hsla(180, 100%, 50%, 0.8)");
        Assert.Equal(ColorSpace.Hsl, color.Space);
        Assert.Equal(0.8, color.Alpha);
    }

    [Fact]
    public void Parse_Hsl_StoresFormatId()
    {
        Assert.Equal("hsl",  new Color("hsl(180 100% 50%)").ParseMeta?.FormatId);
        Assert.Equal("hsla", new Color("hsla(180, 100%, 50%, 1)").ParseMeta?.FormatId);
    }

    // ──────────────────────────────────────────────────────────────────────
    // hwb() parsing
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Hwb_Basic()
    {
        var color = new Color("hwb(180 0% 0%)");
        Assert.Equal(ColorSpace.Hwb, color.Space);
        Assert.Equal(180.0, color.Coords[0], precision: 10);
        Assert.Equal(0.0,   color.Coords[1], precision: 10);
        Assert.Equal(0.0,   color.Coords[2], precision: 10);
    }

    [Fact]
    public void Parse_Hwb_WithAlpha()
    {
        var color = new Color("hwb(180 20% 30% / 0.5)");
        Assert.Equal(180.0, color.Coords[0], precision: 10);
        Assert.Equal(20.0,  color.Coords[1], precision: 10);
        Assert.Equal(30.0,  color.Coords[2], precision: 10);
        Assert.Equal(0.5,   color.Alpha);
    }

    [Fact]
    public void Parse_Hwb_AngleUnits()
    {
        var color = new Color("hwb(0.5turn 0% 0%)");
        Assert.Equal(180.0, color.Coords[0], precision: 10);
    }

    [Fact]
    public void Parse_Hwb_StoresFormatId()
    {
        Assert.Equal("hwb", new Color("hwb(180 0% 0%)").ParseMeta?.FormatId);
    }

    // ──────────────────────────────────────────────────────────────────────
    // lab() parsing
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Lab_Basic()
    {
        var color = new Color("lab(50 25 -25)");
        Assert.Equal(ColorSpace.Lab, color.Space);
        Assert.Equal(50.0,  color.Coords[0], precision: 10);
        Assert.Equal(25.0,  color.Coords[1], precision: 10);
        Assert.Equal(-25.0, color.Coords[2], precision: 10);
    }

    [Fact]
    public void Parse_Lab_WithAlpha()
    {
        var color = new Color("lab(50 25 -25 / 0.6)");
        Assert.Equal(0.6, color.Alpha);
    }

    [Fact]
    public void Parse_Lab_PercentageLightness()
    {
        // 50% of [0, 100] = 50
        var color = new Color("lab(50% 25 -25)");
        Assert.Equal(50.0, color.Coords[0], precision: 10);
    }

    [Fact]
    public void Parse_Lab_PercentageAB()
    {
        // 100% of [-125, 125]: max abs = 125 → 100% = 125
        var color = new Color("lab(50 100% -100%)");
        Assert.Equal(125.0,  color.Coords[1], precision: 10);
        Assert.Equal(-125.0, color.Coords[2], precision: 10);
    }

    [Fact]
    public void Parse_Lab_NoneCoordinates()
    {
        var color = new Color("lab(none none none)");
        Assert.True(double.IsNaN(color.Coords[0]));
        Assert.True(double.IsNaN(color.Coords[1]));
        Assert.True(double.IsNaN(color.Coords[2]));
    }

    [Fact]
    public void Parse_Lab_StoresFormatId()
    {
        Assert.Equal("lab", new Color("lab(50 25 -25)").ParseMeta?.FormatId);
    }

    // ──────────────────────────────────────────────────────────────────────
    // lch() parsing
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Lch_Basic()
    {
        var color = new Color("lch(50 30 180)");
        Assert.Equal(ColorSpace.Lch, color.Space);
        Assert.Equal(50.0,  color.Coords[0], precision: 10);
        Assert.Equal(30.0,  color.Coords[1], precision: 10);
        Assert.Equal(180.0, color.Coords[2], precision: 10);
    }

    [Fact]
    public void Parse_Lch_WithAlpha()
    {
        var color = new Color("lch(50 30 180 / 0.7)");
        Assert.Equal(0.7, color.Alpha);
    }

    [Fact]
    public void Parse_Lch_AngleUnits()
    {
        var color = new Color("lch(50 30 0.5turn)");
        Assert.Equal(180.0, color.Coords[2], precision: 10);
    }

    [Fact]
    public void Parse_Lch_StoresFormatId()
    {
        Assert.Equal("lch", new Color("lch(50 30 180)").ParseMeta?.FormatId);
    }

    // ──────────────────────────────────────────────────────────────────────
    // oklab() parsing
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Oklab_Basic()
    {
        var color = new Color("oklab(0.5 0.1 -0.1)");
        Assert.Equal(ColorSpace.Oklab, color.Space);
        Assert.Equal(0.5,  color.Coords[0], precision: 10);
        Assert.Equal(0.1,  color.Coords[1], precision: 10);
        Assert.Equal(-0.1, color.Coords[2], precision: 10);
    }

    [Fact]
    public void Parse_Oklab_WithAlpha()
    {
        var color = new Color("oklab(0.5 0.1 -0.1 / 0.4)");
        Assert.Equal(0.4, color.Alpha);
    }

    [Fact]
    public void Parse_Oklab_PercentageLightness()
    {
        // 50% of [0, 1] range = 0.5
        var color = new Color("oklab(50% 0.1 -0.1)");
        Assert.Equal(0.5, color.Coords[0], precision: 10);
    }

    [Fact]
    public void Parse_Oklab_StoresFormatId()
    {
        Assert.Equal("oklab", new Color("oklab(0.5 0 0)").ParseMeta?.FormatId);
    }

    // ──────────────────────────────────────────────────────────────────────
    // oklch() parsing
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Oklch_Basic()
    {
        var color = new Color("oklch(0.5 0.1 180)");
        Assert.Equal(ColorSpace.Oklch, color.Space);
        Assert.Equal(0.5,  color.Coords[0], precision: 10);
        Assert.Equal(0.1,  color.Coords[1], precision: 10);
        Assert.Equal(180.0, color.Coords[2], precision: 10);
    }

    [Fact]
    public void Parse_Oklch_WithAlpha()
    {
        var color = new Color("oklch(0.5 0.1 180 / 0.3)");
        Assert.Equal(0.3, color.Alpha);
    }

    [Fact]
    public void Parse_Oklch_NoneHue()
    {
        var color = new Color("oklch(0.5 0.1 none)");
        Assert.True(double.IsNaN(color.Coords[2]));
    }

    [Fact]
    public void Parse_Oklch_AngleUnits()
    {
        var color = new Color("oklch(0.5 0.1 0.5turn)");
        Assert.Equal(180.0, color.Coords[2], precision: 10);
    }

    [Fact]
    public void Parse_Oklch_StoresFormatId()
    {
        Assert.Equal("oklch", new Color("oklch(0.5 0 0)").ParseMeta?.FormatId);
    }

    // ──────────────────────────────────────────────────────────────────────
    // ParseResult discriminated union
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ParseResult_Success_HasCorrectSpace()
    {
        var result = Color.TryParseCss("color(srgb 1 0 0)");
        var success = Assert.IsType<ParseResult.Success>(result);
        Assert.Equal(ColorSpace.Srgb, success.Space);
    }

    [Fact]
    public void ParseResult_Failure_HasErrorMessage()
    {
        var result = Color.TryParseCss("not-a-color");
        var failure = Assert.IsType<ParseResult.Failure>(result);
        Assert.False(string.IsNullOrEmpty(failure.Error));
    }

    [Fact]
    public void ParseResult_Success_HasParseMeta()
    {
        var result = Color.TryParseCss("rgb(255 0 0)");
        var success = Assert.IsType<ParseResult.Success>(result);
        Assert.Equal("rgb", success.Meta?.FormatId);
    }

    // ──────────────────────────────────────────────────────────────────────
    // color() with new registered spaces
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_ColorFunction_HslSpace()
    {
        var color = new Color("color(hsl 180 100 50)");
        Assert.Equal(ColorSpace.Hsl, color.Space);
        Assert.Equal(180.0, color.Coords[0], precision: 10);
        Assert.Equal(100.0, color.Coords[1], precision: 10);
        Assert.Equal(50.0,  color.Coords[2], precision: 10);
    }

    [Fact]
    public void Parse_ColorFunction_OklabSpace()
    {
        var color = new Color("color(oklab 0.5 0.1 -0.1)");
        Assert.Equal(ColorSpace.Oklab, color.Space);
        Assert.Equal(0.5,  color.Coords[0], precision: 10);
        Assert.Equal(0.1,  color.Coords[1], precision: 10);
        Assert.Equal(-0.1, color.Coords[2], precision: 10);
    }

    // ──────────────────────────────────────────────────────────────────────
    // ColorSpace static properties for new spaces
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ColorSpace_Hsl_IsRegistered()
    {
        Assert.NotNull(ColorSpace.FromId("hsl"));
        Assert.Equal("hsl", ColorSpace.Hsl.Id);
    }

    [Fact]
    public void ColorSpace_Hwb_IsRegistered()
    {
        Assert.NotNull(ColorSpace.FromId("hwb"));
    }

    [Fact]
    public void ColorSpace_Lab_IsRegistered()
    {
        Assert.NotNull(ColorSpace.FromId("lab"));
    }

    [Fact]
    public void ColorSpace_Lch_IsRegistered()
    {
        Assert.NotNull(ColorSpace.FromId("lch"));
    }

    [Fact]
    public void ColorSpace_Oklab_IsRegistered()
    {
        Assert.NotNull(ColorSpace.FromId("oklab"));
    }

    [Fact]
    public void ColorSpace_Oklch_IsRegistered()
    {
        Assert.NotNull(ColorSpace.FromId("oklch"));
    }

    [Fact]
    public void ColorSpace_FindFormat_FindsRgbFormatOnSrgb()
    {
        var result = ColorSpace.FindFormat("rgb");
        Assert.NotNull(result);
        Assert.Equal(ColorSpace.Srgb, result!.Value.Space);
        Assert.Equal("rgb", result.Value.Format.Id);
    }

    [Fact]
    public void ColorSpace_FindFormat_FindsHslFormat()
    {
        var result = ColorSpace.FindFormat("hsl");
        Assert.NotNull(result);
        Assert.Equal(ColorSpace.Hsl, result!.Value.Space);
    }

    [Fact]
    public void ColorSpace_FindFormat_FindsHexFormatOnSrgb()
    {
        var result = ColorSpace.FindFormat("hex");
        Assert.NotNull(result);
        Assert.Equal(ColorSpace.Srgb, result!.Value.Space);
    }

    // ──────────────────────────────────────────────────────────────────────
    // ParseMeta round-trip metadata
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ParseMeta_NotSetForNonCssConstructors()
    {
        var color = new Color(ColorSpace.Srgb, [1.0, 0.0, 0.0]);
        Assert.Null(color.ParseMeta);
    }

    [Fact]
    public void ParseMeta_SetForCssStringConstructor()
    {
        var color = new Color("color(srgb 1 0 0)");
        Assert.NotNull(color.ParseMeta);
        Assert.Equal("srgb", color.ParseMeta!.FormatId);
    }

    [Fact]
    public void ParseMeta_HslHasCorrectFormatId()
    {
        Assert.Equal("hsl",  new Color("hsl(180 100% 50%)").ParseMeta?.FormatId);
        Assert.Equal("hsla", new Color("hsla(180, 100%, 50%, 1)").ParseMeta?.FormatId);
    }

    [Fact]
    public void ParseMeta_LabHasCorrectFormatId()
    {
        Assert.Equal("lab", new Color("lab(50 0 0)").ParseMeta?.FormatId);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Tests ported directly from color.js/test/parse.js
    // https://github.com/color-js/color.js/blob/main/test/parse.js
    // ══════════════════════════════════════════════════════════════════════

    // ── none values ───────────────────────────────────────────────────────

    [Fact]
    public void Ported_ParseJs_NoneHue_Lch()
    {
        var color = new Color("lch(90 0 none)");
        Assert.Equal("lch",  color.Space.Id);
        Assert.Equal(90.0,   color.Coords[0], precision: 10);
        Assert.Equal(0.0,    color.Coords[1], precision: 10);
        Assert.True(double.IsNaN(color.Coords[2]));
        Assert.Equal(1.0, color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_NoneHue_Oklch()
    {
        var color = new Color("oklch(1 0 none)");
        Assert.Equal("oklch", color.Space.Id);
        Assert.Equal(1.0,     color.Coords[0], precision: 10);
        Assert.Equal(0.0,     color.Coords[1], precision: 10);
        Assert.True(double.IsNaN(color.Coords[2]));
        Assert.Equal(1.0, color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_NoneHue_HslCommasSyntax()
    {
        // none hue with legacy comma syntax
        var color = new Color("hsl(none, 50%, 50%)");
        Assert.Equal("hsl", color.Space.Id);
        Assert.True(double.IsNaN(color.Coords[0]));
        Assert.Equal(50.0, color.Coords[1], precision: 10);
        Assert.Equal(50.0, color.Coords[2], precision: 10);
        Assert.Equal(1.0,  color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_NoneHue_Hwb()
    {
        var color = new Color("hwb(none 20% 30%)");
        Assert.Equal("hwb", color.Space.Id);
        Assert.True(double.IsNaN(color.Coords[0]));
        Assert.Equal(20.0, color.Coords[1], precision: 10);
        Assert.Equal(30.0, color.Coords[2], precision: 10);
        Assert.Equal(1.0,  color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_NoneAlpha_Oklch()
    {
        var color = new Color("oklch(1 0 120 / none)");
        Assert.Equal("oklch", color.Space.Id);
        Assert.Equal(1.0,   color.Coords[0], precision: 10);
        Assert.Equal(0.0,   color.Coords[1], precision: 10);
        Assert.Equal(120.0, color.Coords[2], precision: 10);
        Assert.True(double.IsNaN(color.Alpha));
    }

    // ── sRGB colors — specific hex values ─────────────────────────────────

    [Fact]
    public void Ported_ParseJs_Hex6_Ff0066()
    {
        // #ff0066 → red=1, green=0, blue=0x66/255=0.4
        var color = new Color("#ff0066");
        Assert.Equal("srgb", color.Space.Id);
        Assert.Equal(1.0, color.Coords[0], precision: 10);
        Assert.Equal(0.0, color.Coords[1], precision: 10);
        Assert.Equal(0.4, color.Coords[2], precision: 10);
        Assert.Equal(1.0, color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Hex3_F06()
    {
        // #f06 expands to #ff0066
        var color = new Color("#f06");
        Assert.Equal("srgb", color.Space.Id);
        Assert.Equal(1.0, color.Coords[0], precision: 10);
        Assert.Equal(0.0, color.Coords[1], precision: 10);
        Assert.Equal(0.4, color.Coords[2], precision: 10);
        Assert.Equal(1.0, color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Hex8_Ff006688()
    {
        // #ff006688 → 0x88=136 → alpha=136/255≈0.5333...
        var color = new Color("#ff006688");
        Assert.Equal("srgb", color.Space.Id);
        Assert.Equal(1.0,               color.Coords[0], precision: 10);
        Assert.Equal(0.0,               color.Coords[1], precision: 10);
        Assert.Equal(0.4,               color.Coords[2], precision: 10);
        Assert.Equal(136.0 / 255.0,     color.Alpha,     precision: 10);
    }

    [Fact]
    public void Ported_ParseJs_Hex4_F068()
    {
        // #f068 expands to #ff006688
        var color = new Color("#f068");
        Assert.Equal(1.0,           color.Coords[0], precision: 10);
        Assert.Equal(0.0,           color.Coords[1], precision: 10);
        Assert.Equal(0.4,           color.Coords[2], precision: 10);
        Assert.Equal(136.0 / 255.0, color.Alpha,     precision: 10);
    }

    [Theory]
    [InlineData("#12")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("#123456789")]
    public void Ported_ParseJs_Hex_WrongCharCount_Throws(string input) =>
        Assert.Throws<FormatException>(() => new Color(input));

    // ── sRGB colors — rgb() / rgba() ──────────────────────────────────────

    [Fact]
    public void Ported_ParseJs_Rgba_PercentagesWithAlpha()
    {
        // rgba(0% 50% 200% / 0.5) — percentages map to [0,1], OOG allowed
        var color = new Color("rgba(0% 50% 200% / 0.5)");
        Assert.Equal("srgb", color.Space.Id);
        Assert.Equal(0.0,  color.Coords[0], precision: 10);
        Assert.Equal(0.5,  color.Coords[1], precision: 10);
        Assert.Equal(2.0,  color.Coords[2], precision: 10);
        Assert.Equal(0.5,  color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Rgb_NumbersWithAlpha()
    {
        // rgb(0 127.5 300 / .5) — numbers /255, OOG allowed
        var color = new Color("rgb(0 127.5 300 / .5)");
        Assert.Equal("srgb", color.Space.Id);
        Assert.Equal(0.0,    color.Coords[0], precision: 10);
        Assert.Equal(0.5,    color.Coords[1], precision: 10);
        Assert.Equal(300.0 / 255.0, color.Coords[2], precision: 10);
        Assert.Equal(0.5,   color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Rgba_CommaNumbers()
    {
        // rgba(0, 127.5, 300, 0.5) — legacy comma syntax
        var color = new Color("rgba(0, 127.5, 300, 0.5)");
        Assert.Equal("srgb", color.Space.Id);
        Assert.Equal(0.0,    color.Coords[0], precision: 10);
        Assert.Equal(0.5,    color.Coords[1], precision: 10);
        Assert.Equal(300.0 / 255.0, color.Coords[2], precision: 10);
        Assert.Equal(0.5,   color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Rgb_AngleNotAllowed_Throws()
    {
        // angles not allowed in rgb()
        Assert.Throws<FormatException>(() => new Color("rgb(10deg 10 10)"));
    }

    // ── Lab and LCH colors ────────────────────────────────────────────────

    [Fact]
    public void Ported_ParseJs_Lab_100PercentLightness()
    {
        // lab(100% 0 0) → L=100
        var color = new Color("lab(100% 0 0)");
        Assert.Equal("lab", color.Space.Id);
        Assert.Equal(100.0, color.Coords[0], precision: 10);
        Assert.Equal(0.0,   color.Coords[1], precision: 10);
        Assert.Equal(0.0,   color.Coords[2], precision: 10);
        Assert.Equal(1.0,   color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Lab_CaseInsensitive()
    {
        // Lab(100% 0 0) — function name case insensitive
        var color = new Color("Lab(100% 0 0)");
        Assert.Equal("lab", color.Space.Id);
        Assert.Equal(100.0, color.Coords[0], precision: 10);
    }

    [Fact]
    public void Ported_ParseJs_Lab_NoPercent()
    {
        // lab(80 0 0)
        var color = new Color("lab(80 0 0)");
        Assert.Equal(80.0, color.Coords[0], precision: 10);
        Assert.Equal(0.0,  color.Coords[1], precision: 10);
        Assert.Equal(0.0,  color.Coords[2], precision: 10);
    }

    [Fact]
    public void Ported_ParseJs_Lab_NegativeAB()
    {
        // lab(100 -50 50)
        var color = new Color("lab(100 -50 50)");
        Assert.Equal(100.0, color.Coords[0], precision: 10);
        Assert.Equal(-50.0, color.Coords[1], precision: 10);
        Assert.Equal(50.0,  color.Coords[2], precision: 10);
    }

    [Fact]
    public void Ported_ParseJs_Lab_AllPercentagesWithAlpha()
    {
        // lab(50% 25% -25% / 50%): L=50, a=31.25, b=-31.25, alpha=0.5
        var color = new Color("lab(50% 25% -25% / 50%)");
        Assert.Equal(50.0,   color.Coords[0], precision: 10);
        Assert.Equal(31.25,  color.Coords[1], precision: 10);
        Assert.Equal(-31.25, color.Coords[2], precision: 10);
        Assert.Equal(0.5,    color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Lab_TransparencyDecimalAlpha()
    {
        // lab(100 -50 5 / .5)
        var color = new Color("lab(100 -50 5 / .5)");
        Assert.Equal(100.0, color.Coords[0], precision: 10);
        Assert.Equal(-50.0, color.Coords[1], precision: 10);
        Assert.Equal(5.0,   color.Coords[2], precision: 10);
        Assert.Equal(0.5,   color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Lch_100PercentLightness()
    {
        // lch(100% 0 0) → L=100
        var color = new Color("lch(100% 0 0)");
        Assert.Equal("lch", color.Space.Id);
        Assert.Equal(100.0, color.Coords[0], precision: 10);
        Assert.Equal(0.0,   color.Coords[1], precision: 10);
        Assert.Equal(0.0,   color.Coords[2], precision: 10);
    }

    [Fact]
    public void Ported_ParseJs_Lch_NoPercentage()
    {
        // lch(100 50 50)
        var color = new Color("lch(100 50 50)");
        Assert.Equal(100.0, color.Coords[0], precision: 10);
        Assert.Equal(50.0,  color.Coords[1], precision: 10);
        Assert.Equal(50.0,  color.Coords[2], precision: 10);
    }

    [Fact]
    public void Ported_ParseJs_Lch_PercentagesWithAlpha()
    {
        // lch(50% 50% 50 / 50%): L=50, C=75, H=50, alpha=0.5
        var color = new Color("lch(50% 50% 50 / 50%)");
        Assert.Equal(50.0, color.Coords[0], precision: 10);
        Assert.Equal(75.0, color.Coords[1], precision: 10);
        Assert.Equal(50.0, color.Coords[2], precision: 10);
        Assert.Equal(0.5,  color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Lch_HueOver360()
    {
        // lch(100 50 450) — hue > 360, not clamped
        var color = new Color("lch(100 50 450)");
        Assert.Equal(100.0, color.Coords[0], precision: 10);
        Assert.Equal(50.0,  color.Coords[1], precision: 10);
        Assert.Equal(450.0, color.Coords[2], precision: 10);
    }

    // ── OKLab colors ──────────────────────────────────────────────────────

    [Fact]
    public void Ported_ParseJs_Oklab_100PercentLightness()
    {
        // oklab(100% 0 0) → L=1.0
        var color = new Color("oklab(100% 0 0)");
        Assert.Equal("oklab", color.Space.Id);
        Assert.Equal(1.0, color.Coords[0], precision: 10);
        Assert.Equal(0.0, color.Coords[1], precision: 10);
        Assert.Equal(0.0, color.Coords[2], precision: 10);
        Assert.Equal(1.0, color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Oklab_WithAlpha()
    {
        // oklab(100% 0 0 / 0.5)
        var color = new Color("oklab(100% 0 0 / 0.5)");
        Assert.Equal(1.0, color.Coords[0], precision: 10);
        Assert.Equal(0.5, color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Oklab_CaseInsensitive()
    {
        // OKLab(100% 0 0) — function name case insensitive
        var color = new Color("OKLab(100% 0 0)");
        Assert.Equal("oklab", color.Space.Id);
        Assert.Equal(1.0, color.Coords[0], precision: 10);
    }

    [Fact]
    public void Ported_ParseJs_Oklab_AllPercentages()
    {
        // oklab(42% 100% -50%): L=0.42, a=0.4, b=-0.2
        // (CSS spec: a/b reference range is [-0.4, 0.4], so 100%=0.4)
        var color = new Color("oklab(42% 100% -50%)");
        Assert.Equal(0.42, color.Coords[0], precision: 10);
        Assert.Equal(0.4,  color.Coords[1], precision: 10);
        Assert.Equal(-0.2, color.Coords[2], precision: 10);
        Assert.Equal(1.0,  color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Oklab_AllNumbers()
    {
        // oklab(1 -0.20 0.20) — raw numbers
        var color = new Color("oklab(1 -0.20 0.20)");
        Assert.Equal(1.0,  color.Coords[0], precision: 10);
        Assert.Equal(-0.2, color.Coords[1], precision: 10);
        Assert.Equal(0.2,  color.Coords[2], precision: 10);
    }

    [Fact]
    public void Ported_ParseJs_Oklab_OutOfRangeNumbers()
    {
        // oklab(10 -0.80 0.80) — OOG, accepted as-is
        var color = new Color("oklab(10 -0.80 0.80)");
        Assert.Equal(10.0, color.Coords[0], precision: 10);
        Assert.Equal(-0.8, color.Coords[1], precision: 10);
        Assert.Equal(0.8,  color.Coords[2], precision: 10);
    }

    // ── OKLch colors ──────────────────────────────────────────────────────

    [Fact]
    public void Ported_ParseJs_Oklch_100PercentLightness()
    {
        // oklch(100% 0 0) → L=1.0
        var color = new Color("oklch(100% 0 0)");
        Assert.Equal("oklch", color.Space.Id);
        Assert.Equal(1.0, color.Coords[0], precision: 10);
        Assert.Equal(0.0, color.Coords[1], precision: 10);
        Assert.Equal(0.0, color.Coords[2], precision: 10);
        Assert.Equal(1.0, color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Oklch_AlphaAsPercentage()
    {
        // oklch(100% 0 0 / 50%) — alpha as percentage
        var color = new Color("oklch(100% 0 0 / 50%)");
        Assert.Equal(1.0, color.Coords[0], precision: 10);
        Assert.Equal(0.5, color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Oklch_CaseInsensitive()
    {
        // OKLch(100% 0 0) — function name case insensitive
        var color = new Color("OKLch(100% 0 0)");
        Assert.Equal("oklch", color.Space.Id);
        Assert.Equal(1.0, color.Coords[0], precision: 10);
    }

    [Fact]
    public void Ported_ParseJs_Oklch_AllNumbers()
    {
        // oklch(1 0.2 50)
        var color = new Color("oklch(1 0.2 50)");
        Assert.Equal(1.0,  color.Coords[0], precision: 10);
        Assert.Equal(0.2,  color.Coords[1], precision: 10);
        Assert.Equal(50.0, color.Coords[2], precision: 10);
    }

    [Fact]
    public void Ported_ParseJs_Oklch_OutOfRangeClampedAlpha()
    {
        // oklch(10 2 500 / 10) — alpha 10 clamped to 1
        var color = new Color("oklch(10 2 500 / 10)");
        Assert.Equal(10.0,  color.Coords[0], precision: 10);
        Assert.Equal(2.0,   color.Coords[1], precision: 10);
        Assert.Equal(500.0, color.Coords[2], precision: 10);
        Assert.Equal(1.0,   color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Oklch_ChromaAsPercentage()
    {
        // oklch(100% 50% 50) — C as percentage: 50% × 0.4 = 0.2
        var color = new Color("oklch(100% 50% 50)");
        Assert.Equal(1.0,  color.Coords[0], precision: 10);
        Assert.Equal(0.2,  color.Coords[1], precision: 10);
        Assert.Equal(50.0, color.Coords[2], precision: 10);
    }

    [Fact]
    public void Ported_ParseJs_Oklch_ChromaOver100Percent()
    {
        // oklch(100% 150% 50) — C: 150% × 0.4 = 0.6
        var color = new Color("oklch(100% 150% 50)");
        Assert.Equal(1.0,  color.Coords[0], precision: 10);
        Assert.Equal(0.6,  color.Coords[1], precision: 5);
        Assert.Equal(50.0, color.Coords[2], precision: 10);
    }

    [Fact]
    public void Ported_ParseJs_Oklch_HueInDegrees()
    {
        // oklch(100% 0 30deg)
        var color = new Color("oklch(100% 0 30deg)");
        Assert.Equal(1.0,  color.Coords[0], precision: 10);
        Assert.Equal(0.0,  color.Coords[1], precision: 10);
        Assert.Equal(30.0, color.Coords[2], precision: 10);
    }

    // ── hsl() colors ──────────────────────────────────────────────────────

    [Fact]
    public void Ported_ParseJs_Hsl_CommasSyntax()
    {
        // hsl(180, 50%, 50%)
        var color = new Color("hsl(180, 50%, 50%)");
        Assert.Equal("hsl", color.Space.Id);
        Assert.Equal(180.0, color.Coords[0], precision: 10);
        Assert.Equal(50.0,  color.Coords[1], precision: 10);
        Assert.Equal(50.0,  color.Coords[2], precision: 10);
        Assert.Equal(1.0,   color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Hsl_NegativeHue()
    {
        // hsl(-180, 50%, 50%) — negative hue, stored as-is
        var color = new Color("hsl(-180, 50%, 50%)");
        Assert.Equal(-180.0, color.Coords[0], precision: 10);
        Assert.Equal(50.0,   color.Coords[1], precision: 10);
        Assert.Equal(50.0,   color.Coords[2], precision: 10);
    }

    [Fact]
    public void Ported_ParseJs_Hsl_HueOver360()
    {
        // hsl(900, 50%, 50%) — hue > 360, not clamped
        var color = new Color("hsl(900, 50%, 50%)");
        Assert.Equal(900.0, color.Coords[0], precision: 10);
    }

    [Fact]
    public void Ported_ParseJs_Hsl_DegHueSpaceSlashAlpha()
    {
        // hsl(90deg 0% 0% / .5)
        var color = new Color("hsl(90deg 0% 0% / .5)");
        Assert.Equal(90.0, color.Coords[0], precision: 10);
        Assert.Equal(0.0,  color.Coords[1], precision: 10);
        Assert.Equal(0.0,  color.Coords[2], precision: 10);
        Assert.Equal(0.5,  color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Hsl_RadHueSpaceSlashAlpha()
    {
        // hsl(1.5707963267948966rad 0% 0% / .5) ≈ 90°
        var color = new Color("hsl(1.5707963267948966rad 0% 0% / .5)");
        Assert.Equal(90.0, color.Coords[0], precision: 5);
        Assert.Equal(0.5,  color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Hsl_GradHueSpaceSlashAlpha()
    {
        // hsl(100grad 0% 0% / .5) — 100 grad = 90°
        var color = new Color("hsl(100grad 0% 0% / .5)");
        Assert.Equal(90.0, color.Coords[0], precision: 10);
        Assert.Equal(0.5,  color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Hsl_TurnHueSpaceSlashAlpha()
    {
        // hsl(0.25turn 0% 0% / .5) — 0.25 turn = 90°
        var color = new Color("hsl(0.25turn 0% 0% / .5)");
        Assert.Equal(90.0, color.Coords[0], precision: 10);
        Assert.Equal(0.5,  color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Hsl_OogSaturation()
    {
        // hsl(230.6 179.7% 37.56% / 1) — saturation > 100%, accepted as-is
        var color = new Color("hsl(230.6 179.7% 37.56% / 1)");
        Assert.Equal(230.6,  color.Coords[0], precision: 5);
        Assert.Equal(179.7,  color.Coords[1], precision: 5);
        Assert.Equal(37.56,  color.Coords[2], precision: 5);
        Assert.Equal(1.0,    color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Hsl_LegacyCommaNoAlpha()
    {
        // hsl(0, 0%, 0%)
        var color = new Color("hsl(0, 0%, 0%)");
        Assert.Equal(0.0, color.Coords[0], precision: 10);
        Assert.Equal(0.0, color.Coords[1], precision: 10);
        Assert.Equal(0.0, color.Coords[2], precision: 10);
        Assert.Equal(1.0, color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Hsl_LegacyCommaWithAlpha()
    {
        // hsl(0, 0%, 0%, 0.5)
        var color = new Color("hsl(0, 0%, 0%, 0.5)");
        Assert.Equal(0.0, color.Coords[0], precision: 10);
        Assert.Equal(0.5, color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Hsl_ModernPercentageNoAlpha()
    {
        // hsl(0 50% 25%)
        var color = new Color("hsl(0 50% 25%)");
        Assert.Equal(0.0,  color.Coords[0], precision: 10);
        Assert.Equal(50.0, color.Coords[1], precision: 10);
        Assert.Equal(25.0, color.Coords[2], precision: 10);
        Assert.Equal(1.0,  color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Hsl_ModernPercentageWithAlpha()
    {
        // hsl(0 50% 25% / 50%) — alpha as percentage
        var color = new Color("hsl(0 50% 25% / 50%)");
        Assert.Equal(0.0,  color.Coords[0], precision: 10);
        Assert.Equal(50.0, color.Coords[1], precision: 10);
        Assert.Equal(25.0, color.Coords[2], precision: 10);
        Assert.Equal(0.5,  color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Hsl_ModernNumberNoAlpha()
    {
        // hsl(0 50 25) — raw numbers for S and L
        var color = new Color("hsl(0 50 25)");
        Assert.Equal(0.0,  color.Coords[0], precision: 10);
        Assert.Equal(50.0, color.Coords[1], precision: 10);
        Assert.Equal(25.0, color.Coords[2], precision: 10);
    }

    [Fact]
    public void Ported_ParseJs_Hsl_ModernNumberWithAlpha()
    {
        // hsl(0 50 25 / 50%)
        var color = new Color("hsl(0 50 25 / 50%)");
        Assert.Equal(50.0, color.Coords[1], precision: 10);
        Assert.Equal(0.5,  color.Alpha);
    }

    [Fact]
    public void Ported_ParseJs_Hsla_DegHueNoAlpha()
    {
        // hsla(240deg 100% 50%)
        var color = new Color("hsla(240deg 100% 50%)");
        Assert.Equal("hsl",  color.Space.Id);
        Assert.Equal(240.0,  color.Coords[0], precision: 10);
        Assert.Equal(100.0,  color.Coords[1], precision: 10);
        Assert.Equal(50.0,   color.Coords[2], precision: 10);
    }

    [Fact]
    public void Ported_ParseJs_Hsla_DegHueWithAlpha()
    {
        // hsla(240deg 100% 50% / 0.5)
        var color = new Color("hsla(240deg 100% 50% / 0.5)");
        Assert.Equal(240.0, color.Coords[0], precision: 10);
        Assert.Equal(0.5,   color.Alpha);
    }

    // ── hwb() colors ──────────────────────────────────────────────────────

    [Fact]
    public void Ported_ParseJs_Hwb_WithPercentages()
    {
        // hwb(180 20% 30%)
        var color = new Color("hwb(180 20% 30%)");
        Assert.Equal("hwb", color.Space.Id);
        Assert.Equal(180.0, color.Coords[0], precision: 10);
        Assert.Equal(20.0,  color.Coords[1], precision: 10);
        Assert.Equal(30.0,  color.Coords[2], precision: 10);
    }

    [Fact]
    public void Ported_ParseJs_Hwb_WithRawNumbers()
    {
        // hwb(180 20 30) — raw numbers (no % sign)
        var color = new Color("hwb(180 20 30)");
        Assert.Equal("hwb", color.Space.Id);
        Assert.Equal(180.0, color.Coords[0], precision: 10);
        Assert.Equal(20.0,  color.Coords[1], precision: 10);
        Assert.Equal(30.0,  color.Coords[2], precision: 10);
    }

    // ── parsing metadata ──────────────────────────────────────────────────

    [Fact]
    public void Ported_ParseJs_Metadata_HexFormatId()
    {
        // Parse metadata should capture "hex" for a hex string
        Assert.Equal("hex", new Color("#ff8000").ParseMeta?.FormatId);
    }
}
