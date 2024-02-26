using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Validation;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

public class InMemorySignatureTests(ITestOutputHelper output) : BundleSignatureTests(output);

public class FsSignatureTests(ITestOutputHelper output) : BundleSignatureTests(output)
{
    public override string? CachePath
    {
        get
        {
            var di = new DirectoryInfo("cache");

            if (di.Exists)
                di.Delete(true);

            di.Create();

            return di.FullName;
        }
    }
}

public class BundleSignatureTests(ITestOutputHelper output) : OpaTestBase(output)
{
    private const string BasePath = "./TestData/signature";

    private string SourcePath { get; } = Path.Combine(BasePath, "source");

    private string SigPath { get; } = Path.Combine(BasePath, ".signatures.json");

    public virtual string? CachePath { get; }

    [Fact]
    public void DefaultValidatorAlgMismatch()
    {
        using var sigFile = File.OpenRead(SigPath);
        var sig = JsonSerializer.Deserialize<BundleSignatures>(sigFile);

        Assert.NotNull(sig);

        var opts = new SignatureValidationOptions
        {
            SigningAlgorithm = "RS512",
            VerificationKeyPath = Path.Combine(BasePath, "rsa_pub.pem"),
        };

        var validator = new DefaultBundleSignatureValidator();

        Assert.Throws<BundleSignatureValidationException>(() => validator.Validate(sig, opts));
    }

    [Fact]
    public void DefaultValidatorKidMismatch()
    {
        using var sigFile = File.OpenRead(SigPath);
        var sig = JsonSerializer.Deserialize<BundleSignatures>(sigFile);

        Assert.NotNull(sig);

        var opts = new SignatureValidationOptions
        {
            VerificationKeyId = "wrong",
            VerificationKeyPath = Path.Combine(BasePath, "rsa_pub.pem"),
        };

        var validator = new DefaultBundleSignatureValidator();

        Assert.Throws<BundleSignatureValidationException>(() => validator.Validate(sig, opts));
    }

    [Fact]
    public void DefaultValidator()
    {
        using var sigFile = File.OpenRead(SigPath);
        var sig = JsonSerializer.Deserialize<BundleSignatures>(sigFile);

        Assert.NotNull(sig);

        var validator = new DefaultBundleSignatureValidator();
        var files = validator.Validate(sig, new() { VerificationKeyPath = Path.Combine(BasePath, "rsa_pub.pem") });

        Assert.Collection(
            files,
            p => Assert.Equal("data.yaml", p.Name),
            p => Assert.Equal("p1.rego", p.Name)
            );
    }

    [Fact]
    public void HappyPath()
    {
        using var fs = File.OpenRead(Path.Combine(BasePath, "ok.tar.gz"));

        var engineOpts = new WasmPolicyEngineOptions
        {
            CachePath = CachePath,
            SignatureValidation = new()
            {
                VerificationKeyPath = Path.Combine(BasePath, "rsa_pub.pem")
            },
        };

        var factory = new OpaBundleEvaluatorFactory(fs, engineOpts, importsAbiFactory: () => new TestImportsAbi(Output));

        Assert.NotNull(factory.Create());
    }

    [Fact]
    public void KeyMissing()
    {
        using var fs = File.OpenRead(Path.Combine(BasePath, "ok.tar.gz"));
        var engineOpts = new WasmPolicyEngineOptions { CachePath = CachePath };

        Assert.Throws<BundleSignatureValidationException>(
            () => new OpaBundleEvaluatorFactory(fs, engineOpts, importsAbiFactory: () => new TestImportsAbi(Output))
            );
    }

    [Fact]
    public void KeyMismatch()
    {
        using var fs = File.OpenRead(Path.Combine(BasePath, "ok.tar.gz"));

        var engineOpts = new WasmPolicyEngineOptions
        {
            CachePath = CachePath,
            SignatureValidation = new() { VerificationKeyPath = Path.Combine(BasePath, "bad.rsa_pub.pem") },
        };

        Assert.Throws<BundleSignatureValidationException>(
            () => new OpaBundleEvaluatorFactory(fs, engineOpts, importsAbiFactory: () => new TestImportsAbi(Output))
            );
    }

