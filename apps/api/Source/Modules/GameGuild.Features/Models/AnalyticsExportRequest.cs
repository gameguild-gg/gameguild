namespace GameGuild.Features;

/// <summary>
///     Request for exporting analytics data
/// </summary>
public abstract class AnalyticsExportRequest
{
    /// <summary>
    ///     Start date for the export
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    ///     End date for the export
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    ///     Feature keys to include (empty for all)
    /// </summary>
    public List<string> FeatureKeys { get; set; } = new List<string>();

    /// <summary>
    ///     Tenant IDs to include (empty for all)
    /// </summary>
    public List<Guid> TenantIds { get; set; } = new List<Guid>();

    /// <summary>
    ///     Export format (csv, json, excel)
    /// </summary>
    public string Format { get; set; } = "json";

    /// <summary>
    ///     Whether to include detailed metrics
    /// </summary>
    public bool IncludeDetails { get; set; } = true;
}
