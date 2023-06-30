namespace Opa.WebApp.Policy;

public class OpaPolicyServiceOptions
{
    public string PolicyBundlePath { get; set; } = default!;

    public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromSeconds(5);
}