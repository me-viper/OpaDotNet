using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

using OpaDotNet.Wasm.Internal;

namespace OpaDotNet.Wasm;

public partial class DefaultOpaImportsAbi
{
    private class JwtConstraints
    {
        [JsonPropertyName("cert")]
        public string? Cert { get; [UsedImplicitly] set; }

        [JsonPropertyName("secret")]
        public string? Secret { get; [UsedImplicitly] set; }

        [JsonPropertyName("alg")]
        public string? Alg { get; [UsedImplicitly] set; }

        [JsonPropertyName("iss")]
        public string? Iss { get; [UsedImplicitly] set; }

        [JsonPropertyName("time")]
        public long? Time { get; [UsedImplicitly] set; }

        [JsonPropertyName("aud")]
        public string? Aud { get; [UsedImplicitly] set; }

        [JsonIgnore]
        public bool SignatureOnly { get; set; }
    }

    private static object[] JwtDecode(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        var sig = Base64UrlDecode(token.RawSignature);
        return new object[] { token.Header, token.Payload, Convert.ToHexString(sig).ToLowerInvariant() };
    }

    private object[] JwtDecodeVerify(string jwt, JwtConstraints? constraints)
    {
        var emptyObj = new object();

        if (constraints == null)
            return new[] { false, emptyObj, emptyObj };

        var tvp = MakeTokenValidationParameters(constraints);

        if (tvp == null)
            return new[] { false, emptyObj, emptyObj };

        if (ValidateToken(jwt, tvp) is not JwtSecurityToken token)
            return new[] { false, emptyObj, emptyObj };

        return new object[] { true, token.Header, token.Payload };
    }

    private TokenValidationParameters? MakeTokenValidationParameters(JwtConstraints constraints)
    {
        var result = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            RequireExpirationTime = false,
            ValidateLifetime = false,
        };

        if (!string.IsNullOrWhiteSpace(constraints.Alg))
            result.ValidAlgorithms = new[] { constraints.Alg };

        if (!string.IsNullOrWhiteSpace(constraints.Secret))
            result.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(constraints.Secret));
        else
        {
            if (string.IsNullOrWhiteSpace(constraints.Cert))
                return null;

            if (SecurityKeyHelpers.TryReadPemKey(constraints.Cert, out var key))
                result.IssuerSigningKey = key;
            else
            {
                var jwks = new JsonWebKeySet(constraints.Cert);

                if (jwks.Keys.Count > 0)
                    result.IssuerSigningKeys = jwks.Keys;
                else
                {
                    var k = new JsonWebKey(constraints.Cert);
                    result.IssuerSigningKey = k;
                }
            }
        }

        if (constraints.SignatureOnly)
            return result;

        result.ValidateLifetime = true;

        if (constraints.Time != null)
        {
            result.LifetimeValidator = (before, expires, _, _) =>
            {
                var beforeNs = before?.ToUniversalTime().Subtract(DateTimeOffset.UnixEpoch.DateTime).TotalNanoseconds;
                var expiresNs = expires?.ToUniversalTime().Subtract(DateTimeOffset.UnixEpoch.DateTime).TotalNanoseconds;
                var timeNs = (double)constraints.Time;
                return (beforeNs == null || timeNs >= beforeNs) && (expiresNs == null || timeNs <= expiresNs);
            };
        }

        if (!string.IsNullOrWhiteSpace(constraints.Iss))
        {
            result.IssuerValidator = (issuer, _, _) =>
            {
                if (!string.Equals(issuer, constraints.Iss, StringComparison.Ordinal))
                    throw new SecurityTokenInvalidIssuerException("Issuer does not match any of the valid issuers.");

                return issuer;
            };
        }

        result.AudienceValidator = (audiences, _, _) =>
        {
            var auds = audiences.ToList();

            // If aud is absent then the aud claim must be absent too.
            if (string.IsNullOrWhiteSpace(constraints.Aud))
                return !auds.Any();

            return auds.Any(p => string.Equals(p, constraints.Aud, StringComparison.Ordinal));
        };

        return result;
    }

    private static SecurityToken? ValidateToken(string jwt, TokenValidationParameters parameters)
    {
#if DEBUG
        IdentityModelEventSource.ShowPII = true;
#endif

        var handler = new JwtSecurityTokenHandler();

        try
        {
            handler.ValidateToken(jwt, parameters, out SecurityToken token);
            return token;
        }
        catch (SecurityTokenValidationException)
        {
            return null;
        }
    }

    private bool JwtVerifyHs(string jwt, string secret, string alg)
    {
        var tvp = MakeTokenValidationParameters(new() { Secret = secret, Alg = alg, SignatureOnly = true });

        if (tvp == null)
            return false;

        return ValidateToken(jwt, tvp) != null;
    }

    private bool JwtVerifyCert(string jwt, string cert, string alg)
    {
        var tvp = MakeTokenValidationParameters(new() { Cert = cert, Alg = alg, SignatureOnly = true });

        if (tvp == null)
            return false;

        return ValidateToken(jwt, tvp) != null;
    }

    private static readonly string[] ReservedJwtHeaders = { "alg", "kid", "x5t", "enc", "zip" };

    private static string JwtEncodeSign(JsonNode? headers, JsonNode? payload, JsonNode? key)
    {
        return JwtEncodeSignRaw(
            JsonSerializer.Serialize(headers),
            JsonSerializer.Serialize(payload),
            JsonSerializer.Serialize(key)
            );
    }

    private static string JwtEncodeSignRaw(string headers, string payload, string key)
    {
        var jwtPayload = JwtPayload.Deserialize(payload);

        // System.IdentityModel.Tokens.Jwt 7.0+ removed deserialization helper method we've
        // been using and didn't provide alternative. So for now using the only one ugly solution available.
        var baseHeader = JwtHeader.Base64UrlDeserialize(Base64UrlEncoder.Encode(headers));

        var jwk = new JsonWebKey(key);
        var alg = baseHeader.Alg ?? jwk.Alg;
        var signingCredentials = new SigningCredentials(jwk, alg);

        // The 'alg', 'kid', 'x5t', 'enc', and 'zip' claims are added by default based on the SigningCredentials,
        // EncryptingCredentials, and/or CompressionAlgorithm provided and SHOULD NOT be included in this dictionary.
        var jwtHeader = new JwtHeader(
            signingCredentials,
            null,
            null,
            baseHeader.Where(p => !ReservedJwtHeaders.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value)
            );

        var token = new JwtSecurityToken(jwtHeader, jwtPayload);
        var handler = new JwtSecurityTokenHandler();

        return handler.WriteToken(token);
    }
}