namespace OpaDotNet.Wasm;

/// <summary>
/// Marks custom built-in implementation.
/// </summary>
/// <param name="name"></param>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal class OpaImportAttribute(string name) : Attribute
{
    /// <summary>
    /// Built-in name.
    /// </summary>
    public string Name { get; } = name;

    internal string? Description { get; set; }

    internal string[]? Categories { get; set; }

    internal OpaImportType Type { get; set; }
}