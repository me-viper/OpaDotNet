using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace OpaDotNet.Wasm.Generators;

[Generator]
public class TestCaseGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var files = context.AdditionalTextsProvider
            .Where(static p => p.Path.EndsWith(".yaml"));

        var opts = context.AnalyzerConfigOptionsProvider
            .Select(static (_, _) => new TestCaseFilter());

        var pipe = files.Combine(opts)
            .Select(
                (p, _) =>
                {
                    var file = p.Left;
                    var filter = p.Right;

                    try
                    {
                        return TestCaseParser.ParseFile(file, filter);
                    }
                    catch (Exception ex)
                    {
                        var failed = new SdkV1TestCaseContainer { Name = file.Path, FileName = file.Path };
                        failed.Diagnostics.Add(Diagnostic.Create(Helpers.FailedToParseTestCaseFile, Location.None, file.Path, ex.Message));
                        return failed;
                    }
                }
                )
            .WithComparer(SdkV1TestCaseContainerEqualityComparer.Instance)
            .Where(p => p != null);

        var dgs = pipe.Where(p => p!.Diagnostics.Count > 0);

        context.RegisterSourceOutput(
            dgs,
            static (context, p) =>
            {
                foreach (var d in p!.Diagnostics)
                    context.ReportDiagnostic(d);
            }
            );

        context.RegisterSourceOutput(
            pipe,
            static (context, p) =>
            {
                if (p!.Cases.Count > 0)
                {
                    var src = TestCaseWriter.WriteTestCases(p.Cases, p.FileName);
                    context.AddSource($"{p.Name}.g.cs", SourceText.From(src, Encoding.UTF8));
                }
            }
            );
    }
}