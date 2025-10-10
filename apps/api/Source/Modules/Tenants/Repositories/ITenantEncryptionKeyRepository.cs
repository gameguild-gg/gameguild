using GameGuild.Modules.Tenants;
using GameGuild.Modules.Tenants.Enums;

namespace GameGuild.Modules.Tenants.Repositories;

public interface ITenantEncryptionKeyRepository
{
    Task<TenantEncryptionKey?> GetByIdAsync(Guid keyId, CancellationToken cancellationToken = default);

    Task<TenantEncryptionKey?> GetActiveKeyAsync(Guid tenantId, TenantKeyPurpose purpose, CancellationToken cancellationToken = default);

    Task<List<TenantEncryptionKey>> GetKeyHistoryAsync(Guid tenantId, TenantKeyPurpose purpose, CancellationToken cancellationToken = default);

    Task<TenantEncryptionKey> CreateAsync(TenantEncryptionKey key, CancellationToken cancellationToken = default);

    Task<TenantEncryptionKey> UpdateAsync(TenantEncryptionKey key, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid keyId, CancellationToken cancellationToken = default);

    Task<bool> KeyNameExistsAsync(Guid tenantId, string keyName, CancellationToken cancellationToken = default);
}