    [Fact]
    public void HashMismatch()
    {
        using var ms = new MemoryStream();

        using (var bw = new BundleWriter(ms))
        {
            bw.WriteEntry(File.OpenRead(Path.Combine(SourcePath, "data.yaml")), "data.yaml");
            bw.WriteEntry("fff", "p1.rego");
            bw.WriteEntry(File.OpenRead(Path.Combine(BasePath, "policy.wasm")), "policy.wasm");
            bw.WriteEntry(File.OpenRead(SigPath), ".signatures.json");
        }

        ms.Seek(0, SeekOrigin.Begin);

        var engineOpts = new WasmPolicyEngineOptions
        {
            CachePath = CachePath,
            SignatureValidation = new()
            {
                VerificationKeyPath = Path.Combine(BasePath, "rsa_pub.pem"),
                ExcludeFiles = new HashSet<string> { "data.yaml", "policy.wasm" },
            },

        };

        var ex = Assert.Throws<BundleChecksumValidationException>(
            () => new OpaBundleEvaluatorFactory(
                ms,
                engineOpts,
                importsAbiFactory: () => new TestImportsAbi(Output)
                )
            );

        Assert.Equal("p1.rego", ex.FileName);
    }

    [Fact]
    public void IgnoreHashMismatch()
    {
        using var ms = new MemoryStream();

        using (var bw = new BundleWriter(ms))
        {
            bw.WriteEntry(File.OpenRead(Path.Combine(SourcePath, "data.yaml")), "data.yaml");
            bw.WriteEntry("fff", "p1.rego");
            bw.WriteEntry(File.OpenRead(Path.Combine(BasePath, "policy.wasm")), "policy.wasm");
            bw.WriteEntry(File.OpenRead(SigPath), ".signatures.json");
        }

        ms.Seek(0, SeekOrigin.Begin);

        var engineOpts = new WasmPolicyEngineOptions
        {
            CachePath = CachePath,
            SignatureValidation = new()
            {
                VerificationKeyPath = Path.Combine(BasePath, "rsa_pub.pem"),
                ExcludeFiles = new HashSet<string> { "data.yaml", "policy.wasm", "p1.rego" },
            },

        };

        var factory = new OpaBundleEvaluatorFactory(
            ms,
            engineOpts,
            importsAbiFactory: () => new TestImportsAbi(Output)
            );

        Assert.NotNull(factory);
    }

    [Fact]
    public void NotInSignature()
    {
        using var ms = new MemoryStream();

        using (var bw = new BundleWriter(ms))
        {
            bw.WriteEntry(File.OpenRead(Path.Combine(BasePath, "policy.wasm")), "policy.wasm");
            bw.WriteEntry(File.OpenRead(SigPath), ".signatures.json");
        }

        ms.Seek(0, SeekOrigin.Begin);

        var engineOpts = new WasmPolicyEngineOptions
        {
            CachePath = CachePath,
            SignatureValidation = new() { VerificationKeyPath = Path.Combine(BasePath, "rsa_pub.pem") },
        };

        var ex = Assert.Throws<BundleSignatureValidationException>(
            () => new OpaBundleEvaluatorFactory(
                ms,
                engineOpts,
                importsAbiFactory: () => new TestImportsAbi(Output)
                )
            );

        Output.WriteLine(ex.ToString());
    }

    [Fact]
    public void NotInBundle()
    {
        using var ms = new MemoryStream();

        using (var bw = new BundleWriter(ms))
        {
            bw.WriteEntry(File.OpenRead(SigPath), ".signatures.json");
        }

        ms.Seek(0, SeekOrigin.Begin);

        var engineOpts = new WasmPolicyEngineOptions
        {
            CachePath = CachePath,
            SignatureValidation = new() { VerificationKeyPath = Path.Combine(BasePath, "rsa_pub.pem") },
        };

        var ex = Assert.Throws<BundleSignatureValidationException>(
            () => new OpaBundleEvaluatorFactory(
                ms,
                engineOpts,
                importsAbiFactory: () => new TestImportsAbi(Output)
                )
            );

        Output.WriteLine(ex.ToString());
    }

    [Fact]
    public void StructureMismatchWithExclude()
    {
        using var ms = new MemoryStream();

        using (var bw = new BundleWriter(ms))
        {
            bw.WriteEntry(File.OpenRead(Path.Combine(BasePath, "policy.wasm")), "policy.wasm");
            bw.WriteEntry("d", "data.yaml");
            bw.WriteEntry("p", "p1.rego");
            bw.WriteEntry(File.OpenRead(SigPath), ".signatures.json");
        }

        ms.Seek(0, SeekOrigin.Begin);

        var engineOpts = new WasmPolicyEngineOptions
        {
            CachePath = CachePath,
            SignatureValidation = new()
            {
                VerificationKeyPath = Path.Combine(BasePath, "rsa_pub.pem"),
                ExcludeFiles = new HashSet<string> { "policy.wasm", "data.yaml", "p1.rego" },
            },
        };

        var factory = new OpaBundleEvaluatorFactory(
            ms,
            engineOpts,
            importsAbiFactory: () => new TestImportsAbi(Output)
            );

        Assert.NotNull(factory);
    }
}