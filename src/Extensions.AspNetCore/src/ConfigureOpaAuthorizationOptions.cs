using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Builtins;

namespace OpaDotNet.Extensions.AspNetCore;

internal class ConfigureOpaAuthorizationOptions(IServiceProvider serviceProvider) : IConfigureOptions<OpaAuthorizationOptions>
{
    public void Configure(OpaAuthorizationOptions options)
    {
        options.EngineOptions ??= WasmPolicyEngineOptions.Default;
        options.EngineOptions.ConfigureBuiltins(
            p =>
            {
                p.DefaultBuiltins = serviceProvider.GetRequiredService<IOpaImportsAbi>();

                foreach (var bi in serviceProvider.GetRequiredService<IEnumerable<IOpaCustomBuiltins>>())
                    p.CustomBuiltins.Add(bi);
            }
            );
    }
}