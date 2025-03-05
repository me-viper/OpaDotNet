using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

namespace OpaDotNet.Wasm.GoCompat;

// GoLang X509 certs are completely different form dotnet implementation in terms of properties being exposed.
// It's not an attempt to be 100% compatible rather trying to return something at least remotely similar to
// https://pkg.go.dev/crypto/x509#Certificate.
[PublicAPI]
internal record X509CertJson
{
    public int Version { get; set; }

    public DateTime NotAfter { get; set; }

    public DateTime NotBefore { get; set; }

    public X500DnJson? Issuer { get; set; }

    public X500DnJson? Subject { get; set; }

    [JsonPropertyName("DNSNames")]
    public string[]? DnsNames { get; set; }

    public string[]? EmailAddresses { get; set; }

    [JsonPropertyName("IsCA")]
    public bool IsCa { get; set; }

    public int MaxPathLen { get; set; }

    public bool MaxPathLenZero { get; set; }

    public int KeyUsage { get; set; }

    public string? SubjectKeyId { get; set; }

    public string? AuthorityKeyId { get; set; }

    public string? SerialNumber { get; set; }

    public string? Signature { get; set; }

    public string? Raw { get; set; }

    public string? RawSubject { get; set; }

    //public string? RawSubjectPublicKeyInfo { get; set; }

    public string? RawIssuer { get; set; }

    public HashSet<X509ExtJson> Extensions { get; set; } = new();

    public string? Thumbprint { get; set; }

    [JsonPropertyName("URIStrings")]
    public string[]? UriStrings { get; set; }


    public static X509CertJson ToJson(X509Certificate2 source)
    {
        var dns1 = source.GetNameInfo(X509NameType.DnsFromAlternativeName, false);
        var email = source.GetNameInfo(X509NameType.EmailName, false);
        var uri = source.GetNameInfo(X509NameType.UrlName, false);

        var dns = new HashSet<string>();

        if (!string.IsNullOrWhiteSpace(dns1))
        {
            if (Uri.CheckHostName(dns1) != UriHostNameType.Unknown)
                dns.Add(dns1);
        }

        var result = new X509CertJson
        {
            NotBefore = source.NotBefore,
            NotAfter = source.NotAfter,
            DnsNames = dns.Count > 0 ? dns.ToArray() : null,
            EmailAddresses = string.IsNullOrWhiteSpace(email) ? null : [email],
            Issuer = X500DnJson.ToJson(source.IssuerName),
            Subject = X500DnJson.ToJson(source.SubjectName),
            Version = source.Version,
            SerialNumber = source.GetSerialNumberString(),
            Signature = Convert.ToBase64String(source.GetX509Signature()),
            Raw = Convert.ToBase64String(source.RawData),
            RawSubject = Convert.ToBase64String(source.SubjectName.RawData),
            RawIssuer = Convert.ToBase64String(source.IssuerName.RawData),
            Thumbprint = source.Thumbprint,
            UriStrings = string.IsNullOrWhiteSpace(uri) ? null : [uri],
        };

        static int MapKeyUsage(X509KeyUsageFlags kuFlags)
        {
            var result = 0;

            if (kuFlags.HasFlag(X509KeyUsageFlags.DigitalSignature))
                result |= 1;

            if (kuFlags.HasFlag(X509KeyUsageFlags.NonRepudiation))
                result |= 1 << 1;

            if (kuFlags.HasFlag(X509KeyUsageFlags.KeyEncipherment))
                result |= 1 << 2;

            if (kuFlags.HasFlag(X509KeyUsageFlags.DataEncipherment))
                result |= 1 << 3;

            if (kuFlags.HasFlag(X509KeyUsageFlags.KeyAgreement))
                result |= 1 << 4;

            if (kuFlags.HasFlag(X509KeyUsageFlags.KeyCertSign))
                result |= 1 << 5;

            if (kuFlags.HasFlag(X509KeyUsageFlags.CrlSign))
                result |= 1 << 6;

            if (kuFlags.HasFlag(X509KeyUsageFlags.EncipherOnly))
                result |= 1 << 7;

            if (kuFlags.HasFlag(X509KeyUsageFlags.DecipherOnly))
                result |= 1 << 8;

            return result;
        }

        static X509ExtJson MapExtension(X509Extension ext)
        {
            return new()
            {
                Critical = ext.Critical,
                Value = Convert.ToBase64String(ext.RawData),
                Id = ext.Oid?.ToIntArray(),
            };
        }

        foreach (var ext in source.Extensions)
        {
            switch (ext)
            {
                case X509BasicConstraintsExtension bce:
                    result.IsCa = bce.CertificateAuthority;
                    result.MaxPathLenZero = bce.HasPathLengthConstraint;
                    result.MaxPathLen = bce.PathLengthConstraint;
                    break;

                case X509KeyUsageExtension kue:
                    result.KeyUsage = MapKeyUsage(kue.KeyUsages);
                    break;

                case X509SubjectKeyIdentifierExtension skie:
                    result.SubjectKeyId = Convert.ToBase64String(skie.SubjectKeyIdentifierBytes.Span);
                    break;

                case X509AuthorityKeyIdentifierExtension akie:
                    result.AuthorityKeyId = akie.KeyIdentifier == null
                        ? null
                        : Convert.ToBase64String(akie.KeyIdentifier.Value.Span);

                    break;
            }

            result.Extensions.Add(MapExtension(ext));
        }

        return result;
    }
}