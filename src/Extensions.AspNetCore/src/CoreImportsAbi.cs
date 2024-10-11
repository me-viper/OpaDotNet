using Microsoft.Extensions.Logging.Abstractions;

using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore;

public class CoreImportsAbi(ILogger<CoreImportsAbi> logger, TimeProvider timeProvider) : DefaultOpaImportsAbi
{
    internal CoreImportsAbi() : this(NullLogger<CoreImportsAbi>.Instance, TimeProvider.System)
    {
    }

    protected override DateTimeOffset Now() => timeProvider.GetUtcNow();

    public override void Print(IEnumerable<string> args) => logger.LogDebug("{Message}", string.Join(", ", args));

    protected override bool OnError(BuiltinContext context, Exception ex)
    {
        logger.LogError(ex, "Failed to evaluate {Function}", context.FunctionName);
        return base.OnError(context, ex);
    }
}