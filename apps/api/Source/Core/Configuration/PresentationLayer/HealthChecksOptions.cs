namespace GameGuild;

public class HealthChecksOptions
{
    public string HealthCheckPath { get; set; } = "/health";

    public int TimeoutSeconds { get; set; } = 30;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(HealthCheckPath))
            throw new InvalidOperationException("Health check path cannot be null or empty.");

        if (!HealthCheckPath.StartsWith("/"))
            throw new InvalidOperationException("Health check path must start with '/'.");

        if (TimeoutSeconds <= 0)
            throw new InvalidOperationException("Timeout seconds must be greater than zero.");
    }
}
