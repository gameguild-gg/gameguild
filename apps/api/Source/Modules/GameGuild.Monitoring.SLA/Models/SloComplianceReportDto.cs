namespace GameGuild.Monitoring.SLA;

/// <summary>
///     DTO for comprehensive SLO compliance report
/// </summary>
public sealed record SloComplianceReportDto
{
    /// <summary>
    ///     When this report was generated
    /// </summary>
    public DateTimeOffset GeneratedAt { get; init; }

    /// <summary>
    ///     Start date of the reporting period
    /// </summary>
    public DateTimeOffset StartDate { get; init; }

    /// <summary>
    ///     End date of the reporting period
    /// </summary>
    public DateTimeOffset EndDate { get; init; }

    /// <summary>
    ///     Tenant ID for the report (optional for multi-tenant reports)
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    ///     Total number of SLOs evaluated
    /// </summary>
    public int TotalSlos { get; init; }

    /// <summary>
    ///     Number of SLOs meeting their targets
    /// </summary>
    public int CompliantSlos { get; init; }

    /// <summary>
    ///     Number of SLOs violating their targets
    /// </summary>
    public int ViolatedSlos { get; init; }

    /// <summary>
    ///     Overall compliance percentage across all SLOs
    /// </summary>
    public double OverallCompliancePercentage { get; init; }

    /// <summary>
    ///     Detailed summaries for each SLO
    /// </summary>
    public IReadOnlyList<SloComplianceSummaryDto> SloSummaries { get; init; } = [];
}
