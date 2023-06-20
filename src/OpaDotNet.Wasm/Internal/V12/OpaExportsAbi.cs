using OpaDotNet.Wasm.Internal.V10;

using Wasmtime;

namespace OpaDotNet.Wasm.Internal.V12;

internal class OpaExportsAbi : V10.OpaExportsAbi, IOpaExportsAbi
{
    private static Version Version { get; } = new(1, 2);

    private readonly Func<int, int, int, int, int, int, int, int> _evalV12;

    public OpaExportsAbi(Instance instance) : base(instance)
    {
        _evalV12 = instance.GetFunction<int, int, int, int, int, int, int, int>("opa_eval")
            ?? throw new ExportResolutionException(Version, "opa_eval");
    }

    public nint Eval(
        int reserved,
        int entrypointId,
        nint dataPtr,
        nint inputPtr,
        int inputLength,
        nint heapPtr,
        EvaluationOutputFormat format)
    {
        return _evalV12(
            reserved,
            entrypointId,
            dataPtr.ToInt32(),
            inputPtr.ToInt32(),
            inputLength,
            heapPtr.ToInt32(),
            (int)format
            );
    }
}