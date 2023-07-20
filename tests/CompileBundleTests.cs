using Microsoft.Extensions.Options;

using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Compilation;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

public class CompileBundleTests
{
    private readonly ILoggerFactory _loggerFactory;

    public CompileBundleTests(ITestOutputHelper output)
    {
        _loggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
    }

    [Fact]
    public async Task Compile()
    {
        var compiler = new RegoCliCompiler(
            new OptionsWrapper<RegoCliCompilerOptions>(new() { ExtraArguments = "--debug" }),
            _loggerFactory.CreateLogger<RegoCliCompiler>()
            );

        var policy = await compiler.CompileBundle(
            Path.Combine("TestData", "compile-bundle", "example"),
            new[] { "test1/hello", "test2/hello" }
            );

        var evaluator = OpaEvaluatorFactory.CreateFromBundle(policy);

        var input1 = new { message = "world" };
        var test1Result = evaluator.EvaluatePredicate(input1, "test1/hello");

        Assert.True(test1Result.Result);

        var input2 = new { message = "world1" };
        var test2Result = evaluator.EvaluatePredicate(input2, "test2/hello");

        Assert.True(test2Result.Result);
    }

    [Fact]
    public Task FileBundle()
    {
        var path = Path.Combine("TestData", "compile-bundle", "bundle.tar.gz");

        var evaluator = OpaEvaluatorFactory.CreateFromBundle(File.OpenRead(path));

        var input1 = new { message = "world" };
        var test1Result = evaluator.EvaluatePredicate(input1, "test1/hello");

        Assert.True(test1Result.Result);

        var input2 = new { message = "world1" };
        var test2Result = evaluator.EvaluatePredicate(input2, "test2/hello");

        Assert.True(test2Result.Result);
        return Task.CompletedTask;
    }
}