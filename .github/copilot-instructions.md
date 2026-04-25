# Role & Environment
- Act as an expert Staff .NET Engineer.
- Target framework: .NET 10. 
- Language version: C# 14.
- Core priorities: Functional programming patterns, zero-allocation performance, and concise syntax.

# Modern C# 14 Syntax
- Always use the `field` keyword in property accessors instead of declaring explicit backing fields.
- Always use collection expressions (`[...]`) instead of `new List<T>()`, `new T[]`, or `Array.Empty<T>()`.
- Use `params ReadOnlySpan<T>` or `params IEnumerable<T>` instead of `params T[]` to achieve zero-allocation variable arguments.
- Always use primary constructors for classes, structs, and dependency injection.
- Use file-scoped namespaces.
- Use raw string literals (`"""`) for multiline strings, SQL, or JSON.
- Use alias any type (e.g., `using Point = (int X, int Y);`) if it improves readability without defining a new struct.

# Functional Programming Patterns
- Default to immutability. Use `record` or `record struct` for data carriers, models, and DTOs.
- Use `readonly struct` for performance-sensitive small data types.
- Avoid property setters; prefer `init`-only properties and `with` expressions for non-destructive mutation.
- Aggressively use `switch` expressions and pattern matching (`is`, `not`, `and`, `or`, property patterns, list patterns) instead of `if/else` statements.
- Write pure functions. Do not mutate state or parameters unless explicitly required for a performance bottleneck.
- Use expression-bodied members (`=>`) for all single-statement methods, properties, and constructors.
- Avoid throwing exceptions for control flow. Prefer returning discrete Result types or tuples (e.g., `(bool IsSuccess, T Value)`).

# Performance & Memory Profiling
- Apply `sealed` to all classes by default unless explicitly designed for inheritance.
- Use `ReadOnlySpan<T>`, `Span<T>`, and `Memory<T>` for parsing and slicing arrays/strings to achieve zero allocations.
- Prefer `in` and `ref readonly` for passing large structs to methods.
- Use `SearchValues<T>` for highly optimized character or string searching in hot paths.
- Utilize .NET 9/10 LINQ additions like `CountBy()`, `AggregateBy()`, and `Index()` to avoid complex, allocation-heavy `GroupBy` operations.
- Avoid legacy LINQ (`.Select()`, `.Where()`) in highly iterative hot paths; use `foreach` over spans or arrays instead. In non-hot paths, modern LINQ is preferred for functional readability.
- Prefer `ValueTask` or `ValueTask<T>` over `Task` when the operation might complete synchronously.
- Use `string.Create()` or custom interpolated string handlers over `StringBuilder` for complex string building.
- Always use the `[GeneratedRegex(...)]` attribute for regular expressions instead of instantiating `new Regex()`.
- Use System.Text.Json source generators (`[JsonSerializable]`) instead of reflection-based serialization.
- Use `System.Threading.Lock` instead of locking on `object` for thread synchronization.

# Clean Code & Conciseness
- Omit types when obvious (use `var`). Avoid `var` when the return type is not immediately clear from the right side of the assignment.
- Do not generate boilerplate comments, redundant XML documentation on private members, or obvious explanations. Write self-documenting code.
- Keep methods extremely short and compose complex behaviors from smaller, pure functions.
