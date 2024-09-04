namespace OpaDotNet.Compilation.Tests.Common;

internal static class TestHelpers
{
    public const string SimplePolicySource = """
        package example
        default allow := false
        """;

    public static string PolicySource(string package, string rule) => $$"""
        package {{package}}
        default {{rule}} := false
        """;

    public static readonly string[] SimplePolicyEntrypoints = ["example/allow"];
}