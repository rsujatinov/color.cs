# color.cs

A C# port of [color.js](https://github.com/color-js/color.js) — a modern, extensible color manipulation library targeting **.NET 10** and **C# 14**.

[![CI](https://github.com/rsujatinov/color.cs/actions/workflows/ci.yml/badge.svg)](https://github.com/rsujatinov/color.cs/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Color.CS.svg)](https://www.nuget.org/packages/Color.CS)

---

## Goals

- Full-fidelity port of the color.js API to idiomatic C#
- Support for multiple color spaces (sRGB, Display-P3, OKLab, OKLCh, …)
- Zero third-party runtime dependencies
- Immutable, functional-style API using C# `record` types
- High performance via `Span<T>` and zero-allocation patterns

---

## Installation

```bash
dotnet add package Color.CS
```

Or add directly to your `.csproj`:

```xml
<PackageReference Include="Color.CS" Version="0.1.0" />
```

---

## Usage

```csharp
using Color.CS;

// Create an opaque red in sRGB
var red = new Color(ColorSpace.Srgb, [1.0, 0.0, 0.0]);

// Create a semi-transparent version
var semiTransparentRed = red.With(alpha: 0.5);

Console.WriteLine(semiTransparentRed.Alpha); // 0.5
Console.WriteLine(semiTransparentRed.Space.Name); // sRGB
```

---

## Building

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet build Color.CS.slnx
dotnet test Color.CS.slnx
```

---

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.

---

## License

[MIT](LICENSE) © Roman Sujatinov
