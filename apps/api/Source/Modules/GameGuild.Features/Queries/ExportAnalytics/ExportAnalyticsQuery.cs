using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Query to export analytics data for feature flags
/// </summary>
public sealed record ExportAnalyticsQuery : IQuery<AnalyticsExportResult>
{
    /// <summary>
    ///     Feature keys to export (null for all)
    /// </summary>
    public IEnumerable<string>? FeatureKeys { get; init; }

    /// <summary>
    ///     Start date of export period
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    ///     End date of export period
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    ///     Export format (csv, json, excel)
    /// </summary>
    public string Format { get; init; } = "json";

    /// <summary>
    ///     Include detailed breakdown
    /// </summary>
    public bool IncludeDetails { get; init; } = true;

    /// <summary>
    ///     Group by field (tenant, user, environment)
    /// </summary>
    public string? GroupBy { get; init; }

    /// <summary>
    ///     Environment filter
    /// </summary>
    public string? Environment { get; init; }

    /// <summary>
    ///     Tenant filter
    /// </summary>
    public Guid? TenantId { get; init; }
}
