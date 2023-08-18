# OpaDotNet Cli Compiler

Cli compiler is simple wrapper over `opa` CLI, b

> [!IMPORTANT]
> Internally `RegoCliCompiler` uses `opa` CLI so you need to ensure it is in your `PATH` environment variable or you provided full path in `RegoCliCompilerOptions.OpaToolPath`.

## Add usings

[!code-csharp[](~/snippets/Snippets.cs#CompilationCliUsings)]

## Compiling single policy file

[!code-csharp[](~/snippets/Snippets.cs#CompileFileCli)]

## Compiling bundle

Create [bundle directory](https://www.openpolicyagent.org/docs/latest/management-bundles/)

[!code-csharp[](~/snippets/Snippets.cs#CompileBundleCli)]

## Compiling policy source

[!code-csharp[](~/snippets/Snippets.cs#CompileSourceCli)]
