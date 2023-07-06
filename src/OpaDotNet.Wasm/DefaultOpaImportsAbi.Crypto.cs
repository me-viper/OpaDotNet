using System.Security.Cryptography;
using System.Text;

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
}