# Integrating with ASP.NET Core

`OpaDotNet.Extensions.AspNetCore` plugs OPA policy evaluation into ASP.NET Core's authorization system: instead of
writing an `IAuthorizationHandler` by hand, you name a Rego rule as the policy and the package evaluates it against
the current request (or any object you supply) using a pooled `IOpaEvaluator`.

## How it works

- `AddOpaAuthorization` registers an `IAuthorizationPolicyProvider` (`OpaPolicyProvider`) that recognizes any
  authorization policy name of the form `Opa/{module}/{rule}` and turns it into an `OpaPolicyRequirement` instead of
  requiring you to pre-register every policy with `AddAuthorization`.
- An `IOpaPolicySource` (file system, compiled bundle, or configuration — see [Policy sources](#policy-sources))
  compiles or loads a policy bundle on startup and exposes it as an `IOpaEvaluator` factory.
- `IOpaPolicyService` (`PooledOpaPolicyService`) pools evaluators created from that factory and evaluates the named
  entrypoint (`{module}/{rule}`) against an input document.
- `OpaPolicyHandler` (or `OpaPolicyHandler<TResource>`) is the `IAuthorizationHandler` that builds the input document
  and calls `IOpaPolicyService` when an `Opa/...` policy requirement is evaluated.

## Quick start

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpaAuthorization(
    cfg =>
    {
        // Compile .rego sources by shelling out to the `opa` CLI.
        cfg.AddCompiler<RegoCliCompiler>();

        // Compile a directory of .rego files on startup (and on change, see MonitoringInterval below).
        cfg.AddFileSystemPolicySource();

        cfg.AddConfiguration(
            p =>
            {
                p.PolicyBundlePath = "./Policy";
                p.AllowedHeaders.Add(".*");
            }
            );
    }
    );

builder.Services.AddAuthentication(/* ... */);
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Evaluates the `allow` rule in the `example` package (Rego: package example).
app.MapGet("/allow", [OpaPolicyAuthorize("example", "allow")]() => "Hi!");

app.Run();
```

> [!IMPORTANT]
> `AddOpaAuthorization` only wires up services — you still need `AddCompiler<T>()` and one policy source
> (`AddFileSystemPolicySource()`, `AddConfigurationPolicySource(...)`, or `AddPolicySource<CompiledBundlePolicySource>()`)
> or the evaluator has nothing to load.

## Requesting a decision

### Attribute-based authorization

`[OpaPolicyAuthorize(module, rule)]` builds the policy name `Opa/{module}/{rule}` for you:

```csharp
app.MapGet("/allow", [OpaPolicyAuthorize("example", "allow")]() => "Hi!");
```

This is equivalent to the plain ASP.NET Core `[Authorize]` attribute, since `OpaPolicyProvider` recognizes the
`Opa/...` name directly:

```csharp
app.MapGet("/allow2", [Authorize("Opa/example/allow")]() => "Hi!");
```

In both cases the default `OpaPolicyHandler` builds the input document from the current `HttpContext` (see
[Policy input](#policy-input)) and evaluates `example/allow`.

### Imperative authorization with a custom input

Call `IAuthorizationService.AuthorizeAsync` directly when you want to pass your own object as `input` instead of
deriving it from the request:

```csharp
app.MapGet(
    "/resource/{name}",
    ([FromServices] IAuthorizationService azs, ClaimsPrincipal user, string name) =>
    {
        var result = azs.AuthorizeAsync(user, new ResourcePolicyInput(name), "opa/example/check_resource");
        return result.Result.Succeeded ? Results.Ok($"Got access to {name}") : Results.Forbid();
    }
    );

internal record ResourcePolicyInput(string Resource);
```

`ResourcePolicyInput` is serialized as-is and becomes `input` on the Rego side (`input.resource`).

### Handling a specific resource type

The default `OpaPolicyHandler` only activates for `HttpContext`/`HttpRequest` resources. To handle a specific
resource type distinctly, register `OpaPolicyHandler<TResource>` (or a subclass of it) as an additional
`IAuthorizationHandler`:

```csharp
builder.Services.AddSingleton<IAuthorizationHandler, OpaPolicyHandler<ResourcePolicyInput>>();
```

## Policy input

For attribute-based and `HttpContext`-based authorization, the input document is built by
`IHttpRequestPolicyInput.Build(...)` and includes:

- `method`, `scheme`, `host`, `pathBase`, `path`, `queryString`, `query`, `protocol`
- `connection` (remote/local IP and port, client certificate PEM)
- `headers` — only headers whose name matches one of `OpaAuthorizationOptions.AllowedHeaders` (a set of regex
  patterns); by default no headers are forwarded, so you must opt in
- `claims` — only included when `OpaAuthorizationOptions.IncludeClaimsInHttpRequest = true`

```csharp
p.AllowedHeaders.Add("^X-.*");   // forward custom X- headers
p.IncludeClaimsInHttpRequest = true;
```

## Policy sources

A policy source is responsible for producing (and, optionally, hot-reloading) a compiled policy bundle. Three are
built in:

| Source                       | Registered via                                                                                             | Loads from                                                                                                                                        |
|------------------------------|------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------|
| `FileSystemPolicySource`     | `AddFileSystemPolicySource()`                                                                              | A directory of `.rego`/`data.json`/`data.yaml` files at `OpaAuthorizationOptions.PolicyBundlePath`, compiled with the registered `IRegoCompiler`. |
| `CompiledBundlePolicySource` | `AddPolicySource<CompiledBundlePolicySource>()`                                                            | An already-compiled `.tar.gz` wasm bundle file at `PolicyBundlePath` — no compiler needed.                                                        |
| `ConfigurationPolicySource`  | `AddConfigurationPolicySource(IConfiguration)` or `AddConfigurationPolicySource(Action<OpaPolicyOptions>)` | Policy sources embedded directly in configuration, compiled with the registered `IRegoCompiler`.                                                  |

`ConfigurationPolicySource` reads a map of named policies, each with a `package`, inline Rego `source`, and optional
`data.json`/`data.yaml`:

```yaml
policies:
  - name: p1
    source: |
      # METADATA
      # entrypoint: true
      package example.allow

      deny if {
        false
      }
```

```csharp
cfg.AddConfigurationPolicySource(builder.Configuration.GetSection("policies"));
```

All built-in sources support hot reload when `OpaAuthorizationOptions.MonitoringInterval > TimeSpan.Zero`:
`FileSystemPolicySource`/`CompiledBundlePolicySource` watch the file system for changes, `ConfigurationPolicySource`
reacts to `IOptionsMonitor<OpaPolicyOptions>` changes. On a successful recompilation the evaluator pool
(`PooledOpaPolicyService`) is swapped out atomically — in-flight evaluations on the old pool finish normally, new
ones use the recompiled policy.

You can implement `IOpaPolicySource` yourself (typically by deriving from the abstract `OpaPolicySource`, which
handles locking, hot-reload change tokens, and pool invalidation for you — implement only
`CompileBundleFromSource`) and register it with `AddPolicySource<T>()`.

## Compiler

`AddCompiler<T>()` registers the `IRegoCompiler` used by any policy source that compiles from `.rego` source
(everything except `CompiledBundlePolicySource`, which needs no compiler):

- `RegoCliCompiler` (`OpaDotNet.Compilation.Cli`) — shells out to the `opa` CLI binary; simplest to set up, requires
  `opa` on `PATH`.
- `RegoInteropCompiler` (`OpaDotNet.Compilation.Interop`) — compiles in-process via the native interop library; no
  external process, but requires the native library to be built/deployed alongside your app.

See [Compilation](~/articles/compilation/compilation.md) for details on both backends.
`OpaAuthorizationOptions.Compiler` (`RegoCompilerOptions`) configures entrypoints, capabilities, and other
compiler-specific options for whichever backend you register.

## Custom built-ins

Register custom built-in functions the same way you would for a bare `IOpaEvaluatorFactory`, via
`AddCustomBuiltins<T>()` (and `AddCustomBuiltins<TBuiltins, TCapabilities>()` when the built-ins need a capabilities
file merged into compilation):

```csharp
cfg.AddCustomBuiltins<MyBuiltins, MyBuiltins>();
```

See [Custom Built-in Functions](~/articles/evaluation/bultins.md) and [Advanced Built-ins](~/articles/reference/builtins.md)
for how to implement `IOpaCustomBuiltins`.

## Custom authorization handler

Override `OpaPolicyHandler.HandleRequirementAsync` when a plain allow/deny isn't enough — for example, to surface
structured deny reasons from the policy result instead of a bare boolean:

```csharp
internal class CustomPolicyHandler : OpaPolicyHandler
{
    public CustomPolicyHandler(
        IOpaPolicyService service,
        IOptions<OpaAuthorizationOptions> options,
        ILogger<CustomPolicyHandler> logger) : base(service, options, logger)
    {
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OpaPolicyRequirement requirement,
        IHttpRequestPolicyInput resource)
    {
        var result = await Service.Evaluate<IHttpRequestPolicyInput, PolicyResult>(resource, requirement.Entrypoint);

        if (result.Deny.Length == 0)
            context.Succeed(requirement);
        else
            Logger.LogDebug("Denied: {Reasons}", string.Join("\n", result.Deny.Select(p => p.Reason)));
    }
}

internal record PolicyResult { public DenyReason[] Deny { get; set; } = []; }
internal record DenyReason { public string? Reason { get; set; } }
```

> [!IMPORTANT]
> Register a custom `IAuthorizationHandler` *before* calling `AddOpaAuthorization` — `AddOpaAuthorization` registers
> the default `OpaPolicyHandler` with `TryAddSingleton`, so registering yours first (under the same
> `IAuthorizationHandler` service) takes priority.

## Configuration reference

Key `OpaAuthorizationOptions` (set via `cfg.AddConfiguration(...)`):

| Option                       | Purpose                                                                                                           |
|------------------------------|-------------------------------------------------------------------------------------------------------------------|
| `PolicyBundlePath`           | Directory (source/bundle policy sources) or file (`CompiledBundlePolicySource`) to load policies from.            |
| `Compiler`                   | `RegoCompilerOptions` — entrypoints, capabilities, output path, etc., passed to the registered `IRegoCompiler`.   |
| `EngineOptions`              | `WasmPolicyEngineOptions` for the underlying evaluator (serialization options, strict built-in errors, timeouts). |
| `AllowedHeaders`             | Regex patterns of request headers forwarded into policy input. Empty by default.                                  |
| `IncludeClaimsInHttpRequest` | Include the current user's claims in policy input. `false` by default.                                            |
| `AuthenticationSchemes`      | Authentication schemes `Opa/...` policies are evaluated against.                                                  |
| `MonitoringInterval`         | How often policy sources check for changes and hot-reload. `TimeSpan.Zero` disables hot reload.                   |
| `MaximumEvaluatorsRetained`  | Max pooled `IOpaEvaluator` instances kept alive. Defaults to `Environment.ProcessorCount * 2`.                    |
| `MaximumEvaluators`          | Caps concurrent evaluations; `0` (default) means unbounded.                                                       |

## Samples

Runnable samples live under [`src/Extensions.AspNetCore/samples`](https://github.com/me-viper/OpaDotNet/tree/main/src/Extensions.AspNetCore/samples)
in this repository:

- **WebApp** — attribute-based and imperative authorization, custom resource input.
- **CustomBuiltins** — registering custom built-in functions with `AddCustomBuiltins`.
- **YarpApp** — configuration-based policy source, a custom `OpaPolicyHandler` returning structured deny reasons, and
  gating a YARP reverse proxy behind OPA policies.
