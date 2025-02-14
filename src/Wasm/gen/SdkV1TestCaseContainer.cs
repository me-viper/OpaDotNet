using System.Text.Json.Serialization;

using Microsoft.CodeAnalysis;

using OpaDotNet.Wasm.Tests;

namespace OpaDotNet.Wasm.Generators;

[UsedImplicitly]
internal class SdkV1TestCaseContainer
{
    private string? _fileName;

    public string FileName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_fileName))
                _fileName = Path.GetRandomFileName();

            return _fileName!;
        }
        set { _fileName = value; }
    }

    public HashSet<SdkV1TestCase> Cases { get; set; } = new();

    public string Hash { get; set; } = string.Empty;

    [JsonIgnore]
    public List<Diagnostic> Diagnostics { get; } = new();
}