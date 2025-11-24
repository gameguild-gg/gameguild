using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Abstractions;

/// <summary>
///     Repository interface for tenant settings data access operations
/// </summary>
public interface ITenantSettingsRepository
{
    /// <summary>
    ///     Get tenant settings by tenant ID
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant settings or null if not found</returns>
    Task<TenantSettings?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Create new tenant settings
    /// </summary>
    /// <param name="settings">The tenant settings to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created tenant settings</returns>
    Task<TenantSettings> CreateAsync(TenantSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update existing tenant settings
    /// </summary>
    /// <param name="settings">The tenant settings to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated tenant settings</returns>
    Task<TenantSettings> UpdateAsync(TenantSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete tenant settings
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
