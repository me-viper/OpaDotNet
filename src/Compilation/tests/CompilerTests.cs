using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Tests.Common;
using OpaDotNet.InternalTesting;

namespace OpaDotNet.Compilation.Tests;

[Collection("Compilation")]
public abstract class CompilerTests<T>
    where T : IRegoCompiler
{
    protected readonly ILoggerFactory LoggerFactory;

    protected readonly ITestOutputHelper OutputHelper;

    protected CompilerTests(ITestOutputHelper output)
    {
        OutputHelper = output;
        LoggerFactory = new LoggerFactory([new XunitLoggerProvider(output)]);
    }

    protected abstract string BaseOutputPath { get; }

    protected string DefaultCaps => Utils.DefaultCapabilities;

    protected abstract T CreateCompiler(ILoggerFactory? loggerFactory = null);

    [Theory]
    [InlineData("ex/test")]
    [InlineData(null)]
    [InlineData(null, "./TestData/policy.rego")]
    [InlineData(null, "TestData/policy.rego")]
    [InlineData(null, ".\\TestData\\policy.rego")]
    [InlineData(null, "TestData\\policy.rego")]
    [InlineData(null, "~TestData\\policy.rego")]
    public async Task CompileFile(string? entrypoint, string? path = null)
    {
        IReadOnlyList<string>? eps = string.IsNullOrWhiteSpace(entrypoint) ? null : [entrypoint];
        var compiler = CreateCompiler(LoggerFactory);

        path ??= Path.Combine("TestData", "policy.rego");

        if (path.StartsWith("~"))
            path = Path.Combine(AppContext.BaseDirectory, path[1..]);

        var opts = new CompilationParameters
        {
            Debug = true,
            Entrypoints = eps,
        };

        await using var policy = await compiler.CompileFileAsync(path, opts, TestContext.Current.CancellationToken);

        AssertBundle.DumpBundle(policy, OutputHelper);

        AssertBundle.IsValid(policy);
    }

    [Theory]
    [InlineData("test1/hello")]
    [InlineData("test2/hello")]
    [InlineData("test1/hello", "./TestData/compile-bundle/example")]
    [InlineData("test1/hello", "TestData/compile-bundle/example")]
    [InlineData("test1/hello", ".\\TestData\\compile-bundle\\example")]
    [InlineData("test1/hello", "TestData\\compile-bundle\\example")]
    [InlineData("test1/hello", "~TestData\\compile-bundle\\example")]
    public async Task CompileBundle(string? entrypoint, string? path = null)
    {
        IReadOnlyList<string>? eps = string.IsNullOrWhiteSpace(entrypoint) ? null : [entrypoint];
        var compiler = CreateCompiler(LoggerFactory);

        path ??= Path.Combine("TestData", "compile-bundle", "example");

        if (path.StartsWith("~"))
            path = Path.Combine(AppContext.BaseDirectory, path[1..]);

        await using var policy = await compiler.CompileBundleAsync(path, new() { Entrypoints = eps, Debug = true }, TestContext.Current.CancellationToken);

        AssertBundle.DumpBundle(policy, OutputHelper);

        var bundle = TarGzHelper.ReadBundle(policy);

        Assert.True(bundle.Policy.Length > 0);
        Assert.True(bundle.Data.Length > 0);

        var data = JsonDocument.Parse(bundle.Data);

        Assert.Equal("root", data.RootElement.GetProperty("root").GetProperty("world").GetString());
        Assert.Equal("world", data.RootElement.GetProperty("test1").GetProperty("world").GetString());
        Assert.Equal("world1", data.RootElement.GetProperty("test2").GetProperty("world").GetString());
    }

    [Theory]
    [InlineData("test1/hello")]
    [InlineData("test2/hello")]
    [InlineData("test1/hello", "./TestData/src.bundle.tar.gz")]
    public async Task CompileBundleFromBundle(string? entrypoint, string? path = null)
    {
        IReadOnlyList<string>? eps = string.IsNullOrWhiteSpace(entrypoint) ? null : [entrypoint];
        var compiler = CreateCompiler(LoggerFactory);

        path ??= Path.Combine("TestData", "src.bundle.tar.gz");
        var policy = await compiler.CompileBundleAsync(path, new() { Entrypoints = eps }, TestContext.Current.CancellationToken);

        AssertBundle.IsValid(policy);
    }

    [Fact]
    public async Task Version()
    {
        var compiler = CreateCompiler();
        var v = await compiler.Version(TestContext.Current.CancellationToken);

        Assert.NotNull(v.Version);
        Assert.NotNull(v.GoVersion);
        Assert.NotNull(v.Platform);

        OutputHelper.WriteLine(v.ToString());
    }

    [Fact]
    public async Task FailCompilation()
    {
        var compiler = CreateCompiler(LoggerFactory);
        var ex = await Assert.ThrowsAsync<RegoCompilationException>(
            () => compiler.CompileSourceAsync("bad rego", new() { Entrypoints = ["ep"] }, TestContext.Current.CancellationToken)
            );

        Assert.Contains("rego_parse_error: package expected", ex.Message);
    }

    [Fact]
    public async Task FailCapabilities()
    {
        var compiler = CreateCompiler(LoggerFactory);

        _ = await Assert.ThrowsAsync<RegoCompilationException>(
            () => compiler.CompileBundleAsync(
                Path.Combine("TestData", "capabilities"),
                new()
                {
                    Entrypoints = ["capabilities/f"],
                    CapabilitiesFilePath = Path.Combine("TestData", "capabilities", "capabilities.json"),
                },
                TestContext.Current.CancellationToken
                )
            );
    }

    [Fact]
    public async Task SetCapabilitiesBundle()
    {
        var compiler = CreateCompiler(LoggerFactory);

        await using var policy = await compiler.CompileBundleAsync(
            Path.Combine("TestData", "compile-bundle", "example"),
            new()
            {
                Entrypoints = ["test1/hello", "test2/hello"],
                CapabilitiesVersion = DefaultCaps,
            },
            TestContext.Current.CancellationToken
            );

        Assert.NotNull(policy);
    }

    [Fact]
    public async Task SetCapabilitiesSource()
    {
        var compiler = CreateCompiler(LoggerFactory);

        await using var policy = await compiler.CompileSourceAsync(
            TestHelpers.SimplePolicySource,
            new()
            {
                Entrypoints = TestHelpers.SimplePolicyEntrypoints,
                CapabilitiesVersion = DefaultCaps,
            },
            TestContext.Current.CancellationToken
            );

        Assert.NotNull(policy);
    }

    [Fact]
    public async Task MergeCapabilities()
    {
        var compiler = CreateCompiler(LoggerFactory);

        await using var policy = await compiler.CompileBundleAsync(
            Path.Combine("TestData", "capabilities"),
            new()
            {
                Entrypoints = ["capabilities/f"],
                CapabilitiesFilePath = Path.Combine("TestData", "capabilities", "capabilities.json"),
                CapabilitiesVersion = DefaultCaps,
            },
            TestContext.Current.CancellationToken
            );

        AssertBundle.IsValid(policy);
    }

    [Fact]
    public async Task BundleWriterMergeCapabilities()
    {
        using var bundle = new MemoryStream();

        await using (var bw = new BundleWriter(bundle))
        {
            var rego = await File.ReadAllBytesAsync(Path.Combine("TestData", "capabilities", "capabilities.rego"), TestContext.Current.CancellationToken);
            bw.WriteEntry(rego, "capabilities.rego");
        }

        bundle.Seek(0, SeekOrigin.Begin);
        var capsBytes = await File.ReadAllBytesAsync(Path.Combine("TestData", "capabilities", "capabilities.json"), TestContext.Current.CancellationToken);

        var compiler = CreateCompiler(LoggerFactory);

        await using var policy = await compiler.CompileBundleAsync(
            bundle,
            new()
            {
                Entrypoints = ["capabilities/f"],
                CapabilitiesBytes = capsBytes,
                CapabilitiesVersion = DefaultCaps,
                Debug = true,
            },
            TestContext.Current.CancellationToken
            );

        AssertBundle.DumpBundle(policy, OutputHelper);

        AssertBundle.Content(
            policy,
            p => AssertBundle.HasEntry(p, "/policy.wasm")
            );
    }

    [Fact]
    public async Task BundleWriterFile()
    {
        using var ms = new MemoryStream();

        var manifest = new BundleManifest
        {
            Revision = "test-2",
            Metadata = { { "source", "test" } },
        };

        await using (var bw = new BundleWriter(ms, manifest))
        {
            using var inStream = new MemoryStream();
            inStream.Write(Encoding.UTF8.GetBytes(TestHelpers.PolicySource("p2", "p2r")));
            inStream.Seek(0, SeekOrigin.Begin);

            bw.WriteEntry(TestHelpers.SimplePolicySource, "p1.rego");
            bw.WriteEntry(inStream, "/tests/p2.rego");
            bw.WriteEntry("{}"u8, "/tests/data.json");
            bw.WriteEntry("{}"u8, @"c:\a\data.json");
        }

        ms.Seek(0, SeekOrigin.Begin);

        var outPath = Path.Combine(BaseOutputPath, "tmp");

        var tmpDir = new DirectoryInfo(outPath);

        if (!tmpDir.Exists)
            tmpDir.Create();

        var compiler = CreateCompiler(LoggerFactory);
        using var bundle = await compiler.CompileBundleAsync(
            ms,
            new()
            {
                Entrypoints = TestHelpers.SimplePolicyEntrypoints,
                PruneUnused = true,
                Debug = true,
                OutputPath = outPath,
            },
            TestContext.Current.CancellationToken
            );

        AssertBundle.DumpBundle(bundle, OutputHelper);

        AssertBundle.Content(
            bundle,
            p => AssertBundle.HasEntry(p, "/policy.wasm"),
            p => AssertBundle.HasEntry(p, "/.manifest"),
            AssertBundle.HasNonEmptyData
            );
    }

    [Fact]
    public async Task MetadataEntrypoints()
    {
        using var ms = new MemoryStream();

        await using (var bw = new BundleWriter(ms))
        {
            var policy = """
                # METADATA
                # entrypoint: true
                package test.ep
                default allow := true
                """;
            bw.WriteEntry(policy, "p1.rego");
        }

        ms.Seek(0, SeekOrigin.Begin);

        var compiler = CreateCompiler(LoggerFactory);
        using var bundle = await compiler.CompileBundleAsync(
            ms,
            new()
            {
                PruneUnused = true,
                Debug = true,
                OutputPath = Path.Combine(BaseOutputPath, "./tmp-cleanup", Path.GetRandomFileName()),
            },
            TestContext.Current.CancellationToken
            );

        AssertBundle.DumpBundle(bundle, OutputHelper);
    }

    [Fact]
    public async Task EnsureCleanup()
    {
        using var ms = new MemoryStream();

        await using (var bw = new BundleWriter(ms))
        {
            using var inStream = new MemoryStream();
            inStream.Write(Encoding.UTF8.GetBytes(TestHelpers.PolicySource("p2", "p2r")));
            inStream.Seek(0, SeekOrigin.Begin);

            bw.WriteEntry(TestHelpers.SimplePolicySource, "p1.rego");
            bw.WriteEntry(inStream, "/tests/p2.rego");
            bw.WriteEntry("{}"u8, "/tests/data.json");
            bw.WriteEntry("{}"u8, @"c:\a\data.json");
        }

        ms.Seek(0, SeekOrigin.Begin);

        var outPath = Path.Combine(BaseOutputPath, "./tmp-cleanup", Path.GetRandomFileName());

        var tmpDir = new DirectoryInfo(outPath);

        if (tmpDir.Exists)
            tmpDir.Delete(true);

        tmpDir.Create();

        var compiler = CreateCompiler(LoggerFactory);

        await using var bundle = await compiler.CompileBundleAsync(
            ms,
            new()
            {
                Entrypoints = TestHelpers.SimplePolicyEntrypoints,
                PruneUnused = true,
                Debug = true,
                OutputPath = outPath,
            },
            TestContext.Current.CancellationToken
            );

        await bundle.DisposeAsync();

        var filesCount = tmpDir.EnumerateFiles().Count();
        Assert.Equal(0, filesCount);
    }

    [Fact]
    public async Task EnsureCleanupOnError()
    {
        using var ms = new MemoryStream();

        await using (var bw = new BundleWriter(ms))
        {
            using var inStream = new MemoryStream();
            inStream.Write("bad policy"u8);
            inStream.Seek(0, SeekOrigin.Begin);

            bw.WriteEntry(inStream, "/tests/p2.rego");
        }

        ms.Seek(0, SeekOrigin.Begin);

        var outPath = Path.Combine(BaseOutputPath, "./tmp-cleanup-fail", Path.GetRandomFileName());

        var tmpDir = new DirectoryInfo(outPath);

        if (tmpDir.Exists)
            tmpDir.Delete(true);

        tmpDir.Create();

        var compiler = CreateCompiler(LoggerFactory);
        await Assert.ThrowsAsync<RegoCompilationException>(
            () => compiler.CompileBundleAsync(
                ms,
                new()
                {
                    Entrypoints = TestHelpers.SimplePolicyEntrypoints,
                    PruneUnused = true,
                    Debug = true,
                    OutputPath = outPath,
                },
                TestContext.Current.CancellationToken
                )
            );

        var filesCount = tmpDir.EnumerateFiles().Count();
        Assert.Equal(0, filesCount);
    }

    [Fact]
    public async Task FailMultiCapabilities()
    {
        var compiler = CreateCompiler(LoggerFactory);

        _ = await Assert.ThrowsAsync<RegoCompilationException>(
            () => compiler.CompileBundleAsync(
                Path.Combine("TestData", "multi-caps"),
                new()
                {
                    Entrypoints = ["capabilities/f", "capabilities/f2"],
                    CapabilitiesFilePath = Path.Combine("TestData", "multi-caps", "caps1.json"),
                    CapabilitiesVersion = DefaultCaps,
                },
                TestContext.Current.CancellationToken
                )
            );
    }

    [Fact]
    public async Task MergeMultiCapabilities()
    {
        var outPath = Path.Combine(BaseOutputPath, "tmp-multi-caps");

        var tmpDir = new DirectoryInfo(outPath);

        if (tmpDir.Exists)
            tmpDir.Delete(true);

        tmpDir.Create();

        await using var caps1Fs = File.OpenRead(Path.Combine("TestData", "multi-caps", "caps1.json"));
        await using var caps2Fs = File.OpenRead(Path.Combine("TestData", "multi-caps", "caps2.json"));
        await using var capsFs = BundleWriter.MergeCapabilities(caps1Fs, caps2Fs);

        var tmpCapsFile = Path.Combine(tmpDir.FullName, "caps.json");

        await using (var fs = new FileStream(tmpCapsFile, FileMode.CreateNew))
        {
            await capsFs.CopyToAsync(fs, TestContext.Current.CancellationToken);
        }

        var compiler = CreateCompiler(LoggerFactory);

        await using var policy = await compiler.CompileBundleAsync(
            Path.Combine("TestData", "multi-caps"),
            new()
            {
                Entrypoints = ["capabilities/f", "capabilities/f2"],
                CapabilitiesFilePath = tmpCapsFile,
                CapabilitiesVersion = DefaultCaps,
                OutputPath = outPath,
            },
            TestContext.Current.CancellationToken
            );

        AssertBundle.IsValid(policy);
    }

    [Fact]
    public async Task BundleWriterMergeMultiCapabilities()
    {
        using var bundle = new MemoryStream();

        await using (var bw = new BundleWriter(bundle))
        {
            var rego = await File.ReadAllBytesAsync(Path.Combine("TestData", "multi-caps", "capabilities.rego"), TestContext.Current.CancellationToken);
            bw.WriteEntry(rego, "capabilities.rego");
        }

        bundle.Seek(0, SeekOrigin.Begin);
        await using var caps1Fs = File.OpenRead(Path.Combine("TestData", "multi-caps", "caps1.json"));
        await using var caps2Fs = File.OpenRead(Path.Combine("TestData", "multi-caps", "caps2.json"));
        await using var capsFs = BundleWriter.MergeCapabilities(caps1Fs, caps2Fs);

        Memory<byte> capsMem = new byte[capsFs.Length];
        _ = await capsFs.ReadAsync(capsMem, TestContext.Current.CancellationToken);

        var compiler = CreateCompiler(LoggerFactory);

        await using var policy = await compiler.CompileBundleAsync(
            bundle,
            new()
            {
                Entrypoints = ["capabilities/f", "capabilities/f2"],
                CapabilitiesBytes = capsMem,
                CapabilitiesVersion = DefaultCaps,
                Debug = true,
            },
            TestContext.Current.CancellationToken
            );

        AssertBundle.DumpBundle(policy, OutputHelper);

        AssertBundle.Content(
            policy,
            p => AssertBundle.HasEntry(p, "/policy.wasm")
            );
    }

    [Theory]
    [InlineData("TestData/compile-bundle/example", new[] { "test1/hello" }, null)]
    [InlineData("TestData/compile-bundle/example", new[] { "test1/hello" }, new[] { "test1" })]
    [InlineData("TestData/compile-bundle/example", new[] { "test1/hello" }, new[] { "test1", "test2" })]
    public async Task Ignore(string path, string[] entrypoints, string[]? exclusions)
    {
        var compiler = CreateCompiler(LoggerFactory);

        await using var bundle = await compiler.CompileBundleAsync(
            path,
            new()
            {
                Entrypoints = entrypoints,
                CapabilitiesVersion = DefaultCaps,
                Ignore = exclusions?.ToHashSet() ?? new HashSet<string>(),
            },
            TestContext.Current.CancellationToken
            );

        AssertBundle.DumpBundle(bundle, OutputHelper);

        AssertBundle.Content(
            bundle,
            p => AssertBundle.HasEntry(p, "/policy.wasm"),
            p => AssertBundle.AssertData(
                p,
                pp =>
                {
                    OutputHelper.WriteLine(pp.RootElement.GetRawText());

                    foreach (var excl in exclusions ?? [])
                    {
                        if (pp.RootElement.TryGetProperty(excl, out _))
                            Assert.Fail($"data.json contains excluded element {excl}");
                    }

                    return true;
                }
                )
            );
    }

    [Theory]
    [InlineData("TestData/compile-bundle/example", new[] { "test1/hello" }, null)]
    [InlineData("TestData/compile-bundle/example", new[] { "test1/hello" }, new[] { "test1" })]
    [InlineData("TestData/compile-bundle/example", new[] { "test1/hello" }, new[] { "test1", "test2" })]
    public async Task FromDirectory(string path, string[] entrypoints, string[]? exclusions)
    {
        using var ms = new MemoryStream();

        var bundleWriter = BundleWriter.FromDirectory(ms, path, exclusions?.ToHashSet());

        Assert.False(bundleWriter.IsEmpty);

        await bundleWriter.DisposeAsync();
        ms.Seek(0, SeekOrigin.Begin);

        var compiler = CreateCompiler(LoggerFactory);

        await using var bundle = await compiler.CompileBundleAsync(
            ms,
            new()
            {
                Entrypoints = entrypoints,
                CapabilitiesVersion = DefaultCaps,
            },
            TestContext.Current.CancellationToken
            );

        AssertBundle.DumpBundle(bundle, OutputHelper);

        AssertBundle.Content(
            bundle,
            p => AssertBundle.HasEntry(p, "/policy.wasm"),
            p => AssertBundle.AssertData(
                p,
                pp =>
                {
                    OutputHelper.WriteLine(pp.RootElement.GetRawText());

                    foreach (var excl in exclusions ?? [])
                    {
                        if (pp.RootElement.TryGetProperty(excl, out _))
                            Assert.Fail($"data.json contains excluded element {excl}");
                    }

                    return true;
                }
                )
            );
    }

    private static DirectoryInfo SetupSymlinks()
    {
        var basePath = Path.Combine("TestData", "symlinks");
        var targetPath = new DirectoryInfo(Path.Combine("TestData", "sl-test"));

        if (targetPath.Exists)
            targetPath.Delete(true);

        targetPath.Create();
        var dataDir = targetPath.CreateSubdirectory(".data");

        File.Copy(Path.Combine(basePath, "data.yaml"), Path.Combine(dataDir.FullName, "data.yaml"));
        File.Copy(Path.Combine(basePath, "policy.rego"), Path.Combine(dataDir.FullName, "policy.rego"));

        File.CreateSymbolicLink(Path.Combine(targetPath.FullName, "data.yaml"), Path.Combine(".data", "data.yaml"));
        File.CreateSymbolicLink(Path.Combine(targetPath.FullName, "policy.rego"), Path.Combine(".data", "policy.rego"));

        return targetPath;
    }

    [Fact]
    public async Task FollowSymlinks()
    {
        // We need to do more setup for this one.
        var targetPath = SetupSymlinks();

        var ignore = new[] { ".*" }.ToHashSet();

        var compiler = CreateCompiler(LoggerFactory);

        await using var bundle = await compiler.CompileBundleAsync(
            targetPath.FullName,
            new()
            {
                Entrypoints = ["sl/allow"],
                CapabilitiesVersion = DefaultCaps,
                FollowSymlinks = true,
                Debug = true,
                Ignore = ignore,
            },
            TestContext.Current.CancellationToken
            );

        AssertBundle.DumpBundle(bundle, OutputHelper);

        AssertBundle.Content(
            bundle,
            p => AssertBundle.HasEntry(p, "/policy.wasm"),
            p => AssertBundle.AssertData(
                p,
                pp =>
                {
                    OutputHelper.WriteLine(pp.RootElement.GetRawText());
                    Assert.True(pp.RootElement.TryGetProperty("test", out _));
                    return true;
                }
                )
            );
    }

    [Fact]
    public async Task Symlinks()
    {
        // We need to do more setup for this one.
        var targetPath = SetupSymlinks();

        var ignore = new[] { ".*" }.ToHashSet();

        using var ms = new MemoryStream();

        var bundleWriter = BundleWriter.FromDirectory(ms, targetPath.FullName, ignore);

        Assert.False(bundleWriter.IsEmpty);

        await bundleWriter.DisposeAsync();
        ms.Seek(0, SeekOrigin.Begin);

        var compiler = CreateCompiler(LoggerFactory);

        await using var bundle = await compiler.CompileBundleAsync(
            ms,
            new()
            {
                Entrypoints = ["sl/allow"],
                CapabilitiesVersion = DefaultCaps,
                Ignore = ignore,
            },
            TestContext.Current.CancellationToken
            );

        AssertBundle.DumpBundle(bundle, OutputHelper);

        AssertBundle.Content(
            bundle,
            p => AssertBundle.HasEntry(p, "/policy.wasm"),
            p => AssertBundle.AssertData(
                p,
                pp =>
                {
                    OutputHelper.WriteLine(pp.RootElement.GetRawText());
                    return pp.RootElement.TryGetProperty("test", out _);
                }
                )
            );
    }

    [Fact]
    public async Task Revision()
    {
        var compiler = CreateCompiler(LoggerFactory);

        var src = """
            package test.v1

            # METADATA
            # entrypoint: true
            allow if { true }
            """;

        await using var policy = await compiler.CompileSourceAsync(
            src,
            new()
            {
                Revision = "rev1",
                CapabilitiesVersion = DefaultCaps,
                Debug = true,
                RegoVersion = RegoVersion.V1,
            },
            TestContext.Current.CancellationToken
            );

        AssertBundle.DumpBundle(policy, OutputHelper);
        AssertBundle.Content(
            policy,
            p => AssertBundle.HasEntry(p, "/data.json"),
            p => p.Name.EndsWith("policy.rego"),
            p => AssertBundle.HasEntry(p, "/policy.wasm"),
            p => AssertBundle.AssertManifest(p, pp => string.Equals("rev1", pp.Revision))
            );
    }

    [Fact]
    public async Task V1Compatibility()
    {
        var compiler = CreateCompiler(LoggerFactory);

        var src = """
            package test.v1

            # METADATA
            # entrypoint: true
            allow if { true }
            """;

        await using var policy = await compiler.CompileSourceAsync(
            src,
            new()
            {
                CapabilitiesVersion = DefaultCaps,
                Debug = true,
                RegoVersion = RegoVersion.V1,
            },
            TestContext.Current.CancellationToken
            );

        AssertBundle.IsValid(policy);
    }
}