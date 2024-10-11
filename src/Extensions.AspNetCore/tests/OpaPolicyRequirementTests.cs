namespace OpaDotNet.Extensions.AspNetCore.Tests;

public class OpaPolicyRequirementTests
{
    [Theory]
    [InlineData("Opa/module/rule", true, "module/rule")]
    [InlineData("Opa/module", true, "module")]
    [InlineData("Opa", false, null)]
    [InlineData("Opa/", false, null)]
    [InlineData("zzz", false, null)]
    [InlineData("Opa/module/rule/xxxx", true, "module/rule/xxxx")]
    [InlineData("Opa/module///", true, "module")]
    public void Parse(string policy, bool expectedResult, string? entrypoint)
    {
        var result = OpaPolicyRequirement.TryParse(policy, out var opr);

        Assert.Equal(expectedResult, result);
        Assert.Equal(entrypoint, opr?.Entrypoint);
    }
}