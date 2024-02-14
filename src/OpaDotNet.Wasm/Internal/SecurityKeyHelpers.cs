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
            var cert = X509Certificate2.CreateFromPem(pem);

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
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            result = new RsaSecurityKey(rsa);

            return true;
        }

        return false;
    }
}