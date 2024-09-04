namespace OpaDotNet.Wasm.Rego;

internal class RegoSet<T>
{
    public IEnumerable<T> Set { get; }

    public RegoSet(IEnumerable<T> set, IEqualityComparer<T>? comparer = null)
    {
        Set = set.ToHashSet(comparer);
    }

    public override string ToString()
    {
        return $"{{ {string.Join(',', Set)} }}";
    }
}