// ReSharper disable RedundantUsingDirective

#pragma warning disable CS0105

#region CapabilitiesUsings

using OpaDotNet.Wasm;
using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Cli;

#endregion

#pragma warning restore CS0105

namespace Snippets;

public partial class DocSamples
{
    [Fact]
    public async Task CapabilitiesVersion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        #region CapabilitiesVersion

        var compiler = new RegoCliCompiler();

        var parameters = new CompilationParameters
        {
            // Validate policies against built-ins available in OPA v0.53.1.
            CapabilitiesVersion = "v0.53.1",
            Entrypoints = ["example/hello"],
        };

        await using var policy = await compiler.CompileBundleAsync("quickstart", parameters, cancellationToken);

        #endregion

        using var engine = OpaBundleEvaluatorFactory.Create(policy);
        var result = engine.EvaluatePredicate(new { message = "world" });

        Assert.True(result.Result);
    }

    [Fact]
    public async Task CapabilitiesFile()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        #region CapabilitiesFile

        var compiler = new RegoCliCompiler();

        var parameters = new CompilationParameters
        {
            // Validate policies against built-ins defined in the capabilities file only.
            CapabilitiesFilePath = Path.Combine("builtins", "capabilities.json"),
            Entrypoints = ["custom_builtins/zero_arg"],
        };

        #endregion

        // capabilities.json declares custom built-ins only, so even `eq` (`=`) is undefined.
        await Assert.ThrowsAsync<RegoCompilationException>(() => compiler.CompileBundleAsync("builtins", parameters, cancellationToken)
            );
    }

    [Fact]
    public async Task CapabilitiesMerged()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        #region CapabilitiesMerged

        var compiler = new RegoCliCompiler();

        var parameters = new CompilationParameters
        {
            // Built-ins from OPA v0.53.1 capabilities...
            CapabilitiesVersion = "v0.53.1",

            // ...merged with built-ins defined in the capabilities file.
            CapabilitiesFilePath = Path.Combine("builtins", "capabilities.json"),
            Entrypoints = ["custom_builtins/zero_arg"],
        };

        await using var policy = await compiler.CompileBundleAsync("builtins", parameters, cancellationToken);

        #endregion

        Assert.True(policy.Length > 0);
    }
}