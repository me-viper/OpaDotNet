using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Microsoft.IdentityModel.Tokens;

namespace OpaDotNet.Wasm.Internal;

internal static class SecurityKeyHelpers
{
    public static bool TryReadPemKey(string pem, out SecurityKey? result)
    {
        result = null;

        if (pem.Contains("-----BEGIN CERTIFICATE"))
        {
#pragma warning disable CA2000
            var cert = X509Certificate2.CreateFromPem(pem);
#pragma warning restore CA2000

            SecurityKey k;

            if (cert.GetECDsaPublicKey() != null)
                k = new ECDsaSecurityKey(cert.GetECDsaPublicKey());
            else if (cert.GetRSAPublicKey() != null)
                k = new RsaSecurityKey(cert.GetRSAPublicKey());
            else
                k = new X509SecurityKey(cert);

            result = k;

            return true;
        }

        if (pem.Contains("-----BEGIN"))
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            result = new RsaSecurityKey(rsa.ExportParameters(false));

            return true;
        }

        return false;
    }
}