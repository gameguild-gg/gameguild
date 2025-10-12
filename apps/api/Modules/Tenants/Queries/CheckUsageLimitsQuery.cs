using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants.Queries;

/// <summary>
///     Query to check if usage limits are exceeded for a tenant
/// </summary>
public record CheckUsageLimitsQuery(
    Guid TenantId,
    ResourceType? ResourceType = null) : IRequest<Result<UsageLimitsCheckDto>>;

/// <summary>
///     DTO for usage limits check result
/// </summary>
public record UsageLimitsCheckDto(
    Guid TenantId,
    bool AnyLimitExceeded,
    List<ResourceLimitStatus> ResourceStatuses);

/// <summary>
///     Status of a single resource limit
/// </summary>
public record ResourceLimitStatus(
    ResourceType ResourceType,
    string? CustomResourceName,
    bool IsExceeded,
    long CurrentUsage,
    long UsageLimit,
    decimal UsagePercentage);
