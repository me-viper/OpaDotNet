namespace OpaDotNet.Extensions.AspNetCore;

/// <summary>
/// Represents OPA policy requirement.
/// </summary>
public record OpaPolicyRequirement : IAuthorizationRequirement
{
    [PublicAPI]
    public OpaPolicyRequirement(string entrypoint)
    {
        ArgumentException.ThrowIfNullOrEmpty(entrypoint);

        Entrypoint = entrypoint;
    }

    /// <summary>
    /// OPA policy entrypoint.
    /// </summary>
    public string Entrypoint { get; }

    /// <summary>
    /// Tries to build <see cref="OpaPolicyRequirement"/> from policy name.
    /// </summary>
    /// <param name="policyName">OPA policy name in format Opa/[module]/?[entrypoint]</param>
    /// <param name="result">Parsed OPA policy requirement</param>
    /// <returns><c>true</c> if <see cref="policyName"/> represents OPA policy, otherwise <c>false</c></returns>
    internal static bool TryParse(string policyName, [MaybeNullWhen(false)] out OpaPolicyRequirement result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(policyName))
            return false;

        policyName = policyName.TrimEnd('/');

        var prefixIndex = policyName.IndexOf(OpaPolicyAuthorizeAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase);

        if (prefixIndex < 0)
            return false;

        prefixIndex += OpaPolicyAuthorizeAttribute.PolicyPrefix.Length + 1;

        if (prefixIndex > policyName.Length)
            return false;

        var ep = policyName[prefixIndex..];

        if (string.IsNullOrWhiteSpace(ep))
            return false;

        result = new OpaPolicyRequirement(ep);

        return true;
    }

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"OPA: {Entrypoint}";
    }
}