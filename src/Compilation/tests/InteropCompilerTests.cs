using System.Reflection;
using System.Runtime.InteropServices;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

using OpaDotNet.Compilation.Interop;
using OpaDotNet.InternalTesting;

using Xunit.Abstractions;

namespace OpaDotNet.Compilation.Tests;

[UsedImplicitly]
[Trait(Utils.CompilerTrait, Utils.InteropCompilerTrait)]
public class InteropCompilerTests(ITestOutputHelper output) : CompilerTests<RegoInteropCompiler>(output)
{
    static InteropCompilerTests()
    {
        // https://github.com/dotnet/sdk/issues/24708
        NativeLibrary.SetDllImportResolver(
            typeof(RegoInteropCompiler).Assembly,
            DllImportResolver
            );
    }

    protected override string BaseOutputPath => "iop";

    protected override RegoInteropCompiler CreateCompiler(ILoggerFactory? loggerFactory = null)
    {
        return new RegoInteropCompiler(loggerFactory?.CreateLogger<RegoInteropCompiler>());
    }

    private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return NativeLibrary.Load(Path.Combine("runtimes/linux-x64/native", libraryName), assembly, searchPath);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return NativeLibrary.Load(Path.Combine("runtimes/win-x64/native", libraryName), assembly, searchPath);

        return IntPtr.Zero;
    }
}