namespace GameGuild.Modules.Resources.DTOs;

/// <summary>
/// Detailed usage response with aggregations and breakdown
/// </summary>
public class ResourceUsageDetailsResponse
{
    /// <summary>
    /// Detailed usage records
    /// </summary>
    public List<UsageDetailItem> Records { get; set; } = new();

    /// <summary>
    /// Aggregated statistics
    /// </summary>
    public UsageAggregation Aggregation { get; set; } = new();

    /// <summary>
    /// Usage breakdown by type
    /// </summary>
    public Dictionary<ResourceUsageType, long> ByType { get; set; } = new();

    /// <summary>
    /// Usage breakdown by source
    /// </summary>
    public Dictionary<string, long> BySource { get; set; } = new();

    /// <summary>
    /// Top users by usage count
    /// </summary>
    public List<UserUsageSummary> TopUsers { get; set; } = new();

    /// <summary>
    /// Top resources by usage count
    /// </summary>
    public List<ResourceUsageSummary> TopResources { get; set; } = new();
}

/// <summary>
/// Detailed usage record item
/// </summary>
public class UsageDetailItem
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
    /// Additional metadata (parsed from JSON)
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// When the usage was recorded
    /// </summary>
    public DateTime RecordedAt { get; set; }
}

/// <summary>
/// Aggregated usage statistics
/// </summary>
public class UsageAggregation
{
    /// <summary>
    /// Total usage count
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// Average usage per record
    /// </summary>
    public double AverageCount { get; set; }

    /// <summary>
    /// Minimum usage count
    /// </summary>
    public long MinCount { get; set; }

    /// <summary>
    /// Maximum usage count
    /// </summary>
    public long MaxCount { get; set; }

    /// <summary>
    /// Total number of records
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// Number of unique users
    /// </summary>
    public int UniqueUsers { get; set; }

    /// <summary>
    /// Number of unique resources
    /// </summary>
    public int UniqueResources { get; set; }

    /// <summary>
    /// Number of unique sources
    /// </summary>
    public int UniqueSources { get; set; }

    /// <summary>
    /// Date range start
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Date range end
    /// </summary>
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// User usage summary
/// </summary>
public class UserUsageSummary
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Total usage count by this user
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// Number of usage records
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// Percentage of total usage
    /// </summary>
    public double PercentageOfTotal { get; set; }
}

/// <summary>
/// Resource usage summary
/// </summary>
public class ResourceUsageSummary
{
    /// <summary>
    /// Resource ID
    /// </summary>
    public Guid ResourceId { get; set; }

    /// <summary>
    /// Total usage count for this resource
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// Number of usage records
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// Percentage of total usage
    /// </summary>
    public double PercentageOfTotal { get; set; }
}
