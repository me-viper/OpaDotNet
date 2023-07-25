﻿using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

// ReSharper disable HeapView.BoxingAllocation

namespace OpaDotNet.Wasm;

[PublicAPI]
public partial class DefaultOpaImportsAbi : IOpaImportsAbi
{
    private readonly ConcurrentDictionary<string, object> _valueCache = new();
    
    protected T CacheGetOrAddValue<T>(string key, Func<T> valueFactory) where T : notnull
    {
        return (T)_valueCache.GetOrAdd(key, valueFactory());
    }

    [ExcludeFromCodeCoverage]
    protected virtual DateTimeOffset Now()
    {
        return DateTimeOffset.Now;
    }

    [ExcludeFromCodeCoverage]
    protected virtual Guid NewGuid()
    {
        return Guid.NewGuid();
    }

    public virtual void Reset()
    {
        _valueCache.Clear();
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    [DoesNotReturn]
    public virtual void Abort(string message)
    {
        throw new OpaEvaluationAbortedException("Aborted: " + message);
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public virtual void PrintLn(string message)
    {
    }

    [ExcludeFromCodeCoverage]
    protected virtual bool Trace(string message)
    {
        return true;
    }

    /// <summary>
    /// Called if built-in function throws error.
    /// </summary>
    /// <param name="context">Built-in function call context</param>
    /// <param name="ex">Exception thrown</param>
    /// <returns><c>true</c> if exception should be rethrown; <c>false</c> suppress exception and return default result</returns>
    [ExcludeFromCodeCoverage]
    protected virtual bool OnError(BuiltinContext context, Exception ex)
    {
        if (ex is NotImplementedException)
            return true;

        return false;
    }

    /// <inheritdoc />
    public virtual object? Func(BuiltinContext context)
    {
        try
        {
            return context.FunctionName switch
            {
                "time.now_ns" => NowNs(),
                "opa.runtime" => OpaRuntime(),
                _ => throw new NotImplementedException(context.FunctionName),
            };
        }
        catch (Exception ex)
        {
            if (OnError(context, ex))
                throw;

            return null;
        }
    }

    /// <inheritdoc />
    public virtual object? Func(BuiltinContext context, BuiltinArg arg1)
    {
        try
        {
            return context.FunctionName switch
            {
                "trace" => Trace(arg1.As<string>()),
                "time.date" => Date(arg1.As<long>()),
                "time.clock" => Clock(arg1.As<long>()),
                "time.weekday" => Weekday(arg1.As<long>()),
                "time.parse_rfc3339_ns" => ParseRfc3339Ns(arg1.As<string>()),
                "time.parse_duration_ns" => ParseDurationNs(arg1.As<string>()),
                "uuid.rfc4122" => NewGuid(arg1.As<string>()),
                "net.cidr_expand" => CidrExpand(arg1.As<string>()),
                "net.cidr_is_valid" => CidrIsValid(arg1.As<string>()),
                "net.cidr_merge" => CidrMerge(arg1.As<string[]>()),
                "net.lookup_ip_addr" => LookupIPAddress(arg1.As<string>()),
                "crypto.md5" => HashMd5(arg1.As<string>()),
                "crypto.sha1" => HashSha1(arg1.As<string>()),
                "crypto.sha256" => HashSha256(arg1.As<string>()),
                "base64url.encode_no_pad" => Base64UrlEncodeNoPad(arg1.As<string>()),
                "hex.decode" => HexDecode(arg1.As<string>()),
                "hex.encode" => HexEncode(arg1.As<string>()),
                "urlquery.encode" => UrlQueryEncode(arg1.As<string>()),
                "urlquery.decode" => UrlQueryDecode(arg1.As<string>()),
                "urlquery.decode_object" => UrlQueryDecodeObject(arg1.As<string>()),
                "urlquery.encode_object" => UrlQueryEncodeObject(arg1.RawJson, context.JsonSerializerOptions),
                "io.jwt.decode" => JwtDecode(arg1.As<string>()),
                "semver.is_valid" => SemverIsValid(arg1.RawJson),
                "yaml.is_valid" => YamlIsValid(arg1.RawJson),
                "yaml.marshal" => YamlMarshal(arg1.RawJson),
                "yaml.unmarshal" => YamlUnmarshal(arg1.As<string>()),
                "json.verify_schema" => JsonVerifySchema(arg1.RawJson, out _),
                _ => throw new NotImplementedException(context.FunctionName),
            };
        }
        catch (Exception ex)
        {
            if (OnError(context, ex))
                throw;

            return null;
        }
    }

    /// <inheritdoc />
    public virtual object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2)
    {
        try
        {
            return context.FunctionName switch
            {
                "indexof_n" => IndexOfN(arg1.As<string>(), arg2.As<string>()),
                "sprintf" => Sprintf(arg1.As<string>(), arg2.Raw),
                "rand.intn" => RandIntN(arg1.As<string>(), arg2.As<int>()),
                "strings.any_prefix_match" => AnyPrefixMatch(arg1.As<string[]>(), arg2.As<string[]>()),
                "strings.any_suffix_match" => AnySuffixMatch(arg1.As<string[]>(), arg2.As<string[]>()),
                "time.diff" => Diff(arg1.As<long>(), arg2.As<long>()),
                "crypto.hmac.equal" => HmacEqual(arg1.As<string>(), arg2.As<string>()),
                "crypto.hmac.md5" => HmacMd5(arg1.As<string>(), arg2.As<string>()),
                "crypto.hmac.sha1" => HmacSha1(arg1.As<string>(), arg2.As<string>()),
                "crypto.hmac.sha256" => HmacSha256(arg1.As<string>(), arg2.As<string>()),
                "crypto.hmac.sha512" => HmacSha512(arg1.As<string>(), arg2.As<string>()),
                "regex.split" => RegexSplit(arg1.As<string>(), arg2.As<string>()),
                "net.cidr_contains_matches" => CidrContainsMatches(arg1.Raw, arg2.Raw, context.JsonSerializerOptions),
                "io.jwt.decode_verify" => JwtDecodeVerify(arg1.As<string>(), arg2.As<JwtConstraints>()),
                "io.jwt.verify_hs256" => JwtVerifyHs(arg1.As<string>(), arg2.As<string>(), "HS256"),
                "io.jwt.verify_hs384" => JwtVerifyHs(arg1.As<string>(), arg2.As<string>(), "HS384"),
                "io.jwt.verify_hs512" => JwtVerifyHs(arg1.As<string>(), arg2.As<string>(), "HS512"),
                "io.jwt.verify_es256" => JwtVerifyCert(arg1.As<string>(), arg2.As<string>(), "ES256"),
                "io.jwt.verify_es384" => JwtVerifyCert(arg1.As<string>(), arg2.As<string>(), "ES384"),
                "io.jwt.verify_es512" => JwtVerifyCert(arg1.As<string>(), arg2.As<string>(), "ES512"),
                "io.jwt.verify_ps256" => JwtVerifyCert(arg1.As<string>(), arg2.As<string>(), "PS256"),
                "io.jwt.verify_ps384" => JwtVerifyCert(arg1.As<string>(), arg2.As<string>(), "PS384"),
                "io.jwt.verify_ps512" => JwtVerifyCert(arg1.As<string>(), arg2.As<string>(), "PS512"),
                "io.jwt.verify_rs256" => JwtVerifyCert(arg1.As<string>(), arg2.As<string>(), "RS256"),
                "io.jwt.verify_rs384" => JwtVerifyCert(arg1.As<string>(), arg2.As<string>(), "RS384"),
                "io.jwt.verify_rs512" => JwtVerifyCert(arg1.As<string>(), arg2.As<string>(), "RS512"),
                "semver.compare" => SemverCompare(arg1.As<string>(), arg2.As<string>()),
                "json.patch" => JsonPatch(arg1.RawJson, arg2.RawJson),
                "json.match_schema" => JsonMatchSchema(arg1.RawJson, arg2.RawJson),
                _ => throw new NotImplementedException(context.FunctionName),
            };
        }
        catch (Exception ex)
        {
            if (OnError(context, ex))
                throw;

            return null;
        }
    }

    /// <inheritdoc />
    public virtual object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3)
    {
        try
        {
            return context.FunctionName switch
            {
                "regex.find_n" => RegexFindN(arg1.As<string>(), arg2.As<string>(), arg3.As<int>()),
                "regex.replace" => RegexReplace(arg1.As<string>(), arg2.As<string>(), arg3.As<string>()),
                "io.jwt.encode_sign" => JwtEncodeSign(arg1.RawJson, arg2.RawJson, arg3.RawJson),
                "io.jwt.encode_sign_raw" => JwtEncodeSignRaw(arg1.As<string>(), arg2.As<string>(), arg3.As<string>()),
                _ => throw new NotImplementedException(context.FunctionName),
            };
        }
        catch (Exception ex)
        {
            if (OnError(context, ex))
                throw;

            return null;
        }
    }

    /// <inheritdoc />
    public virtual object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3, BuiltinArg arg4)
    {
        try
        {
            return context.FunctionName switch
            {
                "time.add_date" => AddDate(arg1.As<long>(), arg2.As<int>(), arg3.As<int>(), arg4.As<int>()),
                "regex.template_match" => RegexTemplateMatch(arg1.As<string>(), arg2.As<string>(), arg3.As<string>(), arg4.As<string>()),
                _ => throw new NotImplementedException(context.FunctionName),
            };
        }
        catch (Exception ex)
        {
            if (OnError(context, ex))
                throw;

            return null;
        }
    }
}