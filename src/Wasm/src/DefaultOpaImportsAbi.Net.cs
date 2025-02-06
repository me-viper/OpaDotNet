extern alias Ipn;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;

using OpaDotNet.Wasm.Rego;

using IPNetwork2 = Ipn::System.Net.IPNetwork2;

namespace OpaDotNet.Wasm;

public partial class DefaultOpaImportsAbi
{
    private record CidrOrIp(IPNetwork2 Net, JsonNode Key)
    {
        private static CidrOrIp Parse(JsonNode? node, JsonSerializerOptions options, object? key = null)
        {
            static JsonNode MakeKey(object key)
            {
                if (key is JsonNode jn)
                    return jn;

                return JsonValue.Create(key)!;
            }

            if (node is JsonValue jv)
            {
                var k = key == null ? jv : MakeKey(key);
                return new(ParseNetwork(jv.GetValue<string>()), k);
            }

            if (node is JsonArray ja)
            {
                string net;

                if (!ja.IsRegoSet())
                {
                    if (!ja.TryGetTuple<string>(out var set))
                        throw new FormatException($"Format {node} is not supported");

                    net = set.Item1;
                }
                else
                {
                    if (!ja.TryGetRegoSet<JsonNode>(out var set, options))
                        throw new FormatException($"Format {node} is not supported");

                    net = set.Set.First().ToString();
                }

                var k = key == null ? ja : MakeKey(key);
                return new(ParseNetwork(net), k);
            }

            throw new FormatException($"Format {node} is not supported");
        }

        public static List<CidrOrIp> ParseAll(JsonNode node, JsonSerializerOptions options)
        {
            var result = new List<CidrOrIp>();

            if (node is JsonArray ja)
            {
                if (ja.IsRegoSet())
                {
                    if (!ja.TryGetRegoSet<JsonNode>(out var set, options))
                        throw new FormatException($"Format {node} is not supported");

                    foreach (var s in set.Set)
                        result.Add(Parse(s, options, s));

                    return result;
                }

                for (var i = 0; i < ja.Count; i++)
                    result.Add(Parse(ja[i], options, i));

                return result;
            }

            if (node is JsonValue jv)
            {
                result.Add(Parse(jv, options));
                return result;
            }

            if (node is JsonObject jo)
            {
                foreach (var (key, n) in jo)
                    result.Add(Parse(n, options, key));

                return result;
            }

            throw new FormatException($"Format {node} is not supported");
        }
    }

    private static RegoSet<object>? CidrContainsMatches(JsonNode? cidrs, JsonNode? cidrOrIps, JsonSerializerOptions options)
    {
        if (cidrs == null || cidrOrIps == null)
            return null;

        var x = CidrOrIp.ParseAll(cidrs, options).ToArray();
        var y = CidrOrIp.ParseAll(cidrOrIps, options).ToArray();

        return CidrContainsMatches(x, y);
    }

    private static RegoSet<object> CidrContainsMatches(CidrOrIp[] cidrs, CidrOrIp[] cidrOrIps)
    {
        var results = new List<JsonNode[]>();

        foreach (var c in cidrs)
        {
            foreach (var t in cidrOrIps)
            {
                if (c.Net.Contains(t.Net))
                    results.Add([c.Key, t.Key]);
            }
        }

        return new(results.ToArray());
    }

    private static RegoSet<string> CidrExpand(string cidr)
    {
        if (!TryParseNetwork(cidr, out var result))
            throw new ArgumentException("Invalid CIDR address", nameof(cidr));

        var r = result.ListIPAddress().Select(p => p.ToString());

        return new RegoSet<string>(r);
    }

    private static bool CidrIsValid(string cidr)
    {
        return IPNetwork2.TryParse(cidr, out _);
    }

    private static object CidrMerge(string[] addresses)
    {
        if (addresses.Length == 0)
            return new JsonArray();

        var nets = addresses.Select(ParseNetwork).ToArray();
        var result = IPNetwork2.Supernet(nets).Select(p => p.ToString()).ToArray();
        return new RegoSet<string>(result);
    }

    private static RegoSet<string>? LookupIpAddress(string name)
    {
        var result = new List<string>();

        if (IPAddress.TryParse(name, out var ip))
        {
            result.Add(ip.ToString());
            return new(result);
        }

        try
        {
            var ipv4 = Dns.GetHostEntry(name, AddressFamily.Unspecified);
            result.AddRange(ipv4.AddressList.Select(p => p.ToString()));
            return new(result);
        }
        catch (SocketException)
        {
            return null;
        }
    }

    private static IPNetwork2 ParseNetwork(string cidrOrIp)
    {
        if (TryParseNetwork(cidrOrIp, out var result))
            return result;

        throw new FormatException($"Invalid IP/CIDR format '{cidrOrIp}'");
    }

    private static bool TryParseNetwork(string cidrOrIp, [MaybeNullWhen(false)] out IPNetwork2 result)
    {
        result = null;

        if (IPAddress.TryParse(cidrOrIp, out var ip))
        {
            result = new IPNetwork2(ip, 24);
            return true;
        }

        return IPNetwork2.TryParse(cidrOrIp, out result);
    }
}