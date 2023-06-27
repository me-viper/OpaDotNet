namespace OpaDotNet.Wasm.Features;

public interface IUpdateDataFeature : IOpaEvaluatorFeature
{
    void UpdateDataPath(ReadOnlySpan<char> dataJson, IEnumerable<string> path);
    
    void RemoveDataPath(IEnumerable<string> path);
}