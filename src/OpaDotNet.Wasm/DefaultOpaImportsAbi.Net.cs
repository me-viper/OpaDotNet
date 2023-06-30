﻿using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;

namespace OpaDotNet.Wasm;

public partial class DefaultOpaImportsAbi
{
    private record CidrOrIp(IPNetwork Net, JsonNode Key)
    {
        private static CidrOrIp Parse(JsonNode? node, object? key = null)
        {
            if (node is JsonValue jv)
            {
                var k = key != null ? JsonValue.Create(key) : jv;
                return new(ParseNetwork(jv.GetValue<string>()), k!);
            }

            if (node is JsonArray ja)
            {
                if (!OpaSet.TryParse<string>(ja, out var set))
                    throw new FormatException($"Format {node} is not supported");

                JsonNode k = key == null ? ja : JsonValue.Create(key)!;
                return new(ParseNetwork(set.Item1), k);
            }

            throw new FormatException($"Format {node} is not supported");
        }

        public static List<CidrOrIp> ParseAll(JsonNode node)
        {
            var result = new List<CidrOrIp>();

            if (node is JsonArray ja)
            {
                for (var i = 0; i < ja.Count; i++)
                    result.Add(Parse(ja[i], i));

                return result;
            }

            if (node is JsonValue jv)
            {
                result.Add(Parse(jv));
                return result;
            }

            if (node is JsonObject jo)
            {
                foreach (var (key, n) in jo)
                    result.Add(Parse(n, key));

                return result;
            }

            throw new FormatException($"Format {node} is not supported");
        }
    }

    private static IEnumerable<object[]>? CidrContainsMatches(JsonNode? cidrs, JsonNode? cidrOrIps)
    {
        if (cidrs == null || cidrOrIps == null)
            return null;

        try
        {
            var x = CidrOrIp.ParseAll(cidrs).ToArray();
            var y = CidrOrIp.ParseAll(cidrOrIps).ToArray();

            return CidrContainsMatches(x, y);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static IEnumerable<JsonNode[]> CidrContainsMatches(CidrOrIp[] cidrs, CidrOrIp[] cidrOrIps)
    {
        var results = new List<JsonNode[]>();

        foreach (var c in cidrs)
        {
            foreach (var t in cidrOrIps)
            {
                if (c.Net.Contains(t.Net))
                    results.Add(new[] { c.Key, t.Key });
            }
        }

        return results;
    }

    private static string[]? CidrExpand(string cidr)
    {
        if (!TryParseNetwork(cidr, out var result))
            return null;

        return result.ListIPAddress()
            .Select(p => p.ToString())
            .ToArray();
    }

    private static bool CidrIsValid(string cidr)
    {
        return IPNetwork.TryParse(cidr, out _);
    }

    private static string[] CidrMerge(string[] addresses)
    {
        throw new NotImplementedException("net.cidr_merge");
    }

    private static string[]? LookupIPAddress(string name)
    {
        var result = new List<string>();

        try
        {
            var ipv4 = Dns.GetHostEntry(name, AddressFamily.InterNetwork);
            result.AddRange(ipv4.AddressList.Select(p => p.ToString()));
        }
        catch (SocketException)
        {
            return null;
        }

        try
        {
            var ipv4 = Dns.GetHostEntry(name, AddressFamily.InterNetworkV6);
            result.AddRange(ipv4.AddressList.Select(p => p.ToString()));
        }
        catch (SocketException)
        {
            if (result.Count == 0)
                return null;
        }

        return result.ToArray();
    }

    private static IPNetwork ParseNetwork(string cidrOrIp)
    {
        if (TryParseNetwork(cidrOrIp, out var result))
            return result;

        throw new FormatException($"Invalid IP/CIDR format '{cidrOrIp}'");
    }

    private static bool TryParseNetwork(string cidrOrIp, [MaybeNullWhen(false)] out IPNetwork result)
    {
        result = null;

        if (IPAddress.TryParse(cidrOrIp, out var ip))
        {
            result = new IPNetwork(ip, 32);
            return true;
        }

        return IPNetwork.TryParse(cidrOrIp, out result);
    }
}