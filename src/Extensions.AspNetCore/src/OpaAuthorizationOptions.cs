using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore;

/// <summary>
/// Specifies options for Opa policy evaluator.
/// </summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public class OpaAuthorizationOptions
{
    /// <summary>
    /// Headers that can be used for policy evaluation. Supports regex.
    /// </summary>
    public HashSet<string> AllowedHeaders { get; set; } = [];

    /// <summary>
    /// If <c>true</c> will append user claims to the policy evaluation query.
    /// </summary>
    public bool IncludeClaimsInHttpRequest { get; set; }

    /// <summary>
    /// Authentication schemes OPA policies will be evaluated against.
    /// </summary>
    public HashSet<string> AuthenticationSchemes { get; set; } = [];

    /// <summary>
    /// Directory containing policy bundle source code.
    /// </summary>
    public string? PolicyBundlePath { get; set; }

    /// <summary>
    /// OPA policy compiler configuration.
    /// </summary>
    public RegoCompilerOptions? Compiler { get; set; }

    /// <summary>
    /// OPA policy engine configuration.
    /// </summary>
    public WasmPolicyEngineOptions EngineOptions { get; set; } = WasmPolicyEngineOptions.Default;

    /// <summary>
    /// Maximum number of <see cref="IOpaEvaluator"/> instances to keep in the pool.
    /// </summary>
    public int MaximumEvaluatorsRetained { get; set; } = Environment.ProcessorCount * 2;

    /// <summary>
    /// Maximum number of <see cref="IOpaEvaluator"/> instances that can operate concurrently.
    /// </summary>
    public int MaximumEvaluators { get; set; }

    /// <summary>
    /// How frequently recompilation is allowed to happen if policy sources have been changed.
    /// </summary>
    public TimeSpan MonitoringInterval { get; set; } = TimeSpan.Zero;
}