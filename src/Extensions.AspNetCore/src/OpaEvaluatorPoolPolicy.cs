using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.ObjectPool;

using OpaDotNet.Extensions.AspNetCore.Telemetry;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Features;

namespace OpaDotNet.Extensions.AspNetCore;

internal class OpaEvaluatorPoolPolicy : PooledObjectPolicy<IOpaEvaluator>
{
    private readonly Func<IOpaEvaluator> _factory;

    public OpaEvaluatorPoolPolicy(Func<IOpaEvaluator> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _factory = factory;
    }

    public override IOpaEvaluator Create() => new TrackedEvaluator(_factory());

    public override bool Return(IOpaEvaluator obj) => true;

    [ExcludeFromCodeCoverage]
    private sealed class TrackedEvaluator : IOpaEvaluator
    {
        private readonly IOpaEvaluator _inner;

        public Version AbiVersion => _inner.AbiVersion;

        public TrackedEvaluator(IOpaEvaluator inner)
        {
            OpaEventSource.Log.EvaluatorCreated();
            _inner = inner;
        }

        public void Dispose()
        {
            OpaEventSource.Log.EvaluatorReleased();
            _inner.Dispose();
        }

        public PolicyEvaluationResult<bool> EvaluatePredicate<TInput>(TInput input, string? entrypoint = null)
            => _inner.EvaluatePredicate(input, entrypoint);

        public PolicyEvaluationResult<TOutput> Evaluate<TInput, TOutput>(TInput input, string? entrypoint = null) where TOutput : notnull
            => _inner.Evaluate<TInput, TOutput>(input, entrypoint);

        public string EvaluateRaw(ReadOnlySpan<char> inputJson, string? entrypoint = null)
            => _inner.EvaluateRaw(inputJson, entrypoint);

        public void SetDataFromRawJson(ReadOnlySpan<char> dataJson) => _inner.SetDataFromRawJson(dataJson);

        public void SetDataFromStream(Stream? utf8Json) => _inner.SetDataFromStream(utf8Json);

        public void SetData<T>(T? data) where T : class => _inner.SetData(data);

        public void Reset() => _inner.Reset();

        public bool TryGetFeature<TFeature>([MaybeNullWhen(false)] out TFeature feature) where TFeature : class, IOpaEvaluatorFeature
            => _inner.TryGetFeature(out feature);
    }
}