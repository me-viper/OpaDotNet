using Microsoft.Extensions.Logging.Abstractions;

using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore;

public class CoreImportsAbi : DefaultOpaImportsAbi
{
    private readonly ILogger _logger;

    public CoreImportsAbi(ILogger<CoreImportsAbi>? logger = null)
    {
        _logger = logger ?? NullLogger<CoreImportsAbi>.Instance;
    }

    public override void PrintLn(string message)
    {
        _logger.LogDebug("{Message}", message);
    }

    public override void Print(IEnumerable<string> args)
    {
        _logger.LogDebug("{Message}", string.Join(", ", args));
    }

    protected override bool Trace(string message)
    {
        _logger.LogDebug("{Message}", message);
        return base.Trace(message);
    }

    protected override bool OnError(BuiltinContext context, Exception ex)
    {
        _logger.LogError(ex, "Failed to evaluate {Function}", context.FunctionName);
        return base.OnError(context, ex);
    }
}