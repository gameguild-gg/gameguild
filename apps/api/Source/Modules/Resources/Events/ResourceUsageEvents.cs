namespace GameGuild.Modules.Resources.Events;

/// <summary>
/// Event published when usage is recorded (for billing integration)
/// </summary>
public class UsageRecordedEvent
{
    public Guid RecordId { get; set; }
    public Guid TenantId { get; set; }
    public ResourceUsageType Type { get; set; }
    public long Count { get; set; }
    public string? Source { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ResourceId { get; set; }
    public string? Metadata { get; set; }
    public DateTime RecordedAt { get; set; }
    public long CumulativeUsage { get; set; }
    public long? RemainingQuota { get; set; }
    public bool IsOverLimit { get; set; }
}

/// <summary>
/// Event published when quota is reset
/// </summary>
public class QuotaResetEvent
{
    public Guid QuotaId { get; set; }
    public Guid TenantId { get; set; }
    public ResourceUsageType UsageType { get; set; }
    public long PreviousUsage { get; set; }
    public DateTime ResetAt { get; set; }
    public ResourceQuotaPeriod Period { get; set; }
}

/// <summary>
/// Event published when quota threshold is exceeded
/// </summary>
public class QuotaThresholdExceededEvent
{
    public Guid QuotaId { get; set; }
    public Guid TenantId { get; set; }
    public ResourceUsageType UsageType { get; set; }
    public long CurrentUsage { get; set; }
    public long HardLimit { get; set; }
    public double Percentage { get; set; }
    public double Threshold { get; set; }
    public DateTime DetectedAt { get; set; }
}

/// <summary>
/// Event published when quota hard limit is exceeded
/// </summary>
public class QuotaHardLimitExceededEvent
{
    public Guid QuotaId { get; set; }
    public Guid TenantId { get; set; }
    public ResourceUsageType UsageType { get; set; }
    public long CurrentUsage { get; set; }
    public long HardLimit { get; set; }
    public DateTime ExceededAt { get; set; }
    public string? BlockedOperation { get; set; }
}

/// <summary>
/// Event published when usage records are archived
/// </summary>
public class UsageRecordsArchivedEvent
{
    public int ArchivedCount { get; set; }
    public DateTime CutoffDate { get; set; }
    public DateTime ArchivedAt { get; set; }
}
