using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

using MemoryStream = System.IO.MemoryStream;

namespace OpaDotNet.Compilation.Abstractions;

/// <summary>
/// Implements writing files into OPA policy bundle.
/// </summary>
/// <remarks>You need to dispose <see cref="BundleWriter"/> instance before you can use resulting bundle.</remarks>
/// <example>
/// <code>
/// using var ms = new MemoryStream();
///
/// using (var writer = new BundleWriter(ms))
/// {
///     writer.WriteEntry("package test", "policy.rego");
/// }
///
/// // Now bundle have been constructed.
/// ms.Seek(0, SeekOrigin.Begin);
/// ...
/// </code>
/// </example>
[PublicAPI]
public sealed class BundleWriter : IDisposable, IAsyncDisposable
{
    private readonly TarWriter _writer;

    /// <summary>
    /// <c>true</c> if BundleWriter has no entries written; otherwise <c>false</c>.
    /// </summary>
    public bool IsEmpty { get; private set; }

    private static JsonDocumentOptions CapsOptions { get; } = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Creates new instance of <see cref="BundleWriter"/>.
    /// </summary>
    /// <param name="stream">Stream to write bundle to.</param>
    /// <param name="manifest">Policy bundle manifest.</param>
    public BundleWriter(Stream stream, BundleManifest? manifest = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var zip = new GZipStream(stream, CompressionMode.Compress, true);
        _writer = new TarWriter(zip);

        if (manifest != null)
            WriteEntry(JsonSerializer.Serialize(manifest), ".manifest");
    }

    private static string NormalizePath(string path) => "/" + path.Replace("\\", "/").TrimStart('/');

    /// <summary>
    /// Merges two capabilities.json streams.
    /// </summary>
    /// <param name="caps1">First capabilities.json stream.</param>
    /// <param name="caps2">Second capabilities.json stream.</param>
    /// <returns>New merged capabilities.json stream.</returns>
    public static Stream MergeCapabilities(Stream caps1, Stream caps2)
    {
        ArgumentNullException.ThrowIfNull(caps1);
        ArgumentNullException.ThrowIfNull(caps2);

        var resultDoc = JsonNode.Parse(caps1);

        if (resultDoc == null)
            throw new RegoCompilationException("Failed to parse capabilities file");

        var resultBins = resultDoc.Root["builtins"]?.AsArray();

        if (resultBins == null)
            throw new RegoCompilationException("Invalid capabilities file: 'builtins' node not found");

        var capsDoc = JsonDocument.Parse(caps2, CapsOptions);
        var capsBins = capsDoc.RootElement.GetProperty("builtins");

        foreach (var bin in capsBins.EnumerateArray())
        {
            JsonNode? node = bin.ValueKind switch
            {
                JsonValueKind.Array => JsonArray.Create(bin),
                JsonValueKind.Object => JsonObject.Create(bin),
                _ => JsonValue.Create(bin)
            };

            resultBins.Add(node);
        }

        var ms = new MemoryStream();

        using (var jw = new Utf8JsonWriter(ms))
        {
            resultDoc.WriteTo(jw);
        }

        ms.Seek(0, SeekOrigin.Begin);

        return ms;
    }

    /// <summary>
    /// Creates new instance of <see cref="BundleWriter"/> and populates it with files in <paramref name="path"/>.
    /// </summary>
    /// <param name="stream">Stream to write bundle to.</param>
    /// <param name="path">Path containing bundle source files.</param>
    /// <param name="exclusions">File name patterns for files the BundleWriter should exclude from the results.</param>
    public static BundleWriter FromDirectory(
        Stream stream,
        string path,
        IReadOnlySet<string>? exclusions)
    {
        var di = new DirectoryInfo(path);

        if (!di.Exists)
            throw new DirectoryNotFoundException($"Directory {di.FullName} was not found");

        var comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var glob = new Matcher(comparison);
        glob.AddInclude("**/data.json");
        glob.AddInclude("**/data.yaml");
        glob.AddInclude("**/*.rego");
        glob.AddInclude("**/policy.wasm");
        glob.AddInclude("**/.manifest");

        if (exclusions != null)
        {
            foreach (var excl in exclusions)
                glob.AddExclude(excl);
        }

        var matches = glob.Execute(new DirectoryInfoWrapper(di));

        var writer = new BundleWriter(stream);

        foreach (var file in matches.Files)
        {
            var filePath = string.IsNullOrWhiteSpace(file.Stem) || string.Equals(file.Path, file.Stem, StringComparison.Ordinal)
                ? file.Path
                : Path.Combine(file.Path, file.Stem);

            var fullPath = Path.Combine(di.FullName, filePath);

            using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            writer.WriteEntry(fs, filePath);
        }

        return writer;
    }

