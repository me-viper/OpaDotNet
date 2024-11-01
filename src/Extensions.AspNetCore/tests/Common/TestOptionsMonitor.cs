using Microsoft.Extensions.Options;

using OpaDotNet.Common;

namespace OpaDotNet.Extensions.AspNetCore.Tests.Common;

internal static class TestOptionsMonitor
{
    public static TestOptionsMonitor<T> Create<T>(T opts) where T : class => new(opts);

    public static TestOptionsMonitor<T> Create<T>(IOptions<T> opts) where T : class => new(opts.Value);
}

internal class TestOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
    where TOptions : class
{
    private readonly TOptions _current;

    public TestOptionsMonitor(TOptions current)
    {
        ArgumentNullException.ThrowIfNull(current);
        _current = current;
    }

    public TOptions Get(string? name) => _current;

    public IDisposable? OnChange(Action<TOptions, string?> listener) => new NopDisposable();

    public TOptions CurrentValue => _current;

    public IOptions<TOptions> Option() => new OptionsWrapper<TOptions>(_current);
}