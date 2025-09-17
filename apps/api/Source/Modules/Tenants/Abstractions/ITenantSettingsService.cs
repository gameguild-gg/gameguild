namespace GameGuild.Modules.Tenants;

/// <summary> Service interface for managing tenant settings Provides CRUD operations and helper methods for tenant-specific configuration </summary>
public interface ITenantSettingsService {
  // === BASIC CRUD OPERATIONS ===

  /// <summary> Get tenant settings by tenant ID (creates default if none exist) </summary>
  /// <param name="tenantId"> Tenant ID (null for global default settings) </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  /// <returns> Tenant settings </returns>
  Task<TenantSettings> GetTenantSettingsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);

  /// <summary> Create or update tenant settings </summary>
  /// <param name="tenantId"> Tenant ID (null for global default settings) </param>
  /// <param name="settings"> Settings to save </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  /// <returns> Updated settings </returns>
  Task<TenantSettings> SaveTenantSettingsAsync(Guid? tenantId, TenantSettings settings, CancellationToken cancellationToken = default);

  /// <summary> Update specific tenant settings fields </summary>
  /// <param name="tenantId"> Tenant ID (null for global default settings) </param>
  /// <param name="updates"> Dictionary of field updates </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  /// <returns> Updated settings </returns>
  Task<TenantSettings> UpdateTenantSettingsAsync(Guid? tenantId, Dictionary<string, object> updates, CancellationToken cancellationToken = default);

  /// <summary> Reset tenant settings to defaults </summary>
  /// <param name="tenantId"> Tenant ID (null for global default settings) </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  /// <returns> Reset settings </returns>
  Task<TenantSettings> ResetTenantSettingsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);

  /// <summary> Delete tenant settings (will fall back to defaults) </summary>
  /// <param name="tenantId"> Tenant ID </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  Task DeleteTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default);

  // === FEATURE FLAGS ===

  /// <summary> Get feature flag value for tenant </summary>
  /// <param name="tenantId"> Tenant ID (null for global default) </param>
  /// <param name="key"> Feature flag key </param>
  /// <param name="defaultValue"> Default value if not found </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  /// <returns> Feature flag value </returns>
  Task<bool> GetFeatureFlagAsync(Guid? tenantId, string key, bool defaultValue = false, CancellationToken cancellationToken = default);

  /// <summary> Set feature flag value for tenant </summary>
  /// <param name="tenantId"> Tenant ID (null for global default) </param>
  /// <param name="key"> Feature flag key </param>
  /// <param name="value"> Feature flag value </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  Task SetFeatureFlagAsync(Guid? tenantId, string key, bool value, CancellationToken cancellationToken = default);

  /// <summary> Get all feature flags for tenant </summary>
  /// <param name="tenantId"> Tenant ID (null for global default) </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  /// <returns> Dictionary of feature flags </returns>
  Task<Dictionary<string, bool>> GetAllFeatureFlagsAsync(Guid? tenantId, CancellationToken cancellationToken = default);

  // === MODULE SETTINGS ===

  /// <summary> Get module setting value for tenant </summary>
  /// <param name="tenantId"> Tenant ID (null for global default) </param>
  /// <param name="module"> Module name </param>
  /// <param name="key"> Setting key </param>
  /// <param name="defaultValue"> Default value if not found </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  /// <returns> Setting value </returns>
  Task<T?> GetModuleSettingAsync<T>(Guid? tenantId, string module, string key, T? defaultValue = default, CancellationToken cancellationToken = default);

  /// <summary> Set module setting value for tenant </summary>
  /// <param name="tenantId"> Tenant ID (null for global default) </param>
  /// <param name="module"> Module name </param>
  /// <param name="key"> Setting key </param>
  /// <param name="value"> Setting value </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  Task SetModuleSettingAsync<T>(Guid? tenantId, string module, string key, T value, CancellationToken cancellationToken = default);

  /// <summary> Get all settings for a specific module </summary>
  /// <param name="tenantId"> Tenant ID (null for global default) </param>
  /// <param name="module"> Module name </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  /// <returns> Dictionary of module settings </returns>
  Task<Dictionary<string, object>> GetModuleSettingsAsync(Guid? tenantId, string module, CancellationToken cancellationToken = default);

  // === UTILITY METHODS ===

  /// <summary> Check if tenant settings exist </summary>
  /// <param name="tenantId"> Tenant ID (null for global default) </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  /// <returns> True if settings exist </returns>
  Task<bool> TenantSettingsExistAsync(Guid? tenantId, CancellationToken cancellationToken = default);

  /// <summary> Get effective settings for tenant (with fallback to global defaults) </summary>
  /// <param name="tenantId"> Tenant ID </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  /// <returns> Effective tenant settings </returns>
  Task<TenantSettings> GetEffectiveTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default);

  /// <summary> Clone settings from one tenant to another </summary>
  /// <param name="sourceTenantId"> Source tenant ID (null for global defaults) </param>
  /// <param name="targetTenantId"> Target tenant ID </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  /// <returns> Cloned settings </returns>
  Task<TenantSettings> CloneTenantSettingsAsync(Guid? sourceTenantId, Guid targetTenantId, CancellationToken cancellationToken = default);
}
