namespace GameGuild.SharedKernel.Configuration;

/// <summary>
///     Configuration options for health checks
/// </summary>
public class HealthCheckOptions : BaseOptions
{
    public HealthCheckOptions()
    {
        Tags.Add("database", "infrastructure");
        Tags.Add("api", "readiness");
    }

    public bool EnableDatabaseCheck { get; set; } = true;

    public bool EnableApiHealthCheck { get; set; } = true;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public Dictionary<string, string> Tags { get; } = new Dictionary<string, string>();

    public override void Validate()
    {
        base.Validate();

        if (Timeout <= TimeSpan.Zero) throw new InvalidOperationException("Health check timeout must be greater than zero");
    }
}
