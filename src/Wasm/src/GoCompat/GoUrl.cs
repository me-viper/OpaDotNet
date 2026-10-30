using System.Globalization;
using System.Text;

namespace OpaDotNet.Wasm.GoCompat;

/// <summary>
/// Path handling of Go's net/url.
/// </summary>
internal static class GoUrl
{
    /// <summary>
    /// Extracts the path of a hierarchical (<c>scheme://authority/path</c>) URL exactly as written, without decoding
    /// or normalization.
    /// </summary>
    /// <returns>Raw path, or <c>null</c> if <paramref name="url"/> isn't hierarchical.</returns>
    public static string? GetRawPath(string url)
    {
        var rest = url;
        var end = rest.IndexOfAny(['?', '#']);

        if (end >= 0)
            rest = rest[..end];

        var authority = rest.IndexOf("://", StringComparison.Ordinal);

        if (authority < 0)
            return null;

        var path = rest.IndexOf('/', authority + 3);

        return path < 0 ? string.Empty : rest[path..];
    }

    /// <summary>
    /// Subset of Go's <c>parseAuthority</c> checks on a hierarchical URL, in Go's order: the port must be ASCII digits,
    /// a host outside brackets may only percent-encode non-ASCII bytes (or <c>%25</c>), and userinfo may only contain
    /// the characters Go allows.
    /// </summary>
    /// <returns>Go's error message, or <c>null</c> if the checks pass or <paramref name="url"/> isn't hierarchical.</returns>
    public static string? CheckAuthority(string url)
    {
        var start = url.IndexOf("://", StringComparison.Ordinal);

        if (start < 0)
            return null;

        start += 3;
        var end = url.IndexOfAny(['/', '?', '#'], start);
        var authority = end < 0 ? url[start..] : url[start..end];
        var at = authority.LastIndexOf('@');

        if (CheckHost(authority[(at + 1)..]) is { } error)
            return error;

        if (at >= 0 && !IsValidUserinfo(authority[..at]))
            return "net/url: invalid userinfo";

        return null;
    }

    /// <summary>
    /// Formats an error the way Go's <c>url.Parse</c> does: <c>parse "url": error</c>.
    /// </summary>
    public static string ParseError(string url, string error)
    {
        var quoted = url.Replace(@"\", @"\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        return $"parse \"{quoted}\": {error}";
    }

    // Go's validUserinfo.
    private static bool IsValidUserinfo(string userinfo)
    {
        foreach (var c in userinfo)
        {
            if (char.IsAsciiLetterOrDigit(c))
                continue;

            if (c is not ('-' or '.' or '_' or ':' or '~' or '!' or '$' or '&' or '\'' or '(' or ')' or '*' or '+' or ','
                or ';' or '=' or '%' or '@'))
            {
                return false;
            }
        }

        return true;
    }

    private static string? CheckHost(string host)
    {
        if (host.StartsWith('['))
        {
            var close = host.LastIndexOf(']');

            if (close < 0)
                return null;

            var bracketPort = host[(close + 1)..];
            return IsValidOptionalPort(bracketPort) ? null : $"invalid port \"{bracketPort}\" after host";
        }

        var colon = host.LastIndexOf(':');

        if (colon >= 0)
        {
            var port = host[colon..];

            if (!IsValidOptionalPort(port))
                return $"invalid port \"{port}\" after host";
        }

        for (var i = 0; i < host.Length; i++)
        {
            if (host[i] != '%')
                continue;

            var escape = host.Substring(i, Math.Min(3, host.Length - i));

            if (escape.Length < 3 || !char.IsAsciiHexDigit(escape[1]) || !char.IsAsciiHexDigit(escape[2]))
                return $"invalid URL escape \"{escape}\"";

            // unhex(s[i+1]) < 8: the escaped byte is ASCII.
            if (escape[1] < '8' && escape != "%25")
                return $"invalid URL escape \"{escape}\"";

            i += 2;
        }

        return null;
    }

    // Go's validOptionalPort.
    private static bool IsValidOptionalPort(string port) =>
        port.Length == 0 || (port[0] == ':' && port.AsSpan(1).IndexOfAnyExceptInRange('0', '9') < 0);

    /// <summary>
    /// Go's <c>(*URL).setPath</c>: decodes <paramref name="rawPath"/> and keeps the raw form only when it differs from
    /// the default encoding of the decoded path.
    /// </summary>
    /// <returns>Decoded path and raw path (empty if it's the default encoding).</returns>
    public static (string Path, string RawPath) SetPath(string rawPath)
    {
        var path = UnescapePath(rawPath);
        return (path, EscapePath(path) == rawPath ? string.Empty : rawPath);
    }

    /// <summary>
    /// Go's <c>unescape(s, encodePath)</c>: decodes <c>%XX</c> sequences; unlike form decoding, <c>+</c> is kept.
    /// </summary>
    /// <exception cref="FormatException">Invalid escape sequence.</exception>
    public static string UnescapePath(string s)
    {
        if (!s.Contains('%'))
            return s;

        var bytes = Encoding.UTF8.GetBytes(s);
        var result = new List<byte>(bytes.Length);

        for (var i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] != '%')
            {
                result.Add(bytes[i]);
                continue;
            }

            if (i + 2 >= bytes.Length || !IsHex(bytes[i + 1]) || !IsHex(bytes[i + 2]))
            {
                var escape = Encoding.UTF8.GetString(bytes, i, Math.Min(3, bytes.Length - i));
                throw new FormatException($"invalid URL escape \"{escape}\"");
            }

            var hex = Encoding.ASCII.GetString(bytes, i + 1, 2);
            result.Add(byte.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            i += 2;
        }

        return Encoding.UTF8.GetString(result.ToArray());
    }

    /// <summary>
    /// Go's <c>escape(s, encodePath)</c>: escapes every byte except unreserved characters and the sub-delimiters Go
    /// leaves as is in paths, using uppercase hex.
    /// </summary>
    public static string EscapePath(string s)
    {
        var sb = new StringBuilder(s.Length);

        foreach (var b in Encoding.UTF8.GetBytes(s))
        {
            if (ShouldEscapeInPath(b))
                sb.Append('%').Append(b.ToString("X2", CultureInfo.InvariantCulture));
            else
                sb.Append((char)b);
        }

        return sb.ToString();
    }

    private static bool ShouldEscapeInPath(byte b)
    {
        var c = (char)b;

        if (char.IsAsciiLetterOrDigit(c))
            return false;

        return c is not ('-' or '_' or '.' or '~' or '$' or '&' or '+' or ',' or '/' or ':' or ';' or '=' or '@');
    }

    private static bool IsHex(byte b) => char.IsAsciiHexDigit((char)b);
}