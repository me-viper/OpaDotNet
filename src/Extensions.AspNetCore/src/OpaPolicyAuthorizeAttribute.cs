namespace OpaDotNet.Extensions.AspNetCore;

[PublicAPI]
public class OpaPolicyAuthorizeAttribute : AuthorizeAttribute
{
    internal const string PolicyPrefix = "Opa";

    public OpaPolicyAuthorizeAttribute(string module, string? rule)
    {
        ArgumentException.ThrowIfNullOrEmpty(module);

        Policy = string.IsNullOrEmpty(rule) ? $"{PolicyPrefix}/{module}" : $"{PolicyPrefix}/{module}/{rule}";
    }
}