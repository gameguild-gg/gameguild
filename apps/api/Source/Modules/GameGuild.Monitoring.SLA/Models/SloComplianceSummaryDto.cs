namespace GameGuild.Monitoring.SLA;

/// <summary>
///     DTO for individual SLO compliance summary within a report
/// </summary>
public sealed record SloComplianceSummaryDto
{
    /// <summary>
    ///     SLO unique identifier
    /// </summary>
    public Guid SloId { get; init; }

    /// <summary>
    ///     SLO name
    /// </summary>
    public string SloName { get; init; } = string.Empty;

    /// <summary>
    ///     Service name this SLO monitors
    /// </summary>
    public string ServiceName { get; init; } = string.Empty;

    /// <summary>
    ///     Whether the SLO is meeting its target
    /// </summary>
    public bool IsCompliant { get; init; }

    /// <summary>
    ///     Actual success percentage achieved
    /// </summary>
    public double ActualPercentage { get; init; }

    /// <summary>
    ///     Target success percentage
    /// </summary>
    public double TargetPercentage { get; init; }

    /// <summary>
    ///     Number of violations in the reporting period
    /// </summary>
    public int ViolationCount { get; init; }

    /// <summary>
    ///     Remaining error budget percentage
    /// </summary>
    public double ErrorBudgetRemaining { get; init; }

    /// <summary>
    ///     Current SLO status
    /// </summary>
    public string Status { get; init; } = string.Empty;
}