    /// <summary>
    /// Writes string content into bundle.
    /// </summary>
    /// <param name="str">String content.</param>
    /// <param name="path">Relative file path inside bundle.</param>
    public void WriteEntry(ReadOnlySpan<char> str, string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        Span<byte> bytes = new byte[Encoding.UTF8.GetByteCount(str)];
        _ = Encoding.UTF8.GetBytes(str, bytes);

        WriteEntry(bytes, path);
    }

    /// <summary>
    /// Writes bytes content into bundle.
    /// </summary>
    /// <param name="bytes">String content.</param>
    /// <param name="path">Relative file path inside bundle.</param>
    public void WriteEntry(ReadOnlySpan<byte> bytes, string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        using var ms = new MemoryStream(bytes.Length);
        ms.Write(bytes);
        ms.Seek(0, SeekOrigin.Begin);

        WriteEntry(ms, path);
    }

    /// <summary>
    /// Writes stream content into bundle.
    /// </summary>
    /// <param name="stream">String content.</param>
    /// <param name="path">Relative file path inside bundle.</param>
    public void WriteEntry(Stream stream, string path)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (Path.IsPathRooted(path))
        {
            if (Path.GetPathRoot(path)?[0] != Path.DirectorySeparatorChar)
                path = path[2..];
        }

        var entry = new PaxTarEntry(TarEntryType.RegularFile, NormalizePath(path))
        {
            DataStream = stream,
        };

        _writer.WriteEntry(entry);
        IsEmpty = false;
    }

    /// <summary>
    /// Merges contents of source bundle into this bundle.
    /// </summary>
    /// <param name="bundle">Source bundle.</param>
    public void WriteBundle(Span<byte> bundle)
    {
        using var ms = new MemoryStream(bundle.Length);
        ms.Write(bundle);
        ms.Seek(0, SeekOrigin.Begin);

        WriteBundle(ms);
    }

    /// <summary>
    /// Merges contents of source bundle into this bundle.
    /// </summary>
    /// <param name="bundle">Source bundle.</param>
    public void WriteBundle(Stream bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        using var gzip = new GZipStream(bundle, CompressionMode.Decompress);
        using var ms = new MemoryStream();

        gzip.CopyTo(ms);
        ms.Seek(0, SeekOrigin.Begin);

        using var tr = new TarReader(ms);

        while (tr.GetNextEntry() is { } sourceEntry)
        {
            var entry = new PaxTarEntry(sourceEntry);
            _writer.WriteEntry(entry);
        }
    }

    /// <summary>
    /// Writes contents of file into bundle.
    /// </summary>
    /// <param name="path">File to write.</param>
    /// <param name="overridePath">Relative file path inside bundle.</param>
    public void WriteFile(string path, string? overridePath = null)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        var targetPath = overridePath ?? path;
        WriteEntry(fs, targetPath);
    }

    /// <summary>
    /// Writes manifest into bundle.
    /// </summary>
    /// <param name="manifest">Policy bundle manifest.</param>
    public void WriteManifest(BundleManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        WriteEntry(JsonSerializer.Serialize(manifest), ".manifest");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _writer.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _writer.DisposeAsync().ConfigureAwait(false);
    }
}