
namespace GameGuild.Resources;

/// <summary>
///     Repository interface for managing resource metadata
/// </summary>
public interface IResourceMetadataRepository
{
    Task<ResourceMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ResourceMetadata?> GetByKeyAsync(Guid tenantId, string key, CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceMetadata>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceMetadata>> GetByCategoryAsync(Guid tenantId, string category, CancellationToken cancellationToken = default);

    Task<ResourceMetadata> CreateAsync(ResourceMetadata metadata, CancellationToken cancellationToken = default);

    Task<ResourceMetadata> UpdateAsync(ResourceMetadata metadata, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> DeleteByKeyAsync(Guid tenantId, string key, CancellationToken cancellationToken = default);

    // User-level methods
    Task<ResourceMetadata?> GetByUserKeyAsync(Guid userId, string key, CancellationToken cancellationToken = default);

    Task<IEnumerable<ResourceMetadata>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
