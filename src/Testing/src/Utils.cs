namespace OpaDotNet.InternalTesting;

public static class Utils
{
    public const string DefaultCapabilities = "v1.0.0";

    public const string CompilerTrait = "Compiler";

    public const string CliCompilerTrait = "Cli";

    public const string InteropCompilerTrait = "Interop";

    public static DirectoryInfo CreateTempDirectory(string basePath)
    {
        var path = Path.GetFileNameWithoutExtension(Path.GetTempFileName());
        return Directory.CreateDirectory(Path.Combine(basePath, path));
    }
}