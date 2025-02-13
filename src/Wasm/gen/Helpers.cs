using Microsoft.CodeAnalysis;

namespace OpaDotNet.Wasm.Generators;

internal static class Helpers
{
    public static readonly DiagnosticDescriptor FailedToParseTestCaseFile = new(
        id: "OPATCGEN001",
        title: "Failed to parse test case file",
        messageFormat: "Failed to parse {0} file: {1}",
        category: "OpaDotNetTestCaseGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
        );
}