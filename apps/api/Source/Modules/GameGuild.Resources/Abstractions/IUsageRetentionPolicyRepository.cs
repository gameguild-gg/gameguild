
namespace GameGuild.Resources;

/// <summary>
///     Repository interface for usage retention policies
/// </summary>
public interface IUsageRetentionPolicyRepository
{
    Task<UsageRetentionPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UsageRetentionPolicy?> GetByTenantAndTypeAsync(Guid? tenantId, ResourceUsageType? type, CancellationToken cancellationToken = default);

    Task<IEnumerable<UsageRetentionPolicy>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<UsageRetentionPolicy> CreateAsync(UsageRetentionPolicy policy, CancellationToken cancellationToken = default);

    Task<UsageRetentionPolicy> AddAsync(UsageRetentionPolicy policy, CancellationToken cancellationToken = default);

    Task<UsageRetentionPolicy> UpdateAsync(UsageRetentionPolicy policy, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<UsageRetentionPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default);
}
