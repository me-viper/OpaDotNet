namespace OpaDotNet.Wasm.Rego;

internal class RegoSetOfAny : RegoSet<object>
{
    public RegoSetOfAny(IEnumerable<object> set, IEqualityComparer<object>? comparer = null)
        : base(set, comparer)
    {
    }
}