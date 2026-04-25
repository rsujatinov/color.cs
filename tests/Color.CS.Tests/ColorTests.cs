using System.Collections.Immutable;

namespace Color.CS.Tests;

public sealed class ColorTests
{
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
}
