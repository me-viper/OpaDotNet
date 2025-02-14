namespace OpaDotNet.Wasm.Generators;

[UsedImplicitly]
internal class TestCaseFilterDefinition
{
    public string Reason { get; set; } = null!;

    public HashSet<string> Regex { get; set; } = new();
}