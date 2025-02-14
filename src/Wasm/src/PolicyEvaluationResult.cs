using System.Text.Json.Serialization;

namespace OpaDotNet.Wasm;

/// <summary>
/// Policy evaluation result.
/// </summary>
/// <typeparam name="T">The type of object returned by the policy.</typeparam>
[PublicAPI]
public class PolicyEvaluationResult<T>
{
    /// <summary>
    /// Policy result.
    /// </summary>
    [JsonPropertyName("result")]
    public T Result { get; set; } = default!;
}