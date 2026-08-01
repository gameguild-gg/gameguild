namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Data transfer object for recording SLI metrics
/// </summary>
public class SliMetricDto
{
    /// <summary>
    ///     SLO identifier
    /// </summary>
    public Guid ServiceLevelObjectiveId { get; set; }

    /// <summary>
    ///     Metric value
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    ///     Whether successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    ///     Response time in milliseconds
    /// </summary>
    public long? ResponseTimeMs { get; set; }

    /// <summary>
    ///     HTTP status code
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    ///     Endpoint being measured
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    ///     Additional metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    ///     Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Timestamp of the measurement (defaults to now)
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }
}
