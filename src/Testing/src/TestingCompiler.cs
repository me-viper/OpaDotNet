using Microsoft.Extensions.Logging;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Cli;

namespace OpaDotNet.InternalTesting;

public class TestingCompiler(ILoggerFactory? loggerFactory = null)
#if INTEROP_COMPILER
    : RegoInteropCompiler(logger: loggerFactory?.CreateLogger<RegoInteropCompiler>())
#else
    : RegoCliCompiler(logger: loggerFactory?.CreateLogger<RegoCliCompiler>())
#endif
{
}