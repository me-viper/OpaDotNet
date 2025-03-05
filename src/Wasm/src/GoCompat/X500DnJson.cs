using System.Security.Cryptography.X509Certificates;

namespace OpaDotNet.Wasm.GoCompat;

[PublicAPI]
internal record X500DnJson
{
    public string? CommonName { get; set; } = string.Empty;

    public string? SerialNumber { get; set; }

    public HashSet<string>? Country { get; set; }

    public HashSet<string>? Organization { get; set; }

    public HashSet<string>? OrganizationalUnit { get; set; }

    public HashSet<string>? Locality { get; set; }

    public HashSet<string>? Province { get; set; }

    public HashSet<string>? StreetAddress { get; set; }

    public HashSet<X509NamesJson> Names { get; set; } = new();

    public static X500DnJson ToJson(X500DistinguishedName dn)
    {
        var result = new X500DnJson();
        var names = dn.EnumerateRelativeDistinguishedNames();

        foreach (var n in names)
        {
            if (n.HasMultipleElements)
                continue;

            var type = n.GetSingleElementType().FriendlyName;

            if (string.IsNullOrWhiteSpace(type))
                continue;

            var val = n.GetSingleElementValue();

            result.Names.Add(
                new()
                {
                    Id = n.GetSingleElementType().ToIntSet(),
                    Value = val,
                }
                );

            if (string.IsNullOrWhiteSpace(val))
                continue;

            switch (type)
            {
                case "CN":
                    result.CommonName = val;
                    break;
                case "C":
                    result.Country ??= new();
                    result.Country.Add(val);
                    break;
                case "O":
                    result.Organization ??= new();
                    result.Organization.Add(val);
                    break;
                case "OU":
                    result.OrganizationalUnit ??= new();
                    result.OrganizationalUnit.Add(val);
                    break;
                case "L":
                    result.Locality ??= new();
                    result.Locality.Add(val);
                    break;
                case "S" or "ST":
                    result.Province ??= new();
                    result.Province.Add(val);
                    break;
                case "STREET":
                    result.StreetAddress ??= new();
                    result.StreetAddress.Add(val);
                    break;
                case "SERIALNUMBER":
                    result.SerialNumber = val;
                    break;
            }
        }

        return result;
    }
}