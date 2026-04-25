namespace Color.CS;

/// <summary>Describes the mathematical nature of a color coordinate.</summary>
public enum CoordType
{
    /// <summary>A linear numeric coordinate (default).</summary>
    Linear,

    /// <summary>An angular coordinate (e.g. hue). Makes the owning space polar.</summary>
    Angle,
}
