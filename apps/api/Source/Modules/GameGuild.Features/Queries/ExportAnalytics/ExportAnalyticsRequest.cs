namespace GameGuild.Features;

/// <summary>
///     Request DTO for analytics export via HTTP API
/// </summary>
public sealed class ExportAnalyticsRequest
{
    /// <summary>
    ///     Feature keys to export (null for all)
    /// </summary>
    public IEnumerable<string>? FeatureKeys { get; set; }

    /// <summary>
    ///     Start date of export period
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    ///     End date of export period
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    ///     Export format (csv, json, excel)
    /// </summary>
    public string Format { get; set; } = "json";

    /// <summary>
    ///     Include detailed breakdown
    /// </summary>
    public bool IncludeDetails { get; set; } = true;

    /// <summary>
    ///     Group by field (tenant, user, environment)
    /// </summary>
    public string? GroupBy { get; set; }

    /// <summary>
    ///     Environment filter
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    ///     Tenant filter
    /// </summary>
    public Guid? TenantId { get; set; }
}
