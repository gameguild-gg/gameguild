namespace GameGuild;

/// <summary>
/// Configuration options for monitoring and logging.
/// </summary>
public class MonitoringOptions
{
    /// <summary>
    /// Enables application insights.
    /// </summary>
    public bool EnableApplicationInsights { get; set; } = false;

    /// <summary>
    /// Application Insights connection string.
    /// </summary>
    public string? ApplicationInsightsConnectionString { get; set; }

    /// <summary>
    /// Enables structured logging.
    /// </summary>
    public bool EnableStructuredLogging { get; set; } = true;

    /// <summary>
    /// Log level for the application.
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Enables health checks.
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Health check endpoints.
    /// </summary>
    public string[] HealthCheckEndpoints { get; set; } = [];

    /// <summary>
    /// Validates the monitoring options.
    /// </summary>
    public void Validate()
    {
        if (EnableApplicationInsights && string.IsNullOrWhiteSpace(ApplicationInsightsConnectionString))
        {
            throw new ArgumentException("Application Insights connection string is required when Application Insights is enabled.");
        }

        var validLogLevels = new[]
        {
            "Trace", "Debug", "Information", "Warning", "Error", "Critical", "None"
        };
        if (!validLogLevels.Contains(LogLevel))
        {
            throw new ArgumentException($"Log level must be one of: {string.Join(", ", validLogLevels)}");
        }
    }
}
