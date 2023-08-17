// ReSharper disable RedundantUsingDirective
// ReSharper disable Xunit.XunitTestWithConsoleOutput

using Microsoft.Extensions.Options;

#region Usings

using OpaDotNet.Wasm;

#endregion

#region CompilationUsings

using OpaDotNet.Wasm;
using OpaDotNet.Compilation.Cli;

#endregion

namespace OpaDotNet.Tests.Snippets;

public partial class Snippets
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

    [Fact(Skip = "Documentation sample")]
    public async Task CustomBuiltins()
    {
        #region CustomBuiltinsCompile

        var opts = new RegoCliCompilerOptions
        {
            // Custom built-ins will be merged with capabilities v0.53.1.
            CapabilitiesVersion = "v0.53.1",
        };

        var compiler = new RegoCliCompiler(new OptionsWrapper<RegoCliCompilerOptions>(opts));
        var policy = await compiler.CompileBundle(
            "./bundle",
            new[]
            {
                "custom_builtins/zero_arg",
                "custom_builtins/one_arg",
                "custom_builtins/two_arg",
                "custom_builtins/three_arg",
                "custom_builtins/four_arg",
            },
            Path.Combine("./bundle", "capabilities.json")
            );

        var factory = new OpaBundleEvaluatorFactory(
            policy,
            importsAbiFactory: () => new CustomBuiltinsSample()
            );

        var engine = factory.Create();

        #endregion

        #region CustomBuiltinsEval

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
    }
}