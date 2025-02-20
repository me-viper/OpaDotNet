using System.Buffers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Nodes;

using Microsoft.IdentityModel.Tokens;

using OpaDotNet.Wasm.GoCompat;

namespace OpaDotNet.Wasm;

public partial class DefaultOpaImportsAbi
{
    private static string HashMd5(string x)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(x));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    private static string HashSha1(string x)
    {
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(x));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    private static string HashSha256(string x)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(x));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    private static bool HmacEqual(string mac1, string mac2)
    {
        var b1 = Encoding.UTF8.GetBytes(mac1);
        var b2 = Encoding.UTF8.GetBytes(mac2);

        if (b1.Length != b2.Length)
            return false;

        var result = 0;

        for (var i = 0; i < b1.Length; i++)
            result |= b1[i] ^ b2[i];

        return result == 0;
    }

    private static string HmacMd5(string x, string key)
    {
        var bytes = HMACMD5.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(x));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    private static string HmacSha1(string x, string key)
    {
        var bytes = HMACSHA1.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(x));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    private static string HmacSha256(string x, string key)
    {
        var bytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(x));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    private static string HmacSha512(string x, string key)
    {
        var bytes = HMACSHA512.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(x));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    internal record CertValidationResult(bool IsValid, X509CertJson[] Cert);

    private static object[] X509ParseAndVerifyCertificates(string certs)
    {
        var chain = X509ParseInternal(certs);
        return X509Verify(chain.ToArray());
    }

    private static X509CertJson[]? X509ParseCertificates(string certs)
    {
        if (string.IsNullOrEmpty(certs))
            return null;

        var result = X509ParseInternal(certs);
        return result.Select(X509CertJson.ToJson).ToArray();
    }

    private static X509Certificate2Collection X509ParseInternal(ReadOnlySpan<char> certs)
    {
        var chain = new X509Certificate2Collection();

        if (IsRawPem(certs))
        {
            chain.ImportFromPem(certs);
            return chain;
        }

        var bytes = ArrayPool<byte>.Shared.Rent(certs.Length);
        char[]? decodedChars = null;

        try
        {
            if (!Convert.TryFromBase64Chars(certs, bytes, out var bw))
                throw new FormatException("Failed to parse certificate");

            ReadOnlySpan<byte> decodedBytes = bytes.AsSpan(0, bw);

            if (!IsRawPem(decodedBytes))
            {
#if NET9_0_OR_GREATER
                var cert = X509CertificateLoader.LoadCertificate(decodedBytes);
                chain.Add(cert);
#else
                chain.Import(decodedBytes);
#endif
            }
            else
            {
                decodedChars = ArrayPool<char>.Shared.Rent(decodedBytes.Length);
                var chars = Encoding.UTF8.GetChars(decodedBytes, decodedChars);
                chain.ImportFromPem(decodedChars.AsSpan(0, chars));
            }

            return chain;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bytes);

            if (decodedChars != null)
                ArrayPool<char>.Shared.Return(decodedChars);
        }
    }

    private static object[] X509Verify(X509Certificate2[] certs)
    {
        if (certs.Length <= 1)
            return [false, Array.Empty<object>()];

        var leaf = certs[^1];

        var chain = new X509Certificate2Collection(certs[..^1]);

        using var validator = new X509Chain();

        for (var i = 1; i < certs.Length - 1; i++)
            validator.ChainPolicy.ExtraStore.Add(chain[i]);

        validator.ChainPolicy.CustomTrustStore.Add(chain[0]);
        validator.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        validator.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

        var isValid = validator.Build(leaf);

        if (!isValid)
            return [false, Array.Empty<object>()];

        return [true, certs.Select(X509CertJson.ToJson).ToArray()];
    }

    private static JsonWebKey X509ParseRsaPrivateKey(string pem)
    {
        var decodedPem = Convert.FromBase64String(pem);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(decodedPem));
        var k = new RsaSecurityKey(rsa);
        return JsonWebKeyConverter.ConvertFromRSASecurityKey(k);
    }

    private static object X509ParseCertificateRequest(ReadOnlySpan<char> csr)
    {
        var csrBuf = ArrayPool<char>.Shared.Rent(csr.Length);

        try
        {
            DecodePemOrDer("CERTIFICATE REQUEST", csr, csrBuf, out var cw);

            var result = CertificateRequest.LoadSigningRequestPem(
                csrBuf.AsSpan(0, cw),
                HashAlgorithmName.MD5,
                CertificateRequestLoadOptions.SkipSignatureValidation | CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions
                );

            // I've got no idea how to map this to expected https://pkg.go.dev/crypto/x509#CertificateRequest.
            // Further investigation required.
            return new
            {
                Subject = X500DnJson.ToJson(result.SubjectName),
                RawSubject = Convert.ToBase64String(result.SubjectName.RawData),
            };
        }
        finally
        {
            ArrayPool<char>.Shared.Return(csrBuf);
        }
    }

    private static object X509ParseKeypair(ReadOnlySpan<char> cert, ReadOnlySpan<char> pem)
    {
        ReadOnlySpan<char> decodedCert;
        ReadOnlySpan<char> decodedPem;

        char[]? certBuf = null;
        char[]? pemBuf = null;

        if (IsRawPem(cert))
            decodedCert = cert;
        else
        {
            certBuf = ArrayPool<char>.Shared.Rent(cert.Length);
            DecodePemOrDer("CERTIFICATE", cert, certBuf, out var cw);
            decodedCert = certBuf.AsSpan(0, cw);
        }

        if (IsRawPem(pem))
            decodedPem = pem;
        else
        {
            pemBuf = ArrayPool<char>.Shared.Rent(pem.Length);
            DecodePemOrDer("PRIVATE KEY", pem, pemBuf, out var cw);
            decodedPem = pemBuf.AsSpan(0, cw);
        }

        try
        {
            using var result = X509Certificate2.CreateFromPem(decodedCert, decodedPem);
            var keys = CryptoParsePrivateKeys(decodedPem);
            var certJson = X509CertJson.ToJson(result);

            return new
            {
                Certificate = new[] { certJson.Raw },
                PrivateKey = keys.Length > 0 ? keys[0] : null,
            };
        }
        finally
        {
            if (certBuf != null)
                ArrayPool<char>.Shared.Return(certBuf);

            if (pemBuf != null)
                ArrayPool<char>.Shared.Return(pemBuf);
        }
    }

    private static object[]? CryptoParsePrivateKeys(ReadOnlySpan<char> keys)
    {
        if (keys.IsEmpty)
            return null;

        char[]? buf = null;

        try
        {
            ReadOnlySpan<char> decodedKeys;

            if (IsRawPem(keys))
                decodedKeys = keys;
            else
            {
                buf = ArrayPool<char>.Shared.Rent(keys.Length);

                DecodePem(keys, buf, out var charsWritten);
                decodedKeys = buf.AsSpan(0, charsWritten);
            }

            var iterations = 0;
            var result = new List<object>();

            while (PemEncoding.TryFind(decodedKeys, out var pem))
            {
                if (iterations++ > 1000)
                    throw new InvalidOperationException("Too many iterations");

                if (TryParsePrivateKey(decodedKeys[pem.Label], decodedKeys[pem.Location], out var k))
                    result.Add(k);

                decodedKeys = decodedKeys[pem.Location.End..];
            }

            return result.ToArray();
        }
        finally
        {
            if (buf != null)
                ArrayPool<char>.Shared.Return(buf);
        }
    }

    private static bool IsRawPem(ReadOnlySpan<char> pem) => pem.StartsWith("-----BEGIN");
    private static bool IsRawPem(ReadOnlySpan<byte> pem) => pem.StartsWith("-----BEGIN"u8);

    private static void DecodePemOrDer(
        ReadOnlySpan<char> type,
        ReadOnlySpan<char> keys,
        Span<char> result,
        out int charsWritten)
    {
        var bytes = ArrayPool<byte>.Shared.Rent(keys.Length);

        try
        {
            if (!Convert.TryFromBase64Chars(keys, bytes, out var bw))
            {
                keys.CopyTo(result);
                charsWritten = keys.Length;
                return;
            }

            if (!IsRawPem(bytes))
            {
                var pem = PemEncoding.Write(type, bytes.AsSpan(0, bw));
                pem.CopyTo(result);
                charsWritten = pem.Length;
                return;
            }

            charsWritten = Encoding.UTF8.GetChars(bytes.AsSpan(0, bw), result);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }
    }

    private static void DecodePem(ReadOnlySpan<char> keys, Span<char> result, out int charsWritten)
    {
        var bytes = ArrayPool<byte>.Shared.Rent(keys.Length);

        try
        {
            if (!Convert.TryFromBase64Chars(keys, bytes, out var bw))
            {
                keys.CopyTo(result);
                charsWritten = keys.Length;
                return;
            }

            charsWritten = Encoding.UTF8.GetChars(bytes.AsSpan(0, bw), result);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }
    }

    private static bool TryParsePrivateKey(
        ReadOnlySpan<char> label,
        ReadOnlySpan<char> key,
        [MaybeNullWhen(false)] out object result)
    {
        result = null;

        // PEM Label              | Import method on RSA
        // -------------------------------------------------------
        // RSA PRIVATE KEY        | RSA.ImportRSAPrivateKey
        // PRIVATE KEY            | RSA.ImportPkcs8PrivateKey
        // ENCRYPTED PRIVATE KEY  | RSA.ImportEncryptedPkcs8PrivateKey
        // RSA PUBLIC KEY         | RSA.ImportRSAPublicKey
        // PUBLIC KEY             | RSA.ImportSubjectPublicKeyInfo
        // EC PRIVATE KEY         | ECDsa.ImportSubjectPublicKeyInfo

        try
        {
            if (label is "EC PRIVATE KEY")
            {
                using var ecsa = ECDsa.Create();
                ecsa.ImportFromPem(key);
                var k = ecsa.ExportParameters(true);

                result = new { D = k.D.ToJson(), X = k.Q.X.ToJson(), Y = k.Q.Y.ToJson(), k.Curve };

                return true;
            }

            if (label is "RSA PRIVATE KEY" or "PRIVATE KEY")
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(key);
                var k = rsa.ExportParameters(true);

                result = new
                {
                    D = k.D.ToJson(),
                    E = k.Exponent.ToJson(),
                    N = k.Modulus.ToJson(),
                    Primes = new[] { k.P.ToJson(), k.Q.ToJson() },
                    Precomputed = new
                    {
                        Dp = k.DP.ToJson(),
                        Dq = k.DQ.ToJson(),
                        Qinv = k.InverseQ.ToJson(),
                    },
                };

                return true;
            }
        }
        catch (Exception)
        {
            // ignored
        }

        return false;
    }
}