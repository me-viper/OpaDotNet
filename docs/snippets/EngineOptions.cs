// ReSharper disable RedundantUsingDirective

#pragma warning disable CS0105

#region EngineOptionsUsings

using System.Text.Json;

using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Features;

#endregion

#pragma warning restore CS0105

namespace Snippets;

public partial class DocSamples
{
    [Fact]
    public void EngineOptions()
    {
        #region EngineOptions

        var options = new WasmPolicyEngineOptions
        {
            // Cancel evaluations running longer than 2 seconds.
            Timeout = TimeSpan.FromSeconds(2),

            // Start with 2 pages (128 KiB) and let memory grow up to 256 pages (16 MiB).
            MinMemoryPages = 2,
            MaxMemoryPages = 256,

            // Fail evaluation instead of treating built-in errors as undefined.
            StrictBuiltinErrors = true,
        };

        using var bundle = File.OpenRead("data/bundle.tar.gz");
        using var factory = new OpaBundleEvaluatorFactory(bundle, options);
        using var engine = factory.Create();

        #endregion

        var result = engine.EvaluatePredicate(new { message = "world" });
        Assert.True(result.Result);
    }

    #region EngineSerializationInput

    public record HelloInput(string Message);

    #endregion

    [Fact]
    public void EngineSerializationOptions()
    {
        #region EngineSerializationOptions

        // By default C# property names are used as is: HelloInput.Message would become input.Message.
        var options = WasmPolicyEngineOptions.DefaultWithJsonOptions(
            p => p.PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            );

        using var bundle = File.OpenRead("data/bundle.tar.gz");
        using var engine = OpaBundleEvaluatorFactory.Create(bundle, options);

        // Input is serialized as { "message": "world" }.
        var result = engine.EvaluatePredicate(new HelloInput("world"));

        #endregion

        Assert.True(result.Result);
    }

    [Fact]
    public void EngineCachePath()
    {
        #region EngineCachePath

        // Directory must exist and be writable.
        var cache = Directory.CreateTempSubdirectory("opa-");

        var options = new WasmPolicyEngineOptions
        {
            CachePath = cache.FullName,
        };

        using var bundle = File.OpenRead("data/bundle.tar.gz");

        // The factory copies the bundle into its own subdirectory of CachePath
        // and deletes that subdirectory when disposed.
        using (var factory = new OpaBundleEvaluatorFactory(bundle, options))
        {
            // Each call reads and unpacks the bundle from disk.
            using var engine = factory.Create();
            var result = engine.EvaluatePredicate(new { message = "world" });
        }

        #endregion

        Assert.Empty(cache.EnumerateFileSystemInfos());
        cache.Delete();
    }

    [Fact]
    public void EngineMaxAbiVersion()
    {
        #region EngineMaxAbiVersion

        var options = new WasmPolicyEngineOptions
        {
            // Use ABI 1.2 even if the policy was built for a newer ABI.
            MaxAbiVersion = new Version(1, 2),
        };

        using var bundle = File.OpenRead("data/bundle.tar.gz");
        using var engine = OpaBundleEvaluatorFactory.Create(bundle, options);

        // engine.AbiVersion is 1.2 now, so features that need ABI 1.3 are not available.
        var canUpdateData = engine.TryGetFeature<IUpdateDataFeature>(out _);

        #endregion

        Assert.Equal(new Version(1, 2), engine.AbiVersion);
        Assert.False(canUpdateData);
    }
}
