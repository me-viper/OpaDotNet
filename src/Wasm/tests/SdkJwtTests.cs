using OpaDotNet.Wasm.Tests.Common;

namespace OpaDotNet.Wasm.Tests;

public class SdkJwtTests(ITestOutputHelper output) : SdkTestBase(output)
{
    [Theory]
    [InlineData(
        """io.jwt.decode("eyJ0eXAiOiAiSldUIiwgImFsZyI6ICJFUzI1NiJ9.eyJuYmYiOiAxNDQ0NDc4NDAwLCAiaXNzIjogInh4eCJ9.lArczfN-pIL8oUU-7PU83u-zfXougXBZj6drFeKFsPEoVhy9WAyiZlRshYqjTSXdaw8yw2L-ovt4zTUZb2PWMg")""",
        """[{"typ": "JWT", "alg": "ES256"}, {"nbf": 1444478400, "iss": "xxx"}, "940adccdf37ea482fca1453eecf53cdeefb37d7a2e8170598fa76b15e285b0f128561cbd580ca266546c858aa34d25dd6b0f32c362fea2fb78cd35196f63d632"]"""
        )]
    public async Task Jwt(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    private const string JwtVerifyIssToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJ4eHgifQ.Mt8_pnXt43Dh1SnoOQLSzXHnb3BPoTa4ATIDXJig0g8";
    private const string JwtVerifyIssSecret = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Theory]
    [InlineData(
        $$"""io.jwt.verify_hs256("{{JwtVerifyIssToken}}", "{{JwtVerifyIssSecret}}")""",
        """true"""
        )]
    [InlineData(
        $$"""io.jwt.decode_verify("{{JwtVerifyIssToken}}", {"iss": "xxx", "secret": "{{JwtVerifyIssSecret}}"})""",
        """[true,{"alg":"HS256","typ":"JWT"},{"iss":"xxx"}]"""
        )]
    [InlineData(
        $$"""io.jwt.decode_verify("{{JwtVerifyIssToken}}", {"iss": "yyy", "secret": "{{JwtVerifyIssSecret}}"})""",
        """[false,{},{}]"""
        )]
    public async Task JwtVerifyIss(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    private const string JwtVerifyAudToken = "eyJhbGciOiJIUzM4NCIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJ4eHgiLCJhdWQiOiJhYWEifQ.xZbdtbn-es4obumv6H1DVrdiZL8GTOya-ujROx63yots_FjvG_5fop00c0ah6MNB";
    private const string JwtVerifyAudSecret = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Theory]
    [InlineData(
        $$"""io.jwt.verify_hs384("{{JwtVerifyAudToken}}", "{{JwtVerifyAudSecret}}")""",
        """true"""
        )]
    [InlineData(
        $$"""io.jwt.decode_verify("{{JwtVerifyAudToken}}", {"aud": "aaa", "secret": "{{JwtVerifyAudSecret}}"})""",
        """[true,{"alg":"HS384","typ":"JWT"},{"iss": "xxx", "aud":"aaa"}]"""
        )]
    [InlineData(
        $$"""io.jwt.decode_verify("{{JwtVerifyAudToken}}", {"aud": "bbb", "secret": "{{JwtVerifyAudSecret}}"})""",
        """[false,{},{}]"""
        )]
    [InlineData(
        $$"""io.jwt.decode_verify("{{JwtVerifyAudToken}}", {"secret": "{{JwtVerifyAudSecret}}"})""",
        """[false,{},{}]"""
        )]
    public async Task JwtVerifyAud(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    private const string JwtVerifyTimeToken = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJ4eHgiLCJleHAiOjE2ODkxNDE4NDcsIm5iZiI6MTY4ODk2OTA0N30.iaQZYESw0-enygzry1EYKT7_xiNGhqExlWG62fUmt3mhb31LXNLKEZL_ki-nhDuQf6hkydakXpIc6m6lVIp-iQ";
    private const string JwtVerifyTimeSecret = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Theory]
    [InlineData(
        $$"""io.jwt.verify_hs512("{{JwtVerifyTimeToken}}", "{{JwtVerifyTimeSecret}}")""",
        """true"""
        )]
    [InlineData(
        $$"""io.jwt.decode_verify("{{JwtVerifyTimeToken}}", {"time": 1689055447000000000, "secret": "{{JwtVerifyTimeSecret}}"})""",
        """[true,{"alg":"HS512","typ":"JWT"},{"iss": "xxx", "exp": 1689141847, "nbf": 1688969047}]"""
        )]
    [InlineData(
        $$"""io.jwt.decode_verify("{{JwtVerifyTimeToken}}", {"time": 1688969047000000001, "secret": "{{JwtVerifyTimeSecret}}"})""",
        """[true,{"alg":"HS512","typ":"JWT"},{"iss": "xxx", "exp": 1689141847, "nbf": 1688969047}]"""
        )]
    [InlineData(
        $$"""io.jwt.decode_verify("{{JwtVerifyTimeToken}}", {"time": 1689141846999999999, "secret": "{{JwtVerifyTimeSecret}}"})""",
        """[true,{"alg":"HS512","typ":"JWT"},{"iss": "xxx", "exp": 1689141847, "nbf": 1688969047}]"""
        )]
    public async Task JwtVerifyTime(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""io.jwt.decode_verify(t, {"time": 1300819379000000000, "cert": json.marshal(s)})""")]
    [InlineData("""io.jwt.decode_verify(t, {"cert": json.marshal(s)})""")]
    public async Task JwtNoExp(string check)
    {
        var src = $$"""
            package sdk

            s := {
                "kty": "oct",
                "k": "AyM1SysPpbyDfgZld3umj1qzKObwVMkoqQ-EstJQLr_T-1qS0gZH75aKtMN3Yj0iPS4hcgUuTwjAzZr1Z9CAow",
            }

            t := io.jwt.encode_sign({ "typ": "JWT", "alg": "HS256" }, { "iss": "joe" }, s)

            x := {{check}}
            r := x[0]
            """;

        using var eval = await Build(src, "sdk");

        var result = eval.EvaluateValue(new { r = false }, "sdk");

        Assert.True(result.r);
    }

    [Theory]
    [InlineData("""io.jwt.decode_verify(t, {"time": 1516239022000000000, "cert": json.marshal(s)})""", false)]
    [InlineData("""io.jwt.decode_verify(t, {"cert": json.marshal(s)})""", false)]
    public async Task JwtFailExp(string check, bool valid)
    {
        var src = $$"""
            package sdk

            s := {
                "kty": "oct",
                "k": "AyM1SysPpbyDfgZld3umj1qzKObwVMkoqQ-EstJQLr_T-1qS0gZH75aKtMN3Yj0iPS4hcgUuTwjAzZr1Z9CAow",
            }

            t := io.jwt.encode_sign({ "typ": "JWT", "alg": "HS256" }, { "iss": "joe", "exp": 1300819379 }, s)

            x := {{check}}

            r := x[0]
            """;

        using var eval = await Build(src, "sdk");

        var result = eval.EvaluateValue(new { r = false }, "sdk");

        Assert.Equal(valid, result.r);
    }

    [Fact]
    public async Task JwtJwkCerts()
    {
        var src = """
            package sdk

            es256_token := "eyJ0eXAiOiAiSldUIiwgImFsZyI6ICJFUzI1NiJ9.eyJuYmYiOiAxNDQ0NDc4NDAwLCAiaXNzIjogInh4eCJ9.lArczfN-pIL8oUU-7PU83u-zfXougXBZj6drFeKFsPEoVhy9WAyiZlRshYqjTSXdaw8yw2L-ovt4zTUZb2PWMg"

            jwks := `{
                "keys": [{
                    "kty":"EC",
                    "crv":"P-256",
                    "x":"z8J91ghFy5o6f2xZ4g8LsLH7u2wEpT2ntj8loahnlsE",
                    "y":"7bdeXLH61KrGWRdh7ilnbcGQACxykaPKfmBccTHIOUo"
                }]
            }`

            r := io.jwt.verify_es256(es256_token, jwks)
            """;
        using var eval = await Build(src, "sdk");

        var result = eval.EvaluateValue(
            new { r = false, },
            "sdk"
            );

        Assert.True(result.r);
    }

    [Fact]
    public async Task JwtPemCerts()
    {
        var src = """
            package sdk

            es256_token := "eyJ0eXAiOiAiSldUIiwgImFsZyI6ICJFUzI1NiJ9.eyJuYmYiOiAxNDQ0NDc4NDAwLCAiaXNzIjogInh4eCJ9.lArczfN-pIL8oUU-7PU83u-zfXougXBZj6drFeKFsPEoVhy9WAyiZlRshYqjTSXdaw8yw2L-ovt4zTUZb2PWMg"

            cert := `-----BEGIN CERTIFICATE-----
            MIIBcDCCARagAwIBAgIJAMZmuGSIfvgzMAoGCCqGSM49BAMCMBMxETAPBgNVBAMM
            CHdoYXRldmVyMB4XDTE4MDgxMDE0Mjg1NFoXDTE4MDkwOTE0Mjg1NFowEzERMA8G
            A1UEAwwId2hhdGV2ZXIwWTATBgcqhkjOPQIBBggqhkjOPQMBBwNCAATPwn3WCEXL
            mjp/bFniDwuwsfu7bASlPae2PyWhqGeWwe23Xlyx+tSqxlkXYe4pZ23BkAAscpGj
            yn5gXHExyDlKo1MwUTAdBgNVHQ4EFgQUElRjSoVgKjUqY5AXz2o74cLzzS8wHwYD
            VR0jBBgwFoAUElRjSoVgKjUqY5AXz2o74cLzzS8wDwYDVR0TAQH/BAUwAwEB/zAK
            BggqhkjOPQQDAgNIADBFAiEA4yQ/88ZrUX68c6kOe9G11u8NUaUzd8pLOtkKhniN
            OHoCIHmNX37JOqTcTzGn2u9+c8NlnvZ0uDvsd1BmKPaUmjmm
            -----END CERTIFICATE-----`

            r := io.jwt.verify_es256(es256_token, cert)
            """;
        using var eval = await Build(src, "sdk");

        var result = eval.EvaluateValue(
            new { r = false, },
            "sdk"
            );

        Assert.True(result.r);
    }

    [Theory]
    [InlineData(
        """
        io.jwt.encode_sign({
            "typ": "JWT",
            "alg": "HS256"
        }, {
            "iss": "joe",
            "exp": 1300819380,
            "aud": ["bob", "saul"],
            "http://example.com/is_root": true,
            "privateParams": {
                "private_one": "one",
                "private_two": "two"
            }
        }, {
            "kty": "oct",
            "k": "AyM1SysPpbyDfgZld3umj1qzKObwVMkoqQ-EstJQLr_T-1qS0gZH75aKtMN3Yj0iPS4hcgUuTwjAzZr1Z9CAow"
        })
        """,
        "\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vZXhhbXBsZS5jb20vaXNfcm9vdCI6dHJ1ZSwiaXNzIjoiam9lIiwicHJpdmF0ZVBhcmFtcyI6eyJwcml2YXRlX3R3byI6InR3byIsInByaXZhdGVfb25lIjoib25lIn0sImF1ZCI6WyJib2IiLCJzYXVsIl0sImV4cCI6MTMwMDgxOTM4MH0.hBeTHKH5VfWc1y502RQytpQQg5UzvLyxWqQVa2mmRAU\""
        )]
    [InlineData(
        """
        io.jwt.encode_sign({
            "typ": "JWT",
            "alg": "HS256"},
            {}, {
            "kty": "oct",
            "k": "AyM1SysPpbyDfgZld3umj1qzKObwVMkoqQ-EstJQLr_T-1qS0gZH75aKtMN3Yj0iPS4hcgUuTwjAzZr1Z9CAow"
        })
        """,
        "\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.e30.6cvao8lnOu6FAdK68jQFcDMXOmaWNwWiYhCgijd-AD8\""
        )]
    [InlineData(
        """
        io.jwt.encode_sign({
            "alg": "RS256"
        }, {
            "iss": "joe",
            "exp": 1300819380,
            "aud": ["bob", "saul"],
            "http://example.com/is_root": true,
            "privateParams": {
                "private_one": "one",
                "private_two": "two"
            }
        },
        {
            "kty": "RSA",
            "n": "ofgWCuLjybRlzo0tZWJjNiuSfb4p4fAkd_wWJcyQoTbji9k0l8W26mPddxHmfHQp-Vaw-4qPCJrcS2mJPMEzP1Pt0Bm4d4QlL-yRT-SFd2lZS-pCgNMsD1W_YpRPEwOWvG6b32690r2jZ47soMZo9wGzjb_7OMg0LOL-bSf63kpaSHSXndS5z5rexMdbBYUsLA9e-KXBdQOS-UTo7WTBEMa2R2CapHg665xsmtdVMTBQY4uDZlxvb3qCo5ZwKh9kG4LT6_I5IhlJH7aGhyxXFvUK-DWNmoudF8NAco9_h9iaGNj8q2ethFkMLs91kzk2PAcDTW9gb54h4FRWyuXpoQ",
            "e": "AQAB",
            "d": "Eq5xpGnNCivDflJsRQBXHx1hdR1k6Ulwe2JZD50LpXyWPEAeP88vLNO97IjlA7_GQ5sLKMgvfTeXZx9SE-7YwVol2NXOoAJe46sui395IW_GO-pWJ1O0BkTGoVEn2bKVRUCgu-GjBVaYLU6f3l9kJfFNS3E0QbVdxzubSu3Mkqzjkn439X0M_V51gfpRLI9JYanrC4D4qAdGcopV_0ZHHzQlBjudU2QvXt4ehNYTCBr6XCLQUShb1juUO1ZdiYoFaFQT5Tw8bGUl_x_jTj3ccPDVZFD9pIuhLhBOneufuBiB4cS98l2SR_RQyGWSeWjnczT0QU91p1DhOVRuOopznQ",
            "p": "4BzEEOtIpmVdVEZNCqS7baC4crd0pqnRH_5IB3jw3bcxGn6QLvnEtfdUdiYrqBdss1l58BQ3KhooKeQTa9AB0Hw_Py5PJdTJNPY8cQn7ouZ2KKDcmnPGBY5t7yLc1QlQ5xHdwW1VhvKn-nXqhJTBgIPgtldC-KDV5z-y2XDwGUc",
            "q": "uQPEfgmVtjL0Uyyx88GZFF1fOunH3-7cepKmtH4pxhtCoHqpWmT8YAmZxaewHgHAjLYsp1ZSe7zFYHj7C6ul7TjeLQeZD_YwD66t62wDmpe_HlB-TnBA-njbglfIsRLtXlnDzQkv5dTltRJ11BKBBypeeF6689rjcJIDEz9RWdc",
            "dp": "BwKfV3Akq5_MFZDFZCnW-wzl-CCo83WoZvnLQwCTeDv8uzluRSnm71I3QCLdhrqE2e9YkxvuxdBfpT_PI7Yz-FOKnu1R6HsJeDCjn12Sk3vmAktV2zb34MCdy7cpdTh_YVr7tss2u6vneTwrA86rZtu5Mbr1C1XsmvkxHQAdYo0",
            "dq": "h_96-mK1R_7glhsum81dZxjTnYynPbZpHziZjeeHcXYsXaaMwkOlODsWa7I9xXDoRwbKgB719rrmI2oKr6N3Do9U0ajaHF-NKJnwgjMd2w9cjz3_-kyNlxAr2v4IKhGNpmM5iIgOS1VZnOZ68m6_pbLBSp3nssTdlqvd0tIiTHU",
            "qi": "IYd7DHOhrWvxkwPQsRM2tOgrjbcrfvtQJipd-DlcxyVuuM9sQLdgjVk2oy26F0EmpScGLq2MowX7fhd_QJQ3ydy5cY7YIBi87w93IKLEdfnbJtoOPLUW0ITrJReOgo1cq9SbsxYawBgfp_gh6A5603k2-ZQwVK0JKSHuLFkuQ3U"
        })
        """,
        "\"eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vZXhhbXBsZS5jb20vaXNfcm9vdCI6dHJ1ZSwiaXNzIjoiam9lIiwicHJpdmF0ZVBhcmFtcyI6eyJwcml2YXRlX3R3byI6InR3byIsInByaXZhdGVfb25lIjoib25lIn0sImF1ZCI6WyJib2IiLCJzYXVsIl0sImV4cCI6MTMwMDgxOTM4MH0.UkC7cqfGzNIOLkUguoqZQOS3LBakt04RJiAylHz6iv5_MtVnqSuhF0agWEPQTjX-5rf3T_Gz-7BaNrw18Fv7wb07FO97pbrdLfcEUTC65qhNjk9lTpyt-m_ICdEF03XDcORC1nnuzKdKk25FUvUtotD5cnZ7o7xgv-HwOU7srEhoudlezB6GulcwMiRyIh17LyjNdCCMsOhrHMFPEMbCWO4IL1OL8Ohns2kxcPDoGnYiiWbAkQCWAXAp8DWCcUaqFjKOY3-GFHFBEBfyRwO_-c8hxx4i1glaPXR2i8OXRuhs7k25s5V0qZNcFjCLeVpmDXrJe3NcetntAoCmPoAR7A\""
        )]
    [InlineData(
        """
        io.jwt.encode_sign_raw(
            `{"typ":"JWT","alg":"HS256"}`,
             `{"iss":"joe","exp":1300819380,"http://example.com/is_root":true}`,
            `{"kty":"oct","k":"AyM1SysPpbyDfgZld3umj1qzKObwVMkoqQ-EstJQLr_T-1qS0gZH75aKtMN3Yj0iPS4hcgUuTwjAzZr1Z9CAow"}`
        )
        """,
        "\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJqb2UiLCJleHAiOjEzMDA4MTkzODAsImh0dHA6Ly9leGFtcGxlLmNvbS9pc19yb290Ijp0cnVlfQ.d6nMDXnJZfNNj-1o1e75s6d0six0lkLp5hSrGaz4o9A\""
        )]
    public async Task JwtEncodeSign(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData(
        """
        io.jwt.decode("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCIsImN0eSI6IkpXVCJ9.ImV5SmhiR2NpT2lKSVV6STFOaUlzSW5SNWNDSTZJa3BYVkNJc0ltTjBlU0k2SWtwWFZDSjkuSW1WNVNtaGlSMk5wVDJsS1NWVjZTVEZPYVVselNXNVNOV05EU1RaSmEzQllWa05LT1M1bGVVcDZaRmRKYVU5cFNYZEphWGRwWVZoT2VrbHFiMmxpTTBKb1NXNHdMbGh0Vm05TWIwaEpNM0I0VFhSTlQxOVhVazlPVFZOS2VrZFZSRkE1Y0VScWVUaEtjREJmZEdSU1dGa2kuOFcwcXg0bUx4c2xtWmw3d0VNVVdCeEg3dFNUM1hzRXVXWHhlc1hxRm5SSSI.U8rwnGAJ-bJoGrAYKEzNtbJQWd3x1eW0Y25nLKHDCgo")
        """,
        """
        [{"alg":"HS256","typ":"JWT"},{"iss":"opa","sub":"0"},"5e65682e81c8de9c4cb4c3bf59138d3122731940cff690e3cbc269d3fb5d4576"]
        """
        )]
    [InlineData(
        """
        io.jwt.decode_verify("eyJhbGciOiAiUlMyNTYiLCAidHlwIjogIkpXVCIsICJjdHkiOiAiSldUIn0.ZXlKaGJHY2lPaUFpVWxNeU5UWWlMQ0FpZEhsd0lqb2dJa3BYVkNJc0lDSmpkSGtpT2lBaVNsZFVJbjAuWlhsS2FHSkhZMmxQYVVGcFZXeE5lVTVVV1dsTVEwRnBaRWhzZDBscWIyZEphM0JZVmtOS09TNWxlVXB3WXpOTmFVOXBRV2xsU0dnMFNXNHdMbkpTVW5KbFVVOURZVzlaTFcxTmF6Y3lhazVHWlZrMVlWbEZVV2hKWjBsRmRGWmtVVGxZYmxsdFVVd3lUSGRmYURkTmJrazBVMFZQTVZCd2EwSklWRXB5Wm5samJFcGxUSHBmYWxKMlVHZEpNbGN4YURGQ05HTmFWRGhEWjIxcFZYZHhRWEk1YzBwdVpIbFZRMUZ0U1dScmJtNTNXa0k1Y1hBdFgzQlRkR1JIV0VvNVduQXplRW80TlhvdFZFSnBXbE4wUVVOVVpGZGxVa2xHU1VVM1ZreFBhMjB0Um14WmR6aDVPVGRuYVVONFRteFVkV2wzYW14bFRqTXdaRGhuV0hVeE5rWkdRekpUU2xodFJqWktiWFl0TmpKSGJFUmhMVzFDV0ZaMGJHSlZTVFZsV1ZVd2FUZHVlVE55UWpCWVVWUXhSa3Q0WlVaM09GODVOMDlGZFY5alkzVkxjbDgyWkhsSFpWRkhkblE1WTNKSmVFRkJNV0ZaYkRkbWJWQnJOa1ZoY2psbFRUTkthR1ZZTWkwMFdreDBkMUZPWTFSRFQwMVlWMGRJY2sxRGFHNU1XVmM0V0VGclRISkVibDl5Um14VWFWTXRady5YaWNjMnNXQ1pfTml0aHVjc3c5WEQ3WU9LcmlyVWRFbkgzTXlpUE0tQ2szdkVVMlJzVEJzZlUySlBoZmpwM3BoYzBWT2dzQVhDendVNVB3eU55VW8xNDkwcThZU3ltLWxpTXlPMkxrLWhqSDVmQXhvaXpnOXlENElJX2xLNld6X1RucGMwYkJHRExkYnVVaHZndk83eXFvLWxlQlFsc2ZSWE92dzRWU1BTRXk4UVB0YlVSdGJuTHBXWTJqR0JLejd2R0lfbzRxREozUGljRzBreUVpV1pOaDN3amVlQ1lSQ1d2WE44cWg3VWs1RUEtOEo1dlg2NTFHcVYtN2dtYVgxbi04RFhhbWhhQ1FjRS1wMWNqU2owNC1YLV9iSmxRdG1iLVRUM2JTeVVQeGdIVm5jdnhOVWJ5OGprVVR6Zmk1TU1ibUl6V1dreEk1WXRKVGR0bUNrUFE.ODBVH_gooCLJxtPVr1MjJC1syG4MnVUFP9LkI9pSaj0QABV4vpfqrBshHn8zOPgUTDeHwbc01Qy96cQlTMQQb94YANmZyL1nzwmdR4piiGXMGSlcCNfDg1o8DK4msMSR-X-j2IkxBDB8rfeFSfLRMgDCjAF0JolW7qWmMD9tBmFNYAjly4vMwToOXosDmFLl5eqyohXDf-3Ohljm5kIjtyMWkt5S9EVuwlIXh2owK5l59c4-TH29gkuaZ3uU4LFPjD7XKUrlOQnEMuu2QD8LAqTyxbnY4JyzUWEvyTM1dVmGnFpLKCg9QBly__y1u2ffhvDsHyuCmEKAbhPE98YvFA", {"cert": "-----BEGIN CERTIFICATE-----\nMIIC/DCCAeSgAwIBAgIJAJRvYDU3ei3EMA0GCSqGSIb3DQEBCwUAMBMxETAPBgNV\nBAMMCHdoYXRldmVyMB4XDTE4MDgxMDEwMzgxNloXDTE4MDkwOTEwMzgxNlowEzER\nMA8GA1UEAwwId2hhdGV2ZXIwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIB\nAQC4kCmzLMW/5jzkzkmN7Me8wPD+ymBUIjsGqliGfMrfFfDV2eTPVtZcYD3IXoB4\nAOUT7XJzWjOsBRFOcVKKEiCPjXiLcwLb/QWQ1x0Budft32r3+N0KQd1rgcRHTPNc\nJoeWCfOgDPp51RTzTT6HQuV4ud+CDhRJP7QMVMIgal9Nuzs49LLZaBPW8/rFsHjk\nJQ4kDujSrpcT6F2FZY3SmWsOJgP7RjVKk5BheYeFKav5ZV4p6iHn/TN4RVpvpNBh\n5z/XoHITJ6lpkHSDpbIaQUTpobU2um8N3biz+HsEAmD9Laa27WUpYSpiM6DDMSXl\ndBDJdumerVRJvXYCtfXqtl17AgMBAAGjUzBRMB0GA1UdDgQWBBRz74MkVzT2K52/\nFJC4mTa9coM/DTAfBgNVHSMEGDAWgBRz74MkVzT2K52/FJC4mTa9coM/DTAPBgNV\nHRMBAf8EBTADAQH/MA0GCSqGSIb3DQEBCwUAA4IBAQAD1ZE4IaIAetqGG+vt9oz1\nIx0j4EPok0ONyhhmiSsF6rSv8zlNWweVf5y6Z+AoTNY1Fym0T7dbpbqIox0EdKV3\nFLzniWOjznupbnqfXwHX/g1UAZSyt3akSatVhvNpGlnd7efTIAiNinX/TkzIjhZ7\nihMIZCGykT1P0ys1OaeEf57wAzviatD4pEMTIW0OOqY8bdRGhuJR1kKUZ/2Nm8Ln\ny7E0y8uODVbH9cAwGyzWB/QFc+bffNgi9uJaPQQc5Zxwpu9utlqyzFvXgV7MBYUK\nEYSLyxp4g4e5aujtLugaC8H6n9vP1mEBr/+T8HGynBZHNTKlDhhL9qDbpkkNB6/w\n-----END CERTIFICATE-----"})
        """,
        """
        [true,{"alg":"RS256","typ":"JWT"},{"iss":"xxx"}]
        """
        )]
    public async Task JwtNested(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }
}