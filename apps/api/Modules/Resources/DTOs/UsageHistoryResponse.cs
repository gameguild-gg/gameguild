namespace GameGuild.Modules.Resources.DTOs;

/// <summary>
/// Paginated response for usage history
/// </summary>
public class UsageHistoryResponse
{
    /// <summary>
    /// Usage history records
    /// </summary>
    public List<UsageHistoryItem> Records { get; set; } = new();

    /// <summary>
    /// Total number of records
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
}

/// <summary>
/// Individual usage history item with trend analysis
/// </summary>
public class UsageHistoryItem
{
    /// <summary>
    /// Usage record ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Resource usage type
    /// </summary>
    public ResourceUsageType Type { get; set; }

    /// <summary>
    /// Usage count
    /// </summary>
    public long Count { get; set; }

    /// <summary>
    /// Source of the usage
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// User who generated the usage
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Resource identifier
    /// </summary>
    public Guid? ResourceId { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// When the usage was recorded
    /// </summary>
    public DateTime RecordedAt { get; set; }

    /// <summary>
    /// Cumulative usage up to this point in time
    /// </summary>
    public long CumulativeUsage { get; set; }

    /// <summary>
    /// Percentage change from previous record
    /// </summary>
    public double? PercentageChange { get; set; }
}
