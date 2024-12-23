using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using OpaDotNet.Compilation.Abstractions;

namespace OpaDotNet.Compilation.Interop;

internal static class Interop
{
    private const string Lib = "Opa.Interop";

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct OpaVersion
    {
        public string LibVersion;

        public string GoVersion;

        public string? Commit;

        public string Platform;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct OpaBytesBuildParams
    {
        public nint Bytes;

        public int BytesLen;

        public OpaBuildParams Params;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct OpaFsBuildParams
    {
        public string Source;

        public OpaBuildParams Params;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct OpaBuildParams
    {
        public string Target;

        public string? CapabilitiesJson;

        public string? CapabilitiesVersion;

        public int BundleMode;

        public nint Entrypoints;

        public int EntrypointsLen;

        public int Debug;

        public int OptimizationLevel;

        public int PruneUnused;

        public string? TempDir;

        public string? Revision;

        public nint Ignore;

        public int IgnoreLen;

        public int RegoVersion;

        public int FollowSymlinks;

        public int DisablePrintStatements;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct OpaBuildResult
    {
        public nint Result;

        public int ResultLen;

        public string? Errors;

        public string? Log;
    }

    [DllImport(Lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern void OpaGetVersion([Out] out nint version);

    [DllImport(Lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern void OpaFreeVersion([In] nint version);

    [DllImport(Lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int OpaBuildFromFs(
        [In] ref OpaFsBuildParams buildParams,
        [Out] out nint result);

    [DllImport(Lib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int OpaBuildFromBytes(
        [In] ref OpaBytesBuildParams buildParams,
        [Out] out nint result);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void OpaFree([In] nint buildResult);

    private static Stream Compile(
        Func<OpaBuildParams, (int, nint)> compile,
        CompilationParameters options,
        bool forceBundle,
        ILogger? logger)
    {
        ArgumentNullException.ThrowIfNull(compile);
        ArgumentNullException.ThrowIfNull(options);

        logger ??= NullLogger.Instance;

        var pEntrypoints = nint.Zero;
        var entrypointsList = Array.Empty<nint>();

        var pIgnore = nint.Zero;
        var ignoreList = Array.Empty<nint>();

        try
        {
            string? caps = null;

            if (!string.IsNullOrWhiteSpace(options.CapabilitiesFilePath))
                caps = File.ReadAllText(options.CapabilitiesFilePath);
            else if (!options.CapabilitiesBytes.IsEmpty)
                caps = Encoding.UTF8.GetString(options.CapabilitiesBytes.Span);

            var buildParams = new OpaBuildParams
            {
                CapabilitiesVersion = options.CapabilitiesVersion,
                CapabilitiesJson = caps,
                BundleMode = forceBundle || options.IsBundle ? 1 : 0,
                OptimizationLevel = 0,
                Target = "wasm",
                Debug = options.Debug ? 1 : 0,
                PruneUnused = options.PruneUnused ? 1 : 0,

                //TempDir = string.IsNullOrWhiteSpace(options.OutputPath) ? null : Path.GetFullPath(options.OutputPath),
                RegoVersion = (int)options.RegoVersion + 1,
                Revision = options.Revision,
                FollowSymlinks = options.FollowSymlinks ? 1 : 0,
                DisablePrintStatements = options.DisablePrintStatements ? 1 : 0,
            };

            if (options.Entrypoints != null)
            {
                var ep = options.Entrypoints.ToArray();
                pEntrypoints = Marshal.AllocCoTaskMem(ep.Length * nint.Size);
                entrypointsList = new nint[ep.Length];

                for (var i = 0; i < ep.Length; i++)
                    entrypointsList[i] = Marshal.StringToCoTaskMemAnsi(ep[i]);

                Marshal.Copy(entrypointsList, 0, pEntrypoints, ep.Length);

                buildParams.Entrypoints = pEntrypoints;
                buildParams.EntrypointsLen = ep.Length;
            }

            if (options.Ignore is { Count: > 0 })
            {
                pIgnore = Marshal.AllocCoTaskMem(options.Ignore.Count * nint.Size);
                ignoreList = new nint[options.Ignore.Count];

                var i = 0;

                foreach (var ign in options.Ignore)
                    ignoreList[i++] = Marshal.StringToCoTaskMemAnsi(ign);

                Marshal.Copy(ignoreList, 0, pIgnore, options.Ignore.Count);

                buildParams.Ignore = pIgnore;
                buildParams.IgnoreLen = options.Ignore.Count;
            }

            var bundle = nint.Zero;

            try
            {
                (var result, bundle) = compile(buildParams);

                if (bundle == nint.Zero)
                    throw new RegoCompilationException("Compilation failed");

                var resultBundle = Marshal.PtrToStructure<OpaBuildResult>(bundle);

                if (!string.IsNullOrWhiteSpace(resultBundle.Log))
                    logger.LogDebug("{BuildLog}", resultBundle.Log);

                if (!string.IsNullOrWhiteSpace(resultBundle.Errors))
                    throw new RegoCompilationException(resultBundle.Errors);

                if (result != 0)
                    throw new RegoCompilationException($"Compilation error {result}");

                if (resultBundle.ResultLen == 0 || resultBundle.Result == nint.Zero)
                    throw new RegoCompilationException("Bad result");

                var bundleBytes = new byte[resultBundle.ResultLen];
                Marshal.Copy(resultBundle.Result, bundleBytes, 0, resultBundle.ResultLen);

                return new MemoryStream(bundleBytes);
            }
            finally
            {
                if (bundle != nint.Zero)
                    OpaFree(bundle);
            }
        }
        finally
        {
            if (pEntrypoints != nint.Zero)
            {
                foreach (var p in entrypointsList)
                    Marshal.FreeCoTaskMem(p);

                Marshal.FreeCoTaskMem(pEntrypoints);
            }

            if (pIgnore != nint.Zero)
            {
                foreach (var p in ignoreList)
                    Marshal.FreeCoTaskMem(p);

                Marshal.FreeCoTaskMem(pIgnore);
            }
        }
    }

    public static Stream Compile(
        string source,
        CompilationParameters options,
        bool forceBundle,
        ILogger? logger)
    {
        ArgumentException.ThrowIfNullOrEmpty(source);
        ArgumentNullException.ThrowIfNull(options);

        static (int, nint) CompileFunc(string source, OpaBuildParams buildParams)
        {
            var fsBuildParams = new OpaFsBuildParams
            {
                Source = source,
                Params = buildParams,
            };

            var result = OpaBuildFromFs(ref fsBuildParams, out var bundle);
            return (result, bundle);
        }

        return Compile(p => CompileFunc(source, p), options, forceBundle, logger);
    }

    public static Stream Compile(
        Stream source,
        CompilationParameters options,
        bool forceBundle,
        ILogger? logger)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);

        static (int, nint) CompileFunc(Stream source, OpaBuildParams buildParams)
        {
            var bytes = nint.Zero;

            try
            {
                var len = (int)source.Length;
                bytes = Marshal.AllocCoTaskMem(len);

                var buf = new byte[len];
                var read = source.Read(buf);
                Marshal.Copy(buf, 0, bytes, read);

                var bytesBuildParams = new OpaBytesBuildParams
                {
                    Bytes = bytes,
                    BytesLen = len,
                    Params = buildParams,
                };

                var result = OpaBuildFromBytes(ref bytesBuildParams, out var bundle);
                return (result, bundle);
            }
            finally
            {
                Marshal.FreeCoTaskMem(bytes);
            }
        }

        return Compile(p => CompileFunc(source, p), options, forceBundle, logger);
    }
}