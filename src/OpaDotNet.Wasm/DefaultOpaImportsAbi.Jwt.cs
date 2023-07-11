using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using JetBrains.Annotations;

using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

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

        [JsonPropertyName("iis")]
        public string? Iss { get; [UsedImplicitly] set; }

        [JsonPropertyName("time")]
        public string? Time { get; [UsedImplicitly] set; }

        [JsonPropertyName("aud")]
        public string? Aud { get; [UsedImplicitly] set; }
    }

    private static object[] JwtDecode(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        var sig = Base64UrlDecode(token.RawSignature);
        return new object[] { token.Header, token.Payload, Convert.ToHexString(sig).ToLowerInvariant() };
    }

    private static object[] JwtDecodeVerify(string jwt, JwtConstraints? constraints)
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

    private static TokenValidationParameters? MakeTokenValidationParameters(JwtConstraints constraints)
    {
        var result = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            RequireExpirationTime = false,
        };

        if (!string.IsNullOrWhiteSpace(constraints.Alg))
            result.ValidAlgorithms = new[] { constraints.Alg };

        if (!string.IsNullOrWhiteSpace(constraints.Time))
        {
            result.RequireExpirationTime = true;

            if (!long.TryParse(constraints.Time, out var time))
                return null;

            result.LifetimeValidator = (before, expires, _, _) =>
            {
                var now = new DateTimeOffset(time / 100, TimeSpan.Zero);
                return now.Date > before && now.Date < expires;
            };
        }

        if (!string.IsNullOrWhiteSpace(constraints.Secret))
            result.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(constraints.Secret));
        else
        {
            if (string.IsNullOrWhiteSpace(constraints.Cert))
                return null;

            try
            {
                if (constraints.Cert.IndexOf("-----BEGIN CERTIFICATE", StringComparison.Ordinal) >= 0)
                {
                    var cert = X509Certificate2.CreateFromPem(constraints.Cert);

                    SecurityKey k;

                    if (cert.GetECDsaPublicKey() != null)
                        k = new ECDsaSecurityKey(cert.GetECDsaPublicKey());
                    else if (cert.GetRSAPublicKey() != null)
                        k = new RsaSecurityKey(cert.GetRSAPublicKey());
                    else
                        k = new X509SecurityKey(cert);

                    result.IssuerSigningKey = k;
                }
                else if (constraints.Cert.IndexOf("-----BEGIN", StringComparison.Ordinal) >= 0)
                {
                    var rsa = RSA.Create();
                    rsa.ImportFromPem(constraints.Cert);
                    result.IssuerSigningKey = new RsaSecurityKey(rsa);
                }
                else
                {
                    var jwks = new JsonWebKeySet(constraints.Cert);
                    result.IssuerSigningKeys = jwks.Keys;
                }
            }
            catch (Exception)
            {
                return null;
            }
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
        IdentityModelEventSource.ShowPII = true;
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

    private static bool JwtVerifyHs(string jwt, string secret, string alg)
    {
        var tvp = MakeTokenValidationParameters(new() { Secret = secret, Alg = alg });

        if (tvp == null)
            return false;

        return ValidateToken(jwt, tvp) != null;
    }

    private static bool JwtVerifyCert(string jwt, string cert, string alg)
    {
        var tvp = MakeTokenValidationParameters(new() { Cert = cert, Alg = alg });

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
        var jwtPayload = JsonExtensions.DeserializeJwtPayload(payload);
        var baseHeader = JsonExtensions.DeserializeJwtHeader(headers);

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