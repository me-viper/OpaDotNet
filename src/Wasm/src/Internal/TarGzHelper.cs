using System.Collections.Immutable;
using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json.Nodes;

using OpaDotNet.Wasm.Validation;

namespace OpaDotNet.Wasm.Internal;

internal static class TarGzHelper
{
    public const string JsonDataFile = "data.json";
    public const string ManifestFile = ".manifest";
    public const string SignaturesFile = ".signatures.json";

    public static OpaPolicy ReadBundleAndValidate(Stream archive, SignatureValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentNullException.ThrowIfNull(options);

        using var gzip = new GZipStream(archive, CompressionMode.Decompress);
        using var ms = new MemoryStream();

        gzip.CopyTo(ms);
        ms.Seek(0, SeekOrigin.Begin);

        if (options is { Validation: SignatureValidationType.Skip })
            return ReadBundle(ms);

        _ = TryGetSignatures(ms, out var signatures);
        ms.Seek(0, SeekOrigin.Begin);

        if (signatures == null && options.Validation == SignatureValidationType.Default)
            return ReadBundle(ms);

        if (signatures == null && options.Validation == SignatureValidationType.Required)
            throw new BundleSignatureValidationException("Signature validation options configured but bundle is not signed");

        var files = options.Validator.Validate(signatures!, options);

        return ReadBundle(ms, files, options.ExcludeFiles);
    }

    public static DirectoryInfo UnpackBundle(Stream archive, DirectoryInfo basePath, SignatureValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentNullException.ThrowIfNull(options);

        using var gzip = new GZipStream(archive, CompressionMode.Decompress);
        using var ms = new MemoryStream();

        var result = new DirectoryInfo(Path.Combine(basePath.FullName, Guid.NewGuid().ToString()));
        result.Create();

        gzip.CopyTo(ms);
        ms.Seek(0, SeekOrigin.Begin);

        if (options is { Validation: SignatureValidationType.Skip })
        {
            ReadBundleInternal(
                ms,
                null,
                null,
                (entry, validator) => ExtractEntry(entry, result.FullName, validator)
                );
            return result;
        }

        _ = TryGetSignatures(ms, out var signatures);
        ms.Seek(0, SeekOrigin.Begin);

        if (signatures == null && options.Validation == SignatureValidationType.Default)
        {
            ReadBundleInternal(
                ms,
                null,
                null,
                (entry, validator) => ExtractEntry(entry, result.FullName, validator)
                );
            return result;
        }

        if (signatures == null && options.Validation == SignatureValidationType.Required)
            throw new BundleSignatureValidationException("Signature validation options configured but bundle is not signed");

        var files = options.Validator.Validate(signatures!, options);

        ReadBundleInternal(
            ms,
            files,
            options.ExcludeFiles,
            (entry, validator) => ExtractEntry(entry, result.FullName, validator)
            );

        return result;
    }

    private static bool TryGetSignatures(Stream contents, out BundleSignatures? result)
    {
        result = null;

        using var tr = new TarReader(contents, true);

        Memory<byte>? sig = null;

        while (tr.GetNextEntry() is { } entry)
        {
            if (string.Equals(entry.Name, $"/{SignaturesFile}", StringComparison.OrdinalIgnoreCase))
            {
                sig = ReadEntry(entry);
                break;
            }
        }

        if (sig == null)
            return false;

        var reader = new Utf8JsonReader(sig.Value.Span);
        result = JsonSerializer.Deserialize<BundleSignatures>(ref reader);

        if (result == null)
            throw new BundleSignatureValidationException("Failed to deserialize signatures file");

        return true;
    }

    private static Memory<byte> ReadEntry(TarEntry entry, Action<byte[]>? validator = null)
    {
        if (entry.DataStream == null)
            throw new InvalidOperationException($"Failed to read {entry.Name}");

        var result = new byte[entry.DataStream.Length];
        var bytesRead = entry.DataStream.Read(result);

        if (bytesRead < entry.DataStream.Length)
            throw new OpaRuntimeException($"Failed to read tar entry {entry.Name}");

        validator?.Invoke(result);

        return result;
    }

    private static Stream GetEntryBytes(SignedFile signedFile, byte[] entry)
    {
        Stream result;
        HashSet<string> structuredFiles = [JsonDataFile, ManifestFile, SignaturesFile];

        if (!structuredFiles.Contains(signedFile.Name!))
            result = new MemoryStream(entry);
        else
        {
            var json = JsonNode.Parse(entry);

            if (json == null)
                return Stream.Null;

            result = new MemoryStream(entry.Length);
            json.ToAlphabeticJsonBytes(result);
        }

        result.Seek(0, SeekOrigin.Begin);

        return result;
    }

    [ExcludeFromCodeCoverage]
    private static void ValidateEntry(SignedFile signedFile, byte[] entry)
    {
        using var bytes = GetEntryBytes(signedFile, entry);

        Func<Stream, byte[]> calcHash;

        // We don't support: SHA-224, SHA-512-224, SHA-512-256.
        if (string.Equals(signedFile.Algorithm, "MD5", StringComparison.Ordinal))
            calcHash = MD5.HashData;
        else if (string.Equals(signedFile.Algorithm, "SHA-1", StringComparison.Ordinal))
            calcHash = SHA1.HashData;
        else if (string.Equals(signedFile.Algorithm, "SHA-256", StringComparison.Ordinal))
            calcHash = SHA256.HashData;
        else if (string.Equals(signedFile.Algorithm, "SHA-384", StringComparison.Ordinal))
            calcHash = SHA384.HashData;
        else if (string.Equals(signedFile.Algorithm, "SHA-512", StringComparison.Ordinal))
            calcHash = SHA512.HashData;
        else
        {
            throw new BundleSignatureValidationException(
                $"Unsupported hashing algorithm {signedFile.Algorithm} for {signedFile.Name}"
                );
        }

        var hashStr = Convert.ToHexString(calcHash(bytes));

        if (string.Equals(signedFile.Hash, hashStr, StringComparison.OrdinalIgnoreCase))
            return;

        throw new BundleChecksumValidationException(
            signedFile.Name!,
            signedFile.Algorithm,
            signedFile.Hash,
            hashStr
            );
    }

    private static OpaPolicy ReadBundle(
        Stream stream,
        IReadOnlySet<SignedFile>? signedFiles = null,
        IReadOnlySet<string>? excludeFiles = null)
    {
        Memory<byte>? policy = null;
        Memory<byte>? data = null;

        ReadBundleInternal(
            stream,
            signedFiles,
            excludeFiles,
            (entry, validator) =>
            {
                if (string.Equals(entry.Name, "/policy.wasm", StringComparison.OrdinalIgnoreCase))
                    policy = ReadEntry(entry, validator);
                else if (string.Equals(entry.Name, $"/{JsonDataFile}", StringComparison.OrdinalIgnoreCase))
                    data = ReadEntry(entry, validator);
                else
                    _ = ReadEntry(entry, validator);
            }
            );

        if (policy == null)
            throw new OpaRuntimeException("Bundle does not contain policy.wasm file");

        return new(policy.Value, data ?? Memory<byte>.Empty);
    }

    private static void ExtractEntry(TarEntry entry, string basePath, Action<byte[]>? validator = null)
    {
        if (entry.DataStream == null)
            throw new InvalidOperationException($"Failed to read {entry.Name}");

        if (entry.EntryType is TarEntryType.SymbolicLink or TarEntryType.HardLink or TarEntryType.GlobalExtendedAttributes)
            return;

        var result = new byte[entry.DataStream.Length];
        var bytesRead = entry.DataStream.Read(result);

        if (bytesRead < entry.DataStream.Length)
            throw new OpaRuntimeException($"Failed to read tar entry {entry.Name}");

        validator?.Invoke(result);

        var path = Path.Combine(basePath, entry.Name.Trim('/'));
        var directory = Path.GetDirectoryName(path)!;

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllBytes(path, result);
    }

    private static void ReadBundleInternal(
        Stream stream,
        IReadOnlySet<SignedFile>? signedFiles,
        IReadOnlySet<string>? excludeFiles,
        Action<TarEntry, Action<byte[]>?> reader)
    {
        using var tr = new TarReader(stream, true);

        excludeFiles ??= ImmutableHashSet<string>.Empty;

        while (tr.GetNextEntry() is { } entry)
        {
            if (string.Equals(entry.Name, $"/{SignaturesFile}", StringComparison.OrdinalIgnoreCase))
                continue;

            Action<byte[]>? validator = null;
            SignedFile? sigFile = null;

            if (signedFiles != null)
            {
                sigFile = signedFiles.FirstOrDefault(p => string.Equals(p.Name, entry.Name[1..], StringComparison.Ordinal));

                var excluded = excludeFiles.Contains(entry.Name[1..]);

                if (!excluded)
                {
                    if (sigFile != null)
                        validator = p => ValidateEntry(sigFile, p);
                    else
                    {
                        throw new BundleSignatureValidationException(
                            $"File {entry.Name[1..]} is present in the bundle but missing in signature"
                            );
                    }
                }
            }

            reader(entry, validator);

            if (sigFile != null)
                sigFile.IsValid = true;
        }

        if (signedFiles == null)
            return;

        var missingFiles = signedFiles.Where(p => !p.IsValid).Select(p => p.Name).ToHashSet();
        missingFiles.ExceptWith(excludeFiles);

        if (missingFiles.Count > 0)
        {
            var files = string.Join(';', missingFiles);

            throw new BundleSignatureValidationException(
                $"Files '{files}' are present in the signature but missing in the bundle"
                );
        }
    }
}