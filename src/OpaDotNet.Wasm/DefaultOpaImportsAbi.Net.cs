using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;

using Microsoft.Extensions.Primitives;

namespace OpaDotNet.Wasm;

public partial class DefaultOpaImportsAbi
{
    private static IEnumerable<object[]>? CidrContainsMatches(JsonNode? cidrs, JsonNode? cidrOrIps)
    {
        if (cidrs == null || cidrOrIps == null)
            return Array.Empty<object[]>();

        string[] cidrVal;
        string[] cidrOrIpsVal;
        
        try
        {
            cidrVal = cidrs switch
            {
                JsonArray ja => ja.Deserialize<string[]>() ?? Array.Empty<string>(),
                JsonValue jv => new[] { jv.GetValue<string>() },
                _ => throw new NotSupportedException($"Format {cidrs} is not supported"),
            };
        }
        catch (JsonException ex)
        {
            throw new NotSupportedException($"Format {cidrs} is not supported", ex);
        }

        try
        {
            cidrOrIpsVal = cidrOrIps switch
            {
                JsonArray ja => ja.Deserialize<string[]>() ?? Array.Empty<string>(),
                JsonValue jv => new[] { jv.GetValue<string>() },
                _ => throw new NotSupportedException($"Format {cidrOrIps} is not supported"),
            };
        }
        catch (JsonException ex)
        {
            throw new NotSupportedException($"Format {cidrOrIps} is not supported", ex);
        }
        
        return CidrContainsMatches(cidrVal, cidrOrIpsVal);
    }

    private static IEnumerable<object[]>? CidrContainsMatches(string[] cidrs, string[] cidrOrIps)
    {
        List<IPNetwork> sourceCidrs;
        List<IPNetwork> targetCidrs;
        
        try
        {
            sourceCidrs = cidrs.Select(ParseNetwork).ToList();
            targetCidrs = cidrOrIps.Select(ParseNetwork).ToList();
        }
        catch (FormatException)
        {
            return null;
        }
        
        var results = new List<object[]>();
        
        for (var i = 0; i < sourceCidrs.Count; i++)
        {
            for (var j = 0; j < targetCidrs.Count; j++)
            {
                if (sourceCidrs[i].Contains(targetCidrs[j]))
                {
                    var tc = targetCidrs[i].Cidr == 32 
                        ? targetCidrs[i].FirstUsable.ToString()
                        : targetCidrs[i].ToString();
                        
                    object iVal = sourceCidrs.Count == 1 ? sourceCidrs[i].ToString() : i; 
                    object jVal = targetCidrs.Count == 1 ? tc : j; 
                    
                    results.Add(new[] { iVal, jVal });
                }
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