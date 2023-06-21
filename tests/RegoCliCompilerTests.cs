using Microsoft.Extensions.Options;

using OpaDotNet.Wasm.Compilation;

namespace OpaDotNet.Tests;

public class RegoCliCompilerTests
{
    [Fact]
    public async Task OpaCliNotFound()
    {
        var opts = new RegoCliCompilerOptions
        {
            OpaToolPath = "./somewhere",
        };
        
        var compiler = new RegoCliCompiler(new OptionsWrapper<RegoCliCompilerOptions>(opts));
        
        var ex = await Assert.ThrowsAsync<RegoCompilationException>(
            () => compiler.CompileFile("fail.rego")
            );
        
        Assert.Equal("fail.rego", ex.SourceFile);
    }
}