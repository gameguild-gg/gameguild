
namespace GameGuild.Resources;

/// <summary>
///     Repository interface for managing resource settings
/// </summary>
public interface IResourceSettingsRepository
{
    Task<ResourceSettings?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ResourceSettings?> GetByKeyAsync(Guid tenantId, string key, CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceSettings>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceSettings>> GetByCategoryAsync(Guid tenantId, string category, CancellationToken cancellationToken = default);

    Task<ResourceSettings> CreateAsync(ResourceSettings settings, CancellationToken cancellationToken = default);

    Task<ResourceSettings> UpdateAsync(ResourceSettings settings, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> DeleteByKeyAsync(Guid tenantId, string key, CancellationToken cancellationToken = default);

    // User-level methods
    Task<ResourceSettings?> GetByUserKeyAsync(Guid userId, string key, CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceSettings>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<string?> GetEffectiveValueAsync(Guid tenantId, string key, Guid? userId = null, CancellationToken cancellationToken = default);
}
