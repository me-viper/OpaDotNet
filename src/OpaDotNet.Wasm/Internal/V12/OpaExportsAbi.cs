using Wasmtime;

namespace OpaDotNet.Wasm.Internal.V12;

internal class OpaExportsAbi : V10.OpaExportsAbi, IOpaExportsAbi, IAbiInitializer<OpaExportsAbi>
{
    private static Version Version { get; } = new(1, 2);

    private readonly Func<int, int, int, int, int, int, int, int> _evalV12;

    static OpaExportsAbi IAbiInitializer<OpaExportsAbi>.Initialize(Instance instance)
    {
        return new OpaExportsAbi(instance);
    }

    protected OpaExportsAbi(Instance instance) : base(instance)
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