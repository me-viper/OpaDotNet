#!/usr/bin/env dotnet

// Runs OPA policy tests (rules prefixed with `test_`) by compiling the source path as a bundle to wasm
// and evaluating each test rule with OpaDotNet.Wasm.
//
// Usage:
//   dotnet run build/opa-test.cs -- <path> [options]
//
// Options:
//   -r, --run <regex>      Only run tests whose full name (data.pkg.test_x) matches the regex.
//   -i, --ignore <glob>    File or directory names to ignore when loading the bundle (repeatable).
//   -v, --verbose          Print every test result, not just failures.
//       --v0               Compile with Rego v0 syntax (default: v1).
//       --opa <path>       Path to the opa CLI used for compilation (default: opa on PATH).
//
// Exit code: 0 when all tests pass, 1 when any test fails or errors, 2 on usage/compilation errors.

#:property TargetFrameworks=net10.0
#:property PublishAot=false
#:project ../src/Wasm/src/OpaDotNet.Wasm.csproj
#:project ../src/Compilation/Cli/src/OpaDotNet.Compilation.Cli.csproj

using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Cli;
using OpaDotNet.Wasm;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, p) =>
{
    p.Cancel = true;
    cts.Cancel();
};

Options opts;

try
{
    opts = Options.Parse(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine($"error: {ex.Message}");
    Console.Error.WriteLine(Options.Usage);
    return 2;
}

try
{
    return await TestRunner.RunAsync(opts, cts.Token);
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Cancelled");
    return 2;
}

internal sealed record Options
{
    public const string Usage = "usage: dotnet run build/opa-test.cs -- <path> [-r <regex>] [-i <glob>]... [-v] [--v0] [--opa <path>]";

    public required string Path { get; init; }

    public Regex? Run { get; init; }

    public HashSet<string> Ignore { get; } = [];

    public bool Verbose { get; init; }

    public RegoVersion RegoVersion { get; init; } = RegoVersion.V1;

    public string? OpaPath { get; init; }

    public static Options Parse(string[] args)
    {
        string? path = null;
        Regex? run = null;
        var verbose = false;
        var regoVersion = RegoVersion.V1;
        string? opaPath = null;
        var ignore = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length
                ? args[++i]
                : throw new ArgumentException($"Option {args[i]} requires a value");

            switch (args[i])
            {
                case "-r" or "--run":
                    run = new Regex(Next(), RegexOptions.CultureInvariant);
                    break;
                case "-i" or "--ignore":
                    ignore.Add(Next());
                    break;
                case "-v" or "--verbose":
                    verbose = true;
                    break;
                case "--v0":
                    regoVersion = RegoVersion.V0;
                    break;
                case "--opa":
                    opaPath = Next();
                    break;
                case "-h" or "--help":
                    throw new ArgumentException("help requested");
                default:
                    if (args[i].StartsWith('-'))
                        throw new ArgumentException($"Unknown option {args[i]}");

                    if (path != null)
                        throw new ArgumentException("Only one path is supported");

                    path = args[i];
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required");

        var result = new Options
        {
            Path = System.IO.Path.GetFullPath(path),
            Run = run,
            Verbose = verbose,
            RegoVersion = regoVersion,
            OpaPath = opaPath,
        };

        result.Ignore.UnionWith(ignore);
        return result;
    }
}

internal sealed record TestCase(string Package, string Rule, string File, int Line)
{
    public string Name => $"data.{Package}.{Rule}";

    public string Entrypoint => $"{Package.Replace('.', '/')}/{Rule}";

    public bool IsTodo => Rule.StartsWith("todo_", StringComparison.Ordinal);
}

internal enum TestOutcome
{
    Pass,
    Fail,
    Error,
    Skip,
}

internal static partial class TestRunner
{
    public static async Task<int> RunAsync(Options opts, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(opts.Path))
        {
            Console.Error.WriteLine($"error: bundle directory {opts.Path} was not found");
            return 2;
        }

        var tests = DiscoverTests(opts)
            .Where(p => opts.Run == null || opts.Run.IsMatch(p.Name))
            .ToList();

        if (tests.Count == 0)
        {
            Console.Error.WriteLine("error: no test cases found");
            return 2;
        }

        // Multiple definitions of the same test rule are merged by the compiler into a single
        // entrypoint that succeeds when any of them succeeds, so failures in one of them are masked.
        foreach (var dup in tests.GroupBy(p => p.Name).Where(p => p.Count() > 1))
        {
            Console.Error.WriteLine(
                $"warning: {dup.Key} is defined {dup.Count()} times; definitions are evaluated as one rule " +
                $"({string.Join(", ", dup.Select(p => $"{p.File}:{p.Line}"))})"
                );
        }

        var distinct = tests.DistinctBy(p => p.Name).ToList();
        var runnable = distinct.Where(p => !p.IsTodo).ToList();

        Stream bundle;

        try
        {
            var compiler = new RegoCliCompiler(options: new RegoCliCompilerOptions { OpaToolPath = opts.OpaPath });
            var parameters = new CompilationParameters
            {
                IsBundle = true,
                Entrypoints = runnable.Select(p => p.Entrypoint).ToList(),
                RegoVersion = opts.RegoVersion,
                Ignore = opts.Ignore,
            };

            bundle = await compiler.CompileBundleAsync(opts.Path, parameters, cancellationToken);
        }
        catch (RegoCompilationException ex)
        {
            Console.Error.WriteLine($"error: compilation failed: {ex.Message}");
            return 2;
        }

        await using (bundle)
        {
            using var factory = new OpaBundleEvaluatorFactory(bundle);
            using var evaluator = factory.Create();

            var counts = new Dictionary<TestOutcome, int>();
            var total = Stopwatch.StartNew();

            foreach (var test in distinct)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sw = Stopwatch.StartNew();
                var (outcome, message) = test.IsTodo ? (TestOutcome.Skip, null) : Evaluate(evaluator, test);
                sw.Stop();

                // Wasm traps carry a multi-line backtrace; keep only the summary unless asked for details.
                if (!opts.Verbose && message != null)
                    message = message.Split('\n', 2)[0].TrimEnd();

                counts[outcome] = counts.GetValueOrDefault(outcome) + 1;

                if (opts.Verbose || outcome is TestOutcome.Fail or TestOutcome.Error)
                    Report(test, outcome, message, sw.Elapsed);
            }

            total.Stop();
            Console.WriteLine(new string('-', 80));

            var failed = counts.GetValueOrDefault(TestOutcome.Fail) + counts.GetValueOrDefault(TestOutcome.Error);

            foreach (var (outcome, count) in counts.OrderBy(p => p.Key))
                Console.WriteLine($"{outcome.ToString().ToUpperInvariant()}: {count}/{distinct.Count}");

            Console.WriteLine(
                string.Create(CultureInfo.InvariantCulture, $"Duration: {total.Elapsed.TotalMilliseconds:F1}ms")
                );

            return failed > 0 ? 1 : 0;
        }
    }

    private static (TestOutcome Outcome, string? Message) Evaluate(IOpaEvaluator evaluator, TestCase test)
    {
        try
        {
            // Empty input leaves `input` undefined, same as `opa test`.
            var raw = evaluator.EvaluateRaw(ReadOnlySpan<char>.Empty, test.Entrypoint);

            using var doc = JsonDocument.Parse(raw);
            var results = doc.RootElement;

            if (results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0)
                return (TestOutcome.Fail, "result is undefined");

            if (!results[0].TryGetProperty("result", out var result))
                return (TestOutcome.Fail, "result is undefined");

            return result.ValueKind == JsonValueKind.True
                ? (TestOutcome.Pass, null)
                : (TestOutcome.Fail, $"result is {result.GetRawText()}");
        }
        catch (OpaRuntimeException ex)
        {
            return (TestOutcome.Error, ex.InnerException == null ? ex.Message : $"{ex.Message}: {ex.InnerException.Message}");
        }
    }

    private static void Report(TestCase test, TestOutcome outcome, string? message, TimeSpan elapsed)
    {
        var color = outcome switch
        {
            TestOutcome.Pass => ConsoleColor.Green,
            TestOutcome.Skip => ConsoleColor.Yellow,
            _ => ConsoleColor.Red,
        };

        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(outcome.ToString().ToUpperInvariant());
        Console.ForegroundColor = prev;

        Console.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $": {test.Name} ({elapsed.TotalMilliseconds:F1}ms) [{test.File}:{test.Line}]"
                )
            );

        if (!string.IsNullOrWhiteSpace(message))
            Console.WriteLine($"  {message}");
    }

    private static IEnumerable<TestCase> DiscoverTests(Options opts)
    {
        var files = Directory.EnumerateFiles(opts.Path, "*.rego", SearchOption.AllDirectories)
            .Where(p => !IsIgnored(opts.Path, p, opts.Ignore))
            .Order(StringComparer.Ordinal);

        foreach (var file in files)
        {
            string? package = null;
            var lines = File.ReadAllLines(file);
            var relPath = Path.GetRelativePath(opts.Path, file);

            for (var i = 0; i < lines.Length; i++)
            {
                var pkg = PackageRegex().Match(lines[i]);

                if (pkg.Success)
                {
                    package = NormalizePackage(pkg.Groups["pkg"].Value);
                    continue;
                }

                if (package == null)
                    continue;

                var rule = TestRuleRegex().Match(lines[i]);

                if (rule.Success)
                    yield return new TestCase(package, rule.Groups["rule"].Value, relPath, i + 1);
            }
        }
    }

    private static bool IsIgnored(string root, string file, IReadOnlySet<string> ignore)
    {
        if (ignore.Count == 0)
            return false;

        var segments = Path.GetRelativePath(root, file).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(s => ignore.Any(g => GlobToRegex(g).IsMatch(s)));
    }

    private static Regex GlobToRegex(string glob) => new(
        "^" + Regex.Escape(glob).Replace(@"\*", ".*", StringComparison.Ordinal).Replace(@"\?", ".", StringComparison.Ordinal) + "$",
        RegexOptions.CultureInvariant
        );

    // package a["b"].c -> a.b.c
    private static string NormalizePackage(string pkg) => QuotedRefRegex().Replace(pkg, ".${seg}").Trim('.');

    [GeneratedRegex("""^package\s+(?<pkg>[\w.\["\]-]+)""", RegexOptions.CultureInvariant)]
    private static partial Regex PackageRegex();

    [GeneratedRegex("""^(?<rule>(?:todo_)?test_\w*)\s*(?:if\b|\{|:?=|$)""", RegexOptions.CultureInvariant)]
    private static partial Regex TestRuleRegex();

    [GeneratedRegex("""\["(?<seg>[^"]+)"\]""", RegexOptions.CultureInvariant)]
    private static partial Regex QuotedRefRegex();
}
