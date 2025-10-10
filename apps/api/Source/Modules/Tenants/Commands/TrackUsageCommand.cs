using GameGuild.Modules.Tenants;
using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants.Commands;

/// <summary>
///     Command to track resource usage for a tenant
/// </summary>
public record TrackUsageCommand(
    Guid TenantId,
    ResourceType ResourceType,
    long Amount,
    string? CustomResourceName = null) : IRequest<Result<UsageTrackingDto>>;

/// <summary>
///     DTO for usage tracking information
/// </summary>
public record UsageTrackingDto(
    Guid Id,
    Guid TenantId,
    ResourceType ResourceType,
    string? CustomResourceName,
    long CurrentUsage,
    long UsageLimit,
    string Unit,
    bool IsLimitExceeded,
    decimal UsagePercentage,
    long RemainingCapacity,
    DateTime LastUpdatedAt,
    DateTime PeriodStartedAt);
