using GameGuild.Core.Domain;

namespace GameGuild.Modules.SlaMonitoring.Entities;

/// <summary>
/// Represents a Service Level Indicator (SLI) metric measurement.
/// </summary>
public class ServiceLevelIndicator : EntityBase
{
    /// <summary>
    /// Gets or sets the SLO this indicator belongs to.
    /// </summary>
    public Guid ServiceLevelObjectiveId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the SLO.
    /// </summary>
    public ServiceLevelObjective? ServiceLevelObjective { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this metric was recorded.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the measured value.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Gets or sets whether this measurement represents a successful request.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Gets or sets the response time in milliseconds.
    /// </summary>
    public int? ResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code (if applicable).
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the endpoint that was measured.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the error message if the request failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
