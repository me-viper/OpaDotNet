using System.Numerics;
using System.Text.Json.Serialization;

namespace OpaDotNet.Wasm.GoCompat;

[JsonConverter(typeof(BigIntJsonConverter))]
internal record BigIntJson(BigInteger N);