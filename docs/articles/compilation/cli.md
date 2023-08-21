# OpaDotNet Cli Compiler

Cli compiler is simple wrapper over `opa` CLI tool.

> [!IMPORTANT]
> Internally `RegoCliCompiler` uses `opa` CLI so you need to ensure it is in your `PATH` environment variable or you provided full path in [RegoCliCompilerOptions.OpaToolPath](xref:OpaDotNet.Compilation.Cli.RegoCliCompilerOptions#OpaDotNet_Compilation_Cli_RegoCliCompilerOptions_OpaToolPath).

> [!NOTE]
> `RegoCliCompiler` requires write access to the file system to store intermediate artifacts and outputs. By default it will use parent directory if source is file (`/data/policy.rego => /data`) or bundle directory if source is bundle (`/data/policy` => `/data/policy`). You can override artifacts directory with [RegoCompilerOptions.OutputPath](xref:OpaDotNet.Compilation.Abstractions.RegoCompilerOptions#OpaDotNet_Compilation_Abstractions_RegoCompilerOptions_OutputPath) property.

## Add usings

[!code-csharp[](~/snippets/Snippets.cs#CompilationCliUsings)]

## Compiling single policy file

[!code-csharp[](~/snippets/Snippets.cs#CompileFileCli)]

## Compiling bundle

Create [bundle directory](https://www.openpolicyagent.org/docs/latest/management-bundles/)

[!code-csharp[](~/snippets/Snippets.cs#CompileBundleCli)]

## Compiling policy source

[!code-csharp[](~/snippets/Snippets.cs#CompileSourceCli)]
