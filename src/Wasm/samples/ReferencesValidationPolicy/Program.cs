using System.Xml.Linq;
using System.Xml.XPath;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Cli;
using OpaDotNet.Wasm;

if (args.Length != 1)
{
    Console.WriteLine("Expected single argument: path to project file");
    return -1;
}

var fi = new FileInfo(args[0]);

if (!fi.Exists)
{
    Console.WriteLine($"Project file {fi.FullName} was not found");
    return -2;
}

var packages = ExtractPackages(fi);

var compiler = new RegoCliCompiler();
var policy = await compiler.CompileBundleAsync(Path.Combine("data", "policy"), new() { Entrypoints = ["samples/invalid_packages"] });
using var evaluator = OpaEvaluatorFactory.CreateFromBundle(policy);

var result = evaluator.Evaluate<Dictionary<string, string>, Dictionary<string, string>>(packages, "samples/invalid_packages");

if (result.Result == null)
{
    Console.WriteLine("Policy failed");
    return -100;
}

if (result.Result.Count == 0)
    Console.WriteLine("All good!");
else
{
    Console.WriteLine("The following packages failed policy validation:");

    foreach (var p in result.Result)
        Console.WriteLine($"{p.Key}: {p.Value}");

    return -3;
}

return 0;


Dictionary<string, string> ExtractPackages(FileInfo projectFile)
{
    var packs = new Dictionary<string, string>();

    var doc = XDocument.Load(projectFile.OpenRead());
    var packRefs = doc.XPathSelectElements(".//PackageReference");

    foreach (var packRef in packRefs)
    {
        var name = packRef.Attribute("Include")?.Value;
        var version = packRef.Attribute("Version")?.Value;

        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(version))
            packs.Add(name, version);
    }

    return packs;
}