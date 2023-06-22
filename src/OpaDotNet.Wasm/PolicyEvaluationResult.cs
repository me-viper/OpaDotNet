using System.Text.Json.Serialization;

using JetBrains.Annotations;

namespace OpaDotNet.Wasm;

[PublicAPI]
public class PolicyEvaluationResult<T> where T : notnull
{
    [JsonPropertyName("result")]
    public T Result { get; set; } = default!;
}