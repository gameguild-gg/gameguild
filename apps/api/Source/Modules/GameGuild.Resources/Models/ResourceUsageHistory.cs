namespace GameGuild.Resources;

/// <summary>
///     Represents historical resource usage data
/// </summary>
public class ResourceUsageHistory
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string ResourceType { get; set; } = string.Empty;

    public long Amount { get; set; }

    public DateTime RecordedAt { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }
}
