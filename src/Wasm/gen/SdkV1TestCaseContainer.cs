using System.Text.Json.Serialization;

using Microsoft.CodeAnalysis;

using OpaDotNet.Wasm.Tests;

namespace OpaDotNet.Wasm.Generators;

[UsedImplicitly]
internal class SdkV1TestCaseContainer
{
    private string? _name;

    public string Name
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_name))
                _name = Path.GetRandomFileName();

            return _name!;
        }
        set { _name = value; }
    }

    public string FileName { get; set; } = null!;

    public HashSet<SdkV1TestCase> Cases { get; set; } = new();

    public string Hash { get; set; } = string.Empty;

    [JsonIgnore]
    public List<Diagnostic> Diagnostics { get; } = new();
}