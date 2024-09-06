using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Builtins;

namespace OpaDotNet.Extensions.AspNetCore.Tests.Common;

internal class TestBuiltinsFactory(ILoggerFactory? loggerFactory = null, TimeProvider? timeProvider = null) : IBuiltinsFactory
{
    private ILogger<CoreImportsAbi> Logger { get; } = loggerFactory == null
        ? NullLogger<CoreImportsAbi>.Instance
        : loggerFactory.CreateLogger<CoreImportsAbi>();

    public IReadOnlyList<Func<IOpaCustomBuiltins>> CustomBuiltins { get; init; } = [];

    public IOpaImportsAbi Create()
    {
        return new CompositeImportsHandler(
            new CoreImportsAbi(Logger, timeProvider ?? TimeProvider.System),
            CustomBuiltins.Select(p => p()).ToList(),
            new ImportsCache(JsonSerializerOptions.Default)
            );
    }
}