namespace GameGuild;

/// <summary>
/// Configuration options for health checks.
/// </summary>
public class HealthCheckOptions
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
}
