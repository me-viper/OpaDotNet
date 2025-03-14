﻿using System.Text.Json.Nodes;

namespace OpaDotNet.Wasm.Tests.Common;

internal class TestImportsAbi(ITestOutputHelper output) : DefaultOpaImportsAbi
{
    public override void Print(IEnumerable<string> args)
    {
        var str = args as string[] ?? args.ToArray();
        var o = string.Join(", ", str);
        output.WriteLine(o);
        base.Print(str);
    }

    public override object? Func(BuiltinContext context)
    {
        if (string.Equals(context.FunctionName, "opa.runtime"))
            return JsonNode.Parse("{}"u8);

        return base.Func(context);
    }

    public override object? Func(BuiltinContext context, BuiltinArg arg1)
    {
        if (string.Equals(context.FunctionName, "test.sleep"))
            return Sleep(arg1.As<string>());

        return base.Func(context, arg1);
    }

    private static long Sleep(string duration)
    {
        var d = ParseDurationNs(duration) ?? throw new FormatException();
        Thread.Sleep(TimeSpan.FromTicks(d / TimeSpan.NanosecondsPerTick));
        return d;
    }

    protected override bool OnError(BuiltinContext context, Exception ex)
    {
        output.WriteLine(ex.ToString());
        return base.OnError(context, ex);
    }

    public static ReadOnlyMemory<byte> Caps()
    {
        var r = """
            {
                "builtins": [
                {
                  "name": "test.sleep",
                  "decl": {
                    "type": "function",
                    "args": [ { "type": "string" } ],
                    "result": { "type": "number" }
                  }
                }
                ]
            }
            """u8;

        return new ReadOnlyMemory<byte>(r.ToArray());
    }
}