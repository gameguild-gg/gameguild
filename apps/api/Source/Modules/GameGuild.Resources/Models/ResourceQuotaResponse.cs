namespace GameGuild.Resources;

/// <summary>
///     Response model for resource quota information
/// </summary>
public class ResourceQuotaResponse
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public ResourceUsageType Type { get; set; }

    public long Limit { get; set; }

    public long CurrentUsage { get; set; }

    public long RemainingQuota { get; set; }

    public decimal UsagePercentage { get; set; }

    public decimal SoftLimitPercentage { get; set; }

    public bool IsActive { get; set; }

    public ResourceQuotaPeriod Period { get; set; }

    public DateTime LastResetDate { get; set; }

    public DateTime NextResetDate { get; set; }

    public string? Description { get; set; }

    public bool IsSoftLimitExceeded { get; set; }

    public bool IsHardLimitExceeded { get; set; }

    public bool ShouldReset { get; set; }

    public long? SoftLimit { get; set; }

    public long? HardLimit { get; set; }
}
