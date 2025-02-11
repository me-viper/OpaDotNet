using Microsoft.CodeAnalysis;

namespace OpaDotNet.Wasm.Tests.SdkV1;

[Generator]
public class TestCaseGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(
            static p =>
            {
                //p.AddEmbeddedAttributeDefinition();
            }
            );
    }
}