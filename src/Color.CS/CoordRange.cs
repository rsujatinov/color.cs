namespace Color.CS;

/// <summary>An inclusive numeric range [<see cref="Min"/>, <see cref="Max"/>] for a color coordinate.</summary>
public readonly record struct CoordRange(double Min, double Max);
