namespace OpaDotNet.Wasm.Features;

/// <summary>
/// External data manipulation.
/// </summary>
public interface IUpdateDataFeature : IOpaEvaluatorFeature
{
    /// <summary>
    /// Updates external data.
    /// </summary>
    /// <param name="dataJson">JSON to insert or update</param>
    /// <param name="path">
    /// The array with path elements.
    /// Must be an array value with string keys (eg: <c>["a", "b", "c"]</c>)
    /// </param>
    /// <example>
    /// Base object <c>{"a": {"b": 123 }}</c>, path <c>["a", "x", "y"]</c>,
    /// and value <c>{"foo": "bar"}</c>
    /// </example>
    void UpdateDataPath(ReadOnlySpan<char> dataJson, IEnumerable<string> path);

    /// <summary>
    /// Removes JSON from external data.
    /// </summary>
    /// <param name="path">
    /// The array with path elements.
    /// Must be an array value with string keys (eg: <c>["a", "b", "c"]</c>)
    /// </param>
    /// <example>
    /// Base object <c>{"a": {"b": 123, "x": {"y": {"foo": "bar" }}}}</c>, path <c>["a", "x"]</c>
    /// will yield <c>{"a": {"b": 123 }}</c>.
    /// </example>
    void RemoveDataPath(IEnumerable<string> path);
}