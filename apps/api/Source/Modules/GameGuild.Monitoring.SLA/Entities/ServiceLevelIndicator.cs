
namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Represents a single Service Level Indicator (SLI) measurement
/// </summary>
public class ServiceLevelIndicator : EntityBase
{
    /// <summary>
    ///     Foreign key to the Service Level Objective
    /// </summary>
    public Guid ServiceLevelObjectiveId { get; set; }

    /// <summary>
    ///     Navigation property to the Service Level Objective
    /// </summary>
    public ServiceLevelObjective? ServiceLevelObjective { get; set; }

    /// <summary>
    ///     Timestamp when this metric was recorded
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    ///     Numeric value of the measurement
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    ///     Whether this measurement was successful (met the criteria)
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    ///     Response time in milliseconds (if applicable)
    /// </summary>
    public long? ResponseTimeMs { get; set; }

    /// <summary>
    ///     HTTP status code (if applicable)
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    ///     API endpoint or service endpoint being measured
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    ///     Additional metadata about this measurement (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    ///     Error message if the measurement failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Creates a successful SLI measurement
    /// </summary>
    public static ServiceLevelIndicator CreateSuccess(Guid sloId, double value, long? responseTimeMs = null, int? statusCode = null, string? endpoint = null)
    {
        return new ServiceLevelIndicator
        {
            ServiceLevelObjectiveId = sloId, Timestamp = DateTimeOffset.UtcNow, Value = value, IsSuccessful = true, ResponseTimeMs = responseTimeMs, StatusCode = statusCode, Endpoint = endpoint
        };
    }

    /// <summary>
    ///     Creates a failed SLI measurement
    /// </summary>
    public static ServiceLevelIndicator CreateFailure(Guid sloId, double value, string errorMessage, long? responseTimeMs = null, int? statusCode = null, string? endpoint = null)
    {
        return new ServiceLevelIndicator
        {
            ServiceLevelObjectiveId = sloId,
            Timestamp = DateTimeOffset.UtcNow,
            Value = value,
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            ResponseTimeMs = responseTimeMs,
            StatusCode = statusCode,
            Endpoint = endpoint
        };
    }
}
