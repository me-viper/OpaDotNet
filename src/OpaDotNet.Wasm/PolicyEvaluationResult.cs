using System.Text.Json.Serialization;

namespace OpaDotNet.Wasm;

public class PolicyEvaluationResult<T> where T : notnull
{
    [JsonPropertyName("result")]
    public T Result { get; set; } = default!;
}