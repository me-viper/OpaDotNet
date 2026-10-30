using OpaDotNet.Wasm.GoCompat;

namespace OpaDotNet.Wasm.Tests;

public class GoUrlTests
{
    // Expected values are from Go's net/url.Parse: Path and cmp.Or(RawPath, Path), as OPA's uri.parse reports them.
    [Theory]
    [InlineData("https://example.com/a%21b", "/a!b", "/a!b")]
    [InlineData("https://example.com/a!b", "/a!b", "/a!b")]
    [InlineData("https://example.com/a%2Ab", "/a*b", "/a*b")]
    [InlineData("https://example.com/a%2ab", "/a*b", "/a%2ab")]
    [InlineData("https://example.com/a%2Fb", "/a/b", "/a%2Fb")]
    [InlineData("https://example.com/a%20b", "/a b", "/a b")]
    [InlineData("https://example.com/a+b", "/a+b", "/a+b")]
    [InlineData("https://example.com/caf%C3%A9", "/café", "/café")]
    [InlineData("https://example.com/café", "/café", "/café")]
    [InlineData("https://example.com/", "/", "/")]
    [InlineData("https://example.com/a/../b", "/a/../b", "/a/../b")]
    [InlineData("https://example.com/a%28b%29?q=%28#f", "/a(b)", "/a(b)")]
    [InlineData("http://[::1]:8080/path", "/path", "/path")]
    public void SetPath(string url, string expectedPath, string expectedRawPath)
    {
        var rawPath = GoUrl.GetRawPath(url);
        Assert.NotNull(rawPath);

        var (path, goRawPath) = GoUrl.SetPath(rawPath);

        Assert.Equal(expectedPath, path);
        Assert.Equal(expectedRawPath, goRawPath.Length > 0 ? goRawPath : path);
    }

    [Theory]
    [InlineData("https://example.com", "")]
    [InlineData("https://example.com?q=1#frag", "")]
    [InlineData("mailto:user@example.com", null)]
    public void GetRawPath(string url, string? expected)
    {
        Assert.Equal(expected, GoUrl.GetRawPath(url));
    }

    // Expected values are Go's url.Parse errors without the `parse "<url>": ` prefix.
    [Theory]
    [InlineData("http://exa%6dple.com/", "invalid URL escape \"%6d\"")]
    [InlineData("http://exa%zzple.com/", "invalid URL escape \"%zz\"")]
    [InlineData("http://exa%6/", "invalid URL escape \"%6\"")]
    [InlineData("http://example.com:٣٤/", "invalid port \":٣٤\" after host")]
    [InlineData("http://example.com:80a/", "invalid port \":80a\" after host")]
    [InlineData("http://[::1]x/", "invalid port \"x\" after host")]
    [InlineData("http://caf%C3%A9.com/", null)]
    [InlineData("http://a%25b.com/", null)]
    [InlineData("http://example.com:/", null)]
    [InlineData("http://user:p%41ss@example.com/", null)]
    [InlineData("http://[::1]:8080/", null)]
    [InlineData("http://example.com/a:b%6d", null)]
    [InlineData("https://example.com:443/path", null)]
    [InlineData("mailto:user@example.com", null)]
    [InlineData("http://a<b@example.com/", "net/url: invalid userinfo")]
    [InlineData("http://user name@example.com/", "net/url: invalid userinfo")]
    [InlineData("http://usér@example.com/", "net/url: invalid userinfo")]
    [InlineData("http://a\"b@example.com/", "net/url: invalid userinfo")]
    [InlineData("http://a<b@exa%6dple.com/", "invalid URL escape \"%6d\"")]
    [InlineData("http://us@er@example.com/", null)]
    [InlineData("http://u!$&'()*+,;=:~._-@example.com/", null)]
    [InlineData("http://@example.com/", null)]
    public void CheckAuthority(string url, string? expected)
    {
        Assert.Equal(expected, GoUrl.CheckAuthority(url));
    }

    [Theory]
    [InlineData("http://a<b@example.com/", "parse \"http://a<b@example.com/\": net/url: invalid userinfo")]
    [InlineData("http://a\"b@example.com/", "parse \"http://a\\\"b@example.com/\": net/url: invalid userinfo")]
    public void ParseError(string url, string expected)
    {
        Assert.Equal(expected, GoUrl.ParseError(url, GoUrl.CheckAuthority(url)!));
    }

    [Theory]
    [InlineData("/a%zz", "%zz")]
    [InlineData("/a%2", "%2")]
    public void UnescapePathInvalid(string path, string escape)
    {
        var ex = Assert.Throws<FormatException>(() => GoUrl.UnescapePath(path));
        Assert.Equal($"invalid URL escape \"{escape}\"", ex.Message);
    }
}