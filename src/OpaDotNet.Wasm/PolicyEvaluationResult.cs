using System.Text.Json.Serialization;

using JetBrains.Annotations;

namespace OpaDotNet.Wasm;

/// <summary>
/// Policy evaluation result.
/// </summary>
/// <typeparam name="T">The type of object returned by the policy.</typeparam>
[PublicAPI]
public class PolicyEvaluationResult<T> where T : notnull
{
    /// <summary>
    /// Policy result.
    /// </summary>
    [JsonPropertyName("result")]
    public T Result { get; set; } = default!;
}