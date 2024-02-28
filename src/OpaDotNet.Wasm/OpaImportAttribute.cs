namespace OpaDotNet.Wasm;

/// <summary>
///
/// </summary>
/// <param name="name"></param>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal class OpaImportAttribute(string name) : Attribute
{
    /// <summary>
    ///
    /// </summary>
    public string Name { get; } = name;

    internal string? Description { get; set; }

    internal string[]? Categories { get; set; }

    /// <summary>
    ///
    /// </summary>
    internal OpaImportType Type { get; set; }
}