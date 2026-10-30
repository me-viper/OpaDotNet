// ReSharper disable RedundantUsingDirective

#pragma warning disable CS0105

#region EvaluationUsings

using System.Text.Json;

using OpaDotNet.Wasm;

#endregion

#pragma warning restore CS0105

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Cli;

namespace Snippets;

public partial class DocSamples
{
    #region EvaluationOutput

    public record UserInfo(string Name, bool Admin);

    #endregion

    [Fact]
    public async Task EvaluationApi()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        IRegoCompiler compiler = new RegoCliCompiler();

        await using var policy = await compiler.CompileFileAsync(
            "evaluation/policy.rego",
            new() { Entrypoints = ["example/allow", "example/user"] },
            cancellationToken
            );

        var options = WasmPolicyEngineOptions.DefaultWithJsonOptions(
            p => p.PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            );

        using var engine = OpaBundleEvaluatorFactory.Create(policy, options);

        #region EvaluationSetData

        // Serialize an object with WasmPolicyEngineOptions.SerializationOptions.
        engine.SetData(new { admin = "alice" });

        // Use a JSON string as is.
        engine.SetDataFromRawJson("""{ "admin": "alice" }""");

        // Read UTF-8 JSON from a seekable stream. The evaluator doesn't dispose the stream.
        using (var data = File.OpenRead("evaluation/data.json"))
            engine.SetDataFromStream(data);

        #endregion

        #region EvaluationMethods

        var input = new { user = "alice" };

        // Boolean decision. Undefined result is reported as false.
        PolicyEvaluationResult<bool> allow = engine.EvaluatePredicate(input, "example/allow");

        // Any JSON-deserializable type. Undefined result throws OpaEvaluationException.
        PolicyEvaluationResult<UserInfo> user = engine.Evaluate<object, UserInfo>(input, "example/user");

        // Same as Evaluate, but undefined result is reported as default(TOutput).
        PolicyEvaluationResult<UserInfo?> maybeUser = engine.EvaluateOrDefault<object, UserInfo>(
            new { },
            "example/user"
            );

        // JSON in, JSON out: [{"result":true}]
        string raw = engine.EvaluateRaw("""{ "user": "alice" }""", "example/allow");

        #endregion

        Assert.True(allow.Result);
        Assert.Equal(new UserInfo("alice", true), user.Result);
        Assert.Null(maybeUser.Result);
        Assert.Equal("""[{"result":true}]""", raw);

        #region EvaluationReset

        // Remove external data set by SetData* calls.
        engine.Reset();

        #endregion

        Assert.False(engine.EvaluatePredicate(input, "example/allow").Result);
    }
}
