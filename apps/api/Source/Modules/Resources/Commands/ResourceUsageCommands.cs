using GameGuild.Messaging;
using GameGuild.Modules.Resources;

namespace GameGuild.Modules.Resources.Commands;

/// <summary>
/// Query to get usage records for a tenant
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="UsageType">Optional usage type filter</param>
/// <param name="StartDate">Optional start date filter</param>
/// <param name="EndDate">Optional end date filter</param>
public record GetUsageRecordsQuery(
    Guid TenantId,
    ResourceUsageType? UsageType = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null) : IRequest<Result<IEnumerable<ResourceUsageRecord>>>;

/// <summary>
/// Query to get current usage summary for a tenant
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
public record GetCurrentUsageSummaryQuery(Guid TenantId) : IRequest<Result<Dictionary<ResourceUsageType, long>>>;

/// <summary>
/// Query to check usage limits for a tenant
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="UsageType">Optional usage type filter</param>
public record CheckUsageLimitsQuery(
    Guid TenantId,
    ResourceUsageType? UsageType = null) : IRequest<Result<Dictionary<ResourceUsageType, ResourceQuotaStatus>>>;

/// <summary>
/// Command to record usage for a tenant
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="UsageType">Type of resource usage</param>
/// <param name="Count">Usage count</param>
/// <param name="Source">Source of the usage</param>
/// <param name="UserId">User who generated the usage (optional)</param>
/// <param name="ResourceId">Resource identifier (optional)</param>
/// <param name="Metadata">Additional metadata (optional)</param>
public record RecordUsageCommand(
    Guid TenantId,
    ResourceUsageType UsageType,
    long Count,
    string? Source = null,
    Guid? UserId = null,
    Guid? ResourceId = null,
    string? Metadata = null) : IRequest<Result<ResourceUsageRecord>>;

/// <summary>
/// Resource quota status result
/// </summary>
/// <param name="CurrentUsage">Current usage count</param>
/// <param name="SoftLimit">Soft limit (warning threshold)</param>
/// <param name="HardLimit">Hard limit (enforcement threshold)</param>
/// <param name="IsWithinLimits">Whether usage is within limits</param>
/// <param name="PercentageUsed">Percentage of hard limit used</param>
public record ResourceQuotaStatus(
    long CurrentUsage,
    long? SoftLimit,
    long? HardLimit,
    bool IsWithinLimits,
    double PercentageUsed);