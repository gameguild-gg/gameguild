using GameGuild.Core.Cqrs;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Query to get aggregated statistics for a tenant
/// </summary>
public sealed record GetTenantStatisticsQuery(Guid TenantId) : IQuery<Result<TenantStatisticsDto>>;

/// <summary>
///     DTO for tenant statistics
/// </summary>
public sealed record TenantStatisticsDto
{
    public Guid TenantId { get; init; }
    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
    public int TotalMembers { get; init; }
    public int ActiveMembers { get; init; }
    public int TotalDomains { get; init; }
    public long StorageUsedBytes { get; init; }
    public decimal StorageUsedMB { get; init; }
    public decimal StorageUsedGB { get; init; }
    public long TotalApiCalls { get; init; }
    public int ActiveSubscriptions { get; init; }
    public DateTime LastUpdatedAt { get; init; }
}
