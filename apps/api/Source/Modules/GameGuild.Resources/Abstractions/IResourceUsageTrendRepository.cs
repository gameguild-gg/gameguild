
namespace GameGuild.Resources;

/// <summary>
///     Repository interface for resource usage trends
/// </summary>
public interface IResourceUsageTrendRepository
{
    Task<ResourceUsageTrend?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceUsageTrend>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceUsageTrend>> GetByTenantAsync(Guid tenantId, ResourceUsageType? type, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceUsageTrend>> GetByTenantAndTypeAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    Task<ResourceUsageTrend> CreateAsync(ResourceUsageTrend trend, CancellationToken cancellationToken = default);

    Task<ResourceUsageTrend> AddAsync(ResourceUsageTrend trend, CancellationToken cancellationToken = default);

    Task<ResourceUsageTrend> UpdateAsync(ResourceUsageTrend trend, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
