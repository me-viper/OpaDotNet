using OpaDotNet.Tests.Common;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

public class SdkCryptoTests(ITestOutputHelper output) : SdkTestBase(output)
{
    [Theory]
    [InlineData("""crypto.md5("message")""", "\"78e731027d8fd50ed642340b7c9a63b3\"")]
    [InlineData("""crypto.sha1("message")""", "\"6f9b9af3cd6e8b8a73c2cdced37fe9f59226e27d\"")]
    [InlineData("""crypto.sha256("message")""", "\"ab530a13e45914982b79f9b7e3fba994cfd1f3fb22f71cea1afbf02b460c6d1d\"")]
    [InlineData("""crypto.hmac.equal("4e4748e62b463521f6775fbf921234b5", "4e4748e62b463521f6775fbf921234b5")""", "true")]
    [InlineData("""crypto.hmac.equal("4e4748e62b463521f6775fbf921234bx", "4e4748e62b463521f6775fbf921234b5")""", "false")]
    [InlineData("""crypto.hmac.md5("message", "key")""", "\"4e4748e62b463521f6775fbf921234b5\"")]
    [InlineData("""crypto.hmac.sha1("message", "key")""", "\"2088df74d5f2146b48146caf4965377e9d0be3a4\"")]
    [InlineData("""crypto.hmac.sha256("message", "key")""", "\"6e9ef29b75fffc5b7abae527d58fdadb2fe42e7219011976917343065f58ed4a\"")]
    [InlineData("""crypto.hmac.sha512("message", "key")""", "\"e477384d7ca229dd1426e64b63ebf2d36ebd6d7e669a6735424e72ea6c01d3f8b56eb39c36d8232f5427999b8d1a3f9cd1128fc69f4d75b434216810fa367e98\"")]
    public async Task Crypto(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }
}