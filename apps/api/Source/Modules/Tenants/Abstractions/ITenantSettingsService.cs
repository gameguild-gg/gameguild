namespace GameGuild.Modules.Tenants;

/// <summary>
///     Service interface for tenant settings management operations
///     Follows hexagonal architecture principles as a port (interface)
/// </summary>
public interface ITenantSettingsService
{
    /// <summary>
    ///     Get tenant settings by tenant ID
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant settings or null if not found</returns>
    Task<TenantSettings?> GetTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update tenant settings
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="settings">The settings to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated tenant settings</returns>
    Task<TenantSettings> UpdateTenantSettingsAsync(Guid tenantId, TenantSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Create default tenant settings for a new tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created default tenant settings</returns>
    Task<TenantSettings> CreateDefaultTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete tenant settings
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if settings were deleted, false if not found</returns>
    Task<bool> DeleteTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Reset tenant settings to default values
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The reset tenant settings</returns>
    Task<TenantSettings> ResetTenantSettingsToDefaultAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Validate tenant settings before saving
    /// </summary>
    /// <param name="settings">The settings to validate</param>
    /// <returns>Validation result with any errors</returns>
    Task<ValidationResult> ValidateTenantSettingsAsync(TenantSettings settings);

    /// <summary>
    ///     Get all tenant settings (for administrative purposes)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all tenant settings</returns>
    Task<IReadOnlyList<TenantSettings>> GetAllTenantSettingsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Validation result for tenant settings
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Failure(params string[] errors) => new() { IsValid = false, Errors = errors.ToList() };
}