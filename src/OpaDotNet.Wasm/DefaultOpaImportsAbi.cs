using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

namespace OpaDotNet.Wasm;

[PublicAPI]
public partial class DefaultOpaImportsAbi : IOpaImportsAbi
{
    protected ConcurrentDictionary<string, object> ValueCache { get; } = new();

    [ExcludeFromCodeCoverage]
    protected virtual DateTimeOffset Now()
    {
        return DateTimeOffset.Now;
    }

    public virtual void Reset()
    {
        ValueCache.Clear();
    }

    [ExcludeFromCodeCoverage]
    public virtual void Abort(string message)
    {
        throw new OpaEvaluationException("Aborted: " + message);
    }

    [ExcludeFromCodeCoverage]
    public virtual void PrintLn(string message)
    {
    }
    
    public virtual object? Func(BuiltinContext context)
    {
        return context.FunctionName switch
        {
            "time.now_ns" => NowNs(Now()),
            _ => throw new NotImplementedException(context.FunctionName)
        };
    }

    public virtual object? Func(BuiltinContext context, BuiltinArg arg1)
    {
        return context.FunctionName switch
        {
            "time.date" => Date(arg1.As<long>()),
            "time.clock" => Clock(arg1.As<long>()),
            "time.weekday" => Weekday(arg1.As<long>()),
            "time.parse_rfc3339_ns" => ParseRfc3339Ns(arg1.As<string>()),
            "uuid.rfc4122" => NewGuid(arg1.As<string>()),
            "net.cidr_expand" => CidrExpand(arg1.As<string>()),
            "net.cidr_is_valid" => CidrIsValid(arg1.As<string>()),
            "net.lookup_ip_addr" => LookupIPAddress(arg1.As<string>()),
            _ => throw new NotImplementedException(context.FunctionName)
        };
    }

    public virtual object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2)
    {
        return context.FunctionName switch
        {
            "indexof_n" => IndexOfN(arg1.As<string>(), arg2.As<string>()),
            "strings.any_prefix_match" => AnyPrefixMatch(arg1.As<string[]>(), arg2.As<string[]>()),
            "strings.any_suffix_match" => AnySuffixMatch(arg1.As<string[]>(), arg2.As<string[]>()),
            "time.diff" => Diff(arg1.As<long>(), arg2.As<long>()),
            "net.cidr_contains_matches" => CidrContainsMatches(arg1.Raw, arg2.Raw),
            _ => throw new NotImplementedException(context.FunctionName)
        };
    }

    public virtual object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3)
    {
        throw new NotImplementedException(context.FunctionName);
    }

    public virtual object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3, BuiltinArg arg4)
    {
        return context.FunctionName switch
        {
            "time.add_date" => AddDate(arg1.As<long>(), arg2.As<int>(), arg3.As<int>(), arg4.As<int>()),
            _ => throw new NotImplementedException(context.FunctionName)
        };
    }
}