using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Builtins;

namespace OpaDotNet.Extensions.AspNetCore;

internal sealed class AspNetCoreBuiltinsFactory : IBuiltinsFactory
{
    private readonly ImportsCache _importsCache;

    private readonly IServiceProvider _serviceProvider;

    public AspNetCoreBuiltinsFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _importsCache = new(_serviceProvider.GetRequiredService<IOptions<WasmPolicyEngineOptions>>().Value.SerializationOptions);
    }

    public IOpaImportsAbi Create()
    {
        return new CompositeImportsHandler(
            _serviceProvider.GetRequiredService<IOpaImportsAbi>(),
            _serviceProvider.GetServices<IOpaCustomBuiltins>().ToList(),
            _importsCache
            );
    }
}

public interface IOpaBundleEvaluatorFactoryBuilder
{
    OpaEvaluatorFactory Build(Stream policy);
}

internal class OpaBundleEvaluatorFactoryBuilder(IOptionsMonitor<OpaAuthorizationOptions> options, IBuiltinsFactory builtins)
    : IOpaBundleEvaluatorFactoryBuilder
{
    public OpaEvaluatorFactory Build(Stream policy) => new OpaBundleEvaluatorFactory(policy, options.CurrentValue.EngineOptions, builtins);
}

// internal class OpaImportsAbiFactory : IOpaImportsAbiFactory
// {
//     public Func<IOpaImportsAbi> ImportsAbi { get; }
//
//     public Func<Stream?> Capabilities { get; } = () => null;
//
//     internal OpaImportsAbiFactory()
//     {
//         ImportsAbi = () => new CoreImportsAbi();
//     }
//
//     public OpaImportsAbiFactory(Func<IOpaImportsAbi> importsAbi, IOptionsMonitor<OpaAuthorizationOptions> options)
//     {
//         ArgumentNullException.ThrowIfNull(importsAbi);
//
//         ImportsAbi = importsAbi;
//
//         Capabilities = () =>
//         {
//             var path = options.CurrentValue.Compiler?.CapabilitiesFilePath;
//             return !string.IsNullOrWhiteSpace(path) ? GetCapsFromFile(path) : null;
//         };
//     }
//
//     public OpaImportsAbiFactory(
//         Func<IOpaImportsAbi> importsAbi,
//         Func<Stream> capabilities,
//         IOptionsMonitor<OpaAuthorizationOptions> options)
//     {
//         ArgumentNullException.ThrowIfNull(importsAbi);
//         ArgumentNullException.ThrowIfNull(capabilities);
//
//         ImportsAbi = importsAbi;
//
//         Capabilities = () =>
//         {
//             var path = options.CurrentValue.Compiler?.CapabilitiesFilePath;
//             return !string.IsNullOrWhiteSpace(path) ? GetCapsFromFile(path) : capabilities();
//         };
//     }
//
//     private static FileStream GetCapsFromFile(string path) => new(path, FileMode.Open, FileAccess.Read);
// }