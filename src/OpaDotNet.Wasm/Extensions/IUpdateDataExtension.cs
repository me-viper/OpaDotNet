namespace OpaDotNet.Wasm.Extensions;

public interface IUpdateDataExtension : IOpaEvaluatorExtension
{
    void UpdateDataPath(ReadOnlySpan<char> dataJson, IEnumerable<string> path);
    
    void RemoveDataPath(IEnumerable<string> path);
}