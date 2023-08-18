# OpaDotNet SDK Interop Compiler

Interop compiler is a wrapper over OPA SDK.

> [!IMPORTANT]
> `RegoInteropCompiler` supports only `linux-x64` and `windows-x64` platforms.

## Add usings

[!code-csharp[](~/snippets/Snippets.cs#CompilationInteropUsings)]

## Compiling single policy file

[!code-csharp[](~/snippets/Snippets.cs#CompileFileInterop)]

## Compiling bundle

Create [bundle directory](https://www.openpolicyagent.org/docs/latest/management-bundles/)

[!code-csharp[](~/snippets/Snippets.cs#CompileBundleInterop)]

## Compiling policy source

[!code-csharp[](~/snippets/Snippets.cs#CompileSourceInterop)]
