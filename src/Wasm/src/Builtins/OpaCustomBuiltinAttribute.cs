namespace OpaDotNet.Wasm.Builtins;

/// <summary>
/// Marks custom built-in implementation.
/// </summary>
/// <param name="name">Built-in name.</param>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class OpaCustomBuiltinAttribute(string name) : Attribute
{
    /// <summary>
    /// Built-in name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// If <c>true</c> enables value memoization across multiple calls in the same query.
    /// </summary>
    public bool Memorize { get; init; }

    internal string? Description { get; set; }

    internal string[]? Categories { get; set; }

    internal OpaImportType Type { get; set; }
}