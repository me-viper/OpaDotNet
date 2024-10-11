using System.IdentityModel.Tokens.Jwt;
using System.Text;

using Microsoft.IdentityModel.Tokens;

using OpaDotNet.Wasm.Internal;

namespace OpaDotNet.Wasm.Validation;

internal class DefaultBundleSignatureValidator : IBundleSignatureValidator
{
    public IReadOnlySet<SignedFile> Validate(BundleSignatures signatures, SignatureValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(signatures);
        ArgumentNullException.ThrowIfNull(options);

        if (signatures.Signatures.Count != 1)
        {
            throw new BundleSignatureValidationException(
                $"Expected exactly one signature, got {signatures.Signatures.Count}"
                );
        }

        if (string.IsNullOrWhiteSpace(options.VerificationKey) && string.IsNullOrWhiteSpace(options.VerificationKeyPath))
            throw new BundleSignatureValidationException("No token verification key specified");

        var verificationKey = options.VerificationKey ?? File.ReadAllText(options.VerificationKeyPath!);

        if (!SecurityKeyHelpers.TryReadPemKey(verificationKey, out var key))
            key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(verificationKey));

        var sigParams = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateActor = false,
            ValidateLifetime = false,
            IssuerSigningKey = key,
            ValidAlgorithms = [options.SigningAlgorithm],
        };

        JwtSecurityToken? sig;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(signatures.Signatures[0], sigParams, out var token);
            sig = token as JwtSecurityToken;

            if (sig == null)
                throw new BundleSignatureValidationException("Expected JWT token");

            if (!string.IsNullOrWhiteSpace(sig.Header.Kid))
            {
                if (!string.Equals(sig.Header.Kid, options.VerificationKeyId, StringComparison.Ordinal))
                    throw new BundleSignatureValidationException("KeyId mismatch");
            }

            // If supplied in the payload, must match exactly the value provided out-of-band to OPA.
            if (!string.IsNullOrWhiteSpace(options.Scope))
            {
                if (!sig.Claims.Any(
                    p => p.Type.Equals("scope", StringComparison.Ordinal)
                        && options.Scope.Equals(p.Value, StringComparison.Ordinal)
                    ))
                {
                    throw new BundleSignatureValidationException("JWT scope mismatch");
                }
            }
        }
        catch (SecurityTokenValidationException ex)
        {
            throw new BundleSignatureValidationException("Token validation failed", ex);
        }

        var files = sig.Payload.Claims
            .Where(p => string.Equals(p.Type, "files", StringComparison.Ordinal))
            .Select(p => JsonSerializer.Deserialize<SignedFile>(p.Value))
            .Where(p => p != null)
            .Cast<SignedFile>();

        return files.ToHashSet();
    }
}