// ReSharper disable RedundantUsingDirective
// ReSharper disable Xunit.XunitTestWithConsoleOutput

#pragma warning disable CS0105

using System.Globalization;

using OpaDotNet.Wasm.Builtins;

#region Usings

using OpaDotNet.Wasm;

#endregion

#region CompilationUsings

using OpaDotNet.Wasm;
using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Interop;

#endregion

#pragma warning restore CS0105

namespace Snippets;

public partial class DocSamples
{
    #region CustomBuiltinsImpl

    public class CustomBuiltinsSample : DefaultOpaImportsAbi
    {
        // Built-in with zero arguments.
        public override object? Func(BuiltinContext context)
        {
            if (string.Equals("custom.zeroArgBuiltin", context.FunctionName, StringComparison.Ordinal))
                return "hello";

            return base.Func(context);
        }

        // Built-in with one argument.
        public override object? Func(BuiltinContext context, BuiltinArg arg1)
        {
            if (string.Equals("custom.oneArgBuiltin", context.FunctionName, StringComparison.Ordinal))
                return $"hello {arg1.AsOrNull<string>()}";

            return base.Func(context, arg1);
        }

        // Built-in with two arguments.
        public override object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2)
        {
            if (string.Equals("custom.twoArgBuiltin", context.FunctionName, StringComparison.Ordinal))
                return $"hello {arg1.AsOrNull<string>()} {arg2.AsOrNull<string>()}";

            return base.Func(context, arg1, arg2);
        }

        // Built-in with three arguments.
        public override object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3)
        {
            if (string.Equals("custom.threeArgBuiltin", context.FunctionName, StringComparison.Ordinal))
                return $"hello {arg1.AsOrNull<string>()} {arg2.AsOrNull<string>()} {arg3.AsOrNull<string>()}";

            return base.Func(context, arg1, arg2, arg3);
        }

        // Built-in with four arguments.
        public override object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3, BuiltinArg arg4)
        {
            if (string.Equals("custom.fourArgBuiltin", context.FunctionName, StringComparison.Ordinal))
                return $"hello {arg1.AsOrNull<string>()} {arg2.AsOrNull<string>()} {arg3.AsOrNull<string>()} {arg4.AsOrNull<string>()}";

            return base.Func(context, arg1, arg2, arg3, arg4);
        }
    }

    #endregion

    #region CustomBuiltinsImplV26

    public class OpaCustomBuiltins : IOpaCustomBuiltins
    {
        public void Reset()
        {
        }

        // Built-in with zero arguments.
        [OpaCustomBuiltin("custom.zeroArgBuiltin")]
        public static string ZeroArgBuiltin() => "hello";

        // Built-in with one argument.
        [OpaCustomBuiltin("custom.oneArgBuiltin")]
        public static string OneArgBuiltin(string arg1) => $"hello {arg1}";

        // Built-in with two arguments.
        [OpaCustomBuiltin("custom.twoArgBuiltin")]
        public static string TwoArgBuiltin(string arg1, string arg2) => $"hello {arg1} {arg2}";

        // Built-in with three arguments.
        [OpaCustomBuiltin("custom.threeArgBuiltin")]
        public static string ThreeArgBuiltin(string arg1, string arg2, string arg3)
            => $"hello {arg1} {arg2} {arg3}";

        // Built-in with four arguments.
        [OpaCustomBuiltin("custom.fourArgBuiltin")]
        public static string FourArgBuiltin(string arg1, string arg2, string arg3, string arg4)
            => $"hello {arg1} {arg2} {arg3} {arg4}";
    }

    #endregion
    
    [Fact]
    public async Task CustomBuiltinsV26()
    {
        #region CustomBuiltinsCompileV26

        var compilationParameters = new CompilationParameters
        {
            // Custom built-ins will be merged with capabilities v0.53.1.
            CapabilitiesVersion = "v0.53.1",

            // Provide built-ins capabilities for the compiler.
            CapabilitiesFilePath = Path.Combine("builtins", "capabilities.json"),
            Entrypoints =
            [
                "custom_builtins/zero_arg",
                "custom_builtins/one_arg",
                "custom_builtins/two_arg",
                "custom_builtins/three_arg",
                "custom_builtins/four_arg",
            ],
        };

        var compiler = new RegoInteropCompiler();
        var policy = await compiler.CompileBundleAsync(
            "builtins",
            compilationParameters
            );

        var opts = new WasmPolicyEngineOptions();
        opts.ConfigureBuiltins(p => p.CustomBuiltins.Add(new OpaCustomBuiltins()));

        using var factory = new OpaBundleEvaluatorFactory(policy, opts);

        var engine = factory.Create();

        #endregion

        #region CustomBuiltinsEvalV26

        var resultZeroArg = engine.Evaluate<object, string>(new object(), "custom_builtins/zero_arg");
        Console.WriteLine(resultZeroArg.Result);

        var resultOneArg = engine.Evaluate<object, string>(
            new { args = new[] { "arg0" } },
            "custom_builtins/one_arg"
            );
        Console.WriteLine(resultOneArg.Result);

        var resultTwoArg = engine.Evaluate<object, string>(
            new { args = new[] { "arg0", "arg1" } },
            "custom_builtins/two_arg"
            );
        Console.WriteLine(resultTwoArg.Result);

        var resultThreeArg = engine.Evaluate<object, string>(
            new { args = new[] { "arg0", "arg1", "arg2" } },
            "custom_builtins/three_arg"
            );
        Console.WriteLine(resultThreeArg.Result);

        var resultFourArg = engine.Evaluate<object, string>(
            new { args = new[] { "arg0", "arg1", "arg2", "arg3" } },
            "custom_builtins/four_arg"
            );
        Console.WriteLine(resultFourArg.Result);

        #endregion

        Assert.Equal("hello", resultZeroArg.Result);
        Assert.Equal("hello arg0", resultOneArg.Result);
        Assert.Equal("hello arg0 arg1", resultTwoArg.Result);
        Assert.Equal("hello arg0 arg1 arg2", resultThreeArg.Result);
        Assert.Equal("hello arg0 arg1 arg2 arg3", resultFourArg.Result);
    }
}