namespace OpaDotNet.Extensions.AspNetCore.Tests;

public class OpaPolicyAuthorizeAttributeTests
{
    [Theory]
    [InlineData("x", "z", "Opa/x/z")]
    [InlineData("x", null, "Opa/x")]
    public void PolicyName(string module, string? rule, string expected)
    {
        var att = new OpaPolicyAuthorizeAttribute(module, rule);
        Assert.Equal(expected, att.Policy);
    }
}