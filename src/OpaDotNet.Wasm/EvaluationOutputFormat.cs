namespace OpaDotNet.Wasm;

/// <summary>
/// Policy evaluation result output format.
/// </summary>
internal enum EvaluationOutputFormat
{
    /// <summary>
    /// JSON
    /// </summary>
    Json,
    
    /// <summary>
    /// Serialized Rego values.
    /// </summary>
    Value
}