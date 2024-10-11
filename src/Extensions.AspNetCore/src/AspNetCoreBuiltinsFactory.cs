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