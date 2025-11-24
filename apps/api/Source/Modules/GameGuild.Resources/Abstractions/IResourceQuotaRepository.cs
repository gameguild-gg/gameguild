using GameGuild.Resources.Entities;
using GameGuild.Resources.Models;

namespace GameGuild.Resources.Abstractions;

/// <summary>
///     Repository interface for managing resource quotas
/// </summary>
public interface IResourceQuotaRepository
{
    Task<ResourceQuota?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ResourceQuota?> GetByTenantAndTypeAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceQuota>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<ResourceQuota> CreateAsync(ResourceQuota quota, CancellationToken cancellationToken = default);

    Task<ResourceQuota> UpdateAsync(ResourceQuota quota, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceQuota>> GetActiveQuotasAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<Guid>> GetTenantsExceedingLimitsAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceQuota>> GetQuotasExceedingLimitsAsync(ResourceUsageType? type = null, bool softLimitOnly = false, CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceQuota>> GetQuotasDueForResetAsync(CancellationToken cancellationToken = default);
}
