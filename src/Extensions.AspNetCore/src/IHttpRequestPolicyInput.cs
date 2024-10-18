using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

using JetBrains.Annotations;

using Microsoft.AspNetCore.Http;

namespace OpaDotNet.Extensions.AspNetCore;

[PublicAPI]
public interface IConnectionInput
{
    string? RemoteIpAddress { get; }

    int RemotePort { get; }

    string? LocalIpAddress { get; }

    int LocalPort { get; }
}

[PublicAPI]
[JsonConverter(typeof(ClaimPolicyInputJsonSerializer))]
public record ClaimPolicyInput(string Type, string Value);

internal class ClaimPolicyInputJsonSerializer : JsonConverter<ClaimPolicyInput>
{
    public override ClaimPolicyInput Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("Deserialization is not supported");
    }

    public override void Write(Utf8JsonWriter writer, ClaimPolicyInput value, JsonSerializerOptions options)
    {
        var typeProp = options.PropertyNamingPolicy?.ConvertName(nameof(ClaimPolicyInput.Type)) ?? nameof(ClaimPolicyInput.Type);
        var valProp = options.PropertyNamingPolicy?.ConvertName(nameof(ClaimPolicyInput.Value)) ?? nameof(ClaimPolicyInput.Value);

        writer.WriteStartObject();
        writer.WriteString(typeProp, value.Type);
        writer.WritePropertyName(valProp);

        if (value.Value.Length > 1 && value.Value[0] == '[')
            writer.WriteRawValue(value.Value);
        else
            writer.WriteStringValue(value.Value);

        writer.WriteEndObject();
    }
}

[PublicAPI]
public interface IHttpRequestPolicyInput
{
    string Method { get; }

    string Scheme { get; }

    string Host { get; }

    string? PathBase { get; }

    string? Path { get; }

    string? QueryString { get; }

    IQueryCollection Query { get; }

    string Protocol { get; }

    IConnectionInput Connection { get; }

    IDictionary<string, string?> Headers { get; }

    IEnumerable<ClaimPolicyInput> Claims { get; }

    static IHttpRequestPolicyInput Build(
        HttpRequest request,
        IReadOnlySet<string> includedHeaders,
        IEnumerable<Claim>? claims)
    {
        return new HttpRequestPolicyInput(request, includedHeaders, claims);
    }
}