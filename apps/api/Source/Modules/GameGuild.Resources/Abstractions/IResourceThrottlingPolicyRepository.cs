
namespace GameGuild.Resources;

/// <summary>
///     Repository interface for resource throttling policies
/// </summary>
public interface IResourceThrottlingPolicyRepository
{
    Task<ResourceThrottlingPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ResourceThrottlingPolicy?> GetByTenantAndTypeAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceThrottlingPolicy>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<ResourceThrottlingPolicy> CreateAsync(ResourceThrottlingPolicy policy, CancellationToken cancellationToken = default);

    Task<ResourceThrottlingPolicy> UpdateAsync(ResourceThrottlingPolicy policy, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceThrottlingPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceThrottlingPolicy>> GetActivePoliciesAsync(ResourceUsageType? type, CancellationToken cancellationToken = default);
}
