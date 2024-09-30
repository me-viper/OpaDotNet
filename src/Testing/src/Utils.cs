namespace OpaDotNet.InternalTesting;

public static class Utils
{
    public const string DefaultCapabilities = "v0.64.0";

    public static DirectoryInfo CreateTempDirectory(string basePath)
    {
        var path = Path.GetFileNameWithoutExtension(Path.GetTempFileName());
        return Directory.CreateDirectory(Path.Combine(basePath, path));
    }
}