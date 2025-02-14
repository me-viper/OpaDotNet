namespace OpaDotNet.Wasm.Generators;

internal class SdkV1TestCaseContainerEqualityComparer : IEqualityComparer<SdkV1TestCaseContainer?>
{
    public static SdkV1TestCaseContainerEqualityComparer Instance { get; } = new();

    public bool Equals(SdkV1TestCaseContainer? x, SdkV1TestCaseContainer? y) => string.Equals(x?.Hash, y?.Hash, StringComparison.Ordinal);

    public int GetHashCode(SdkV1TestCaseContainer? obj) => obj?.Hash.GetHashCode() ?? 0;
}