
namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Data transfer object for Service Level Objective
/// </summary>
public class SloDto
{
    /// <summary>
    ///     SLO identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Tenant identifier
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    ///     SLO name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Service name
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    ///     Target percentage
    /// </summary>
    public double TargetPercentage { get; set; }

    /// <summary>
    ///     Time window in days
    /// </summary>
    public int TimeWindowDays { get; set; }

    /// <summary>
    ///     Error budget percentage
    /// </summary>
    public double ErrorBudgetPercentage { get; set; }

    /// <summary>
    ///     Alert threshold percentage
    /// </summary>
    public double AlertThresholdPercentage { get; set; }

    /// <summary>
    ///     Whether enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    ///     Current status
    /// </summary>
    public SloStatus Status { get; set; }

    /// <summary>
    ///     Last evaluated timestamp
    /// </summary>
    public DateTimeOffset? LastEvaluatedAt { get; set; }

    /// <summary>
    ///     Current actual percentage
    /// </summary>
    public double? CurrentActualPercentage { get; set; }

    /// <summary>
    ///     Remaining error budget percentage
    /// </summary>
    public double? RemainingErrorBudget { get; set; }

    /// <summary>
    ///     Created timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    ///     Updated timestamp
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
