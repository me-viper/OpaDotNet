using Microsoft.Extensions.Configuration;

namespace OpaDotNet.Extensions.AspNetCore;

[UsedImplicitly]
public record OpaPolicyDefinitionOptions
{
    [UsedImplicitly]
    public string? Package { get; set; }

    [UsedImplicitly]
    [ConfigurationKeyName("data.json")]
    public string? DataJson { get; set; }

    [UsedImplicitly]
    [ConfigurationKeyName("data.yaml")]
    public string? DataYaml { get; set; }

    [UsedImplicitly]
    public string? Source { get; set; }
}