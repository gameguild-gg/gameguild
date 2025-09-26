namespace GameGuild.Modules.Tenants;

/// <summary>
///     Repository interface for tenant settings data access operations
///     Follows hexagonal architecture principles as a port (interface)
/// </summary>
public interface ITenantSettingsRepository
{
    /// <summary>
    ///     Get tenant settings by tenant ID
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant settings or null if not found</returns>
    Task<TenantSettings?> GetTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Create new tenant settings
    /// </summary>
    /// <param name="settings">The settings to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created tenant settings</returns>
    Task<TenantSettings> CreateTenantSettingsAsync(TenantSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update existing tenant settings
    /// </summary>
    /// <param name="settings">The settings to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated tenant settings</returns>
    Task<TenantSettings> UpdateTenantSettingsAsync(TenantSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Create or update tenant settings
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="settings">The settings to create or update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created or updated tenant settings</returns>
    Task<TenantSettings> CreateOrUpdateTenantSettingsAsync(Guid tenantId, TenantSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete tenant settings
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if settings were deleted, false if not found</returns>
    Task<bool> DeleteTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all tenant settings (for administrative purposes)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all tenant settings</returns>
    Task<IReadOnlyList<TenantSettings>> GetAllTenantSettingsAsync(CancellationToken cancellationToken = default);
}