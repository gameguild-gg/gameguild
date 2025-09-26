using GameGuild.Modules.Localization;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Service implementation for tenant settings management operations
///     Follows hexagonal architecture principles as an adapter (implementation)
/// </summary>
public class TenantSettingsService(
    ITenantSettingsRepository repository,
    ITenantCacheService cacheService,
    ILanguageRepository languageRepository,
    ILogger<TenantSettingsService> logger) : ITenantSettingsService
{
    public async Task<TenantSettings?> GetTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting tenant settings for tenant: {TenantId}", tenantId);

        // Try cache first
        TenantSettings? cachedSettings = cacheService.GetTenantSettings(tenantId);
        if (cachedSettings != null)
        {
            logger.LogDebug("Found tenant settings in cache for tenant: {TenantId}", tenantId);
            return cachedSettings;
        }

        // Fallback to database
        TenantSettings? settings = await repository.GetTenantSettingsAsync(tenantId, cancellationToken);

        if (settings != null)
        {
            logger.LogDebug("Found tenant settings in database for tenant: {TenantId}", tenantId);
        }
        else
        {
            logger.LogDebug("Tenant settings not found for tenant: {TenantId}", tenantId);
        }

        return settings;
    }

    public async Task<TenantSettings> UpdateTenantSettingsAsync(Guid tenantId, TenantSettings settings, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Updating tenant settings for tenant: {TenantId}", tenantId);

        // Validate settings
        ValidationResult validationResult = await ValidateTenantSettingsAsync(settings);
        if (!validationResult.IsValid)
        {
            string errors = string.Join(", ", validationResult.Errors);
            throw new ArgumentException($"Invalid tenant settings: {errors}");
        }

        // Update settings
        settings = await repository.CreateOrUpdateTenantSettingsAsync(tenantId, settings, cancellationToken);

        // Refresh cache
        await cacheService.RefreshTenantSettingsAsync(tenantId, cancellationToken);

        logger.LogInformation("Updated tenant settings for tenant: {TenantId}", tenantId);

        return settings;
    }

    public async Task<TenantSettings> CreateDefaultTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Creating default tenant settings for tenant: {TenantId}", tenantId);

        // Get default language
        Language? defaultLanguage = await languageRepository.GetDefaultAsync(cancellationToken);
        Guid defaultLanguageId = defaultLanguage?.Id ?? Guid.Empty;

        // Create default settings
        TenantSettings defaultSettings = TenantSettings.CreateDefault(tenantId, defaultLanguageId);

        // Save settings
        TenantSettings createdSettings = await repository.CreateTenantSettingsAsync(defaultSettings, cancellationToken);

        // Refresh cache
        await cacheService.RefreshTenantSettingsAsync(tenantId, cancellationToken);

        logger.LogInformation("Created default tenant settings for tenant: {TenantId}", tenantId);

        return createdSettings;
    }

    public async Task<bool> DeleteTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Deleting tenant settings for tenant: {TenantId}", tenantId);

        bool deleted = await repository.DeleteTenantSettingsAsync(tenantId, cancellationToken);

        if (deleted)
        {
            // Clear cache
            cacheService.ClearTenantSettings(tenantId);
            logger.LogInformation("Deleted tenant settings for tenant: {TenantId}", tenantId);
        }
        else
        {
            logger.LogDebug("Tenant settings not found for deletion, tenant: {TenantId}", tenantId);
        }

        return deleted;
    }

    public async Task<TenantSettings> ResetTenantSettingsToDefaultAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Resetting tenant settings to default for tenant: {TenantId}", tenantId);

        // Delete existing settings
        await DeleteTenantSettingsAsync(tenantId, cancellationToken);

        // Create new default settings
        TenantSettings defaultSettings = await CreateDefaultTenantSettingsAsync(tenantId, cancellationToken);

        logger.LogInformation("Reset tenant settings to default for tenant: {TenantId}", tenantId);

        return defaultSettings;
    }

    public async Task<ValidationResult> ValidateTenantSettingsAsync(TenantSettings settings)
    {
        List<string> errors = new();

        // Validate required fields
        if (settings.TenantId == Guid.Empty)
        {
            errors.Add("TenantId is required");
        }

        // Validate default language exists
        if (settings.DefaultLanguageId != Guid.Empty)
        {
            Language? language = await languageRepository.GetByIdAsync(settings.DefaultLanguageId);
            if (language == null)
            {
                errors.Add("Default language does not exist");
            }
        }

        // Validate timezone
        if (!string.IsNullOrWhiteSpace(settings.TimeZone))
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                errors.Add("Invalid timezone");
            }
        }

        // Validate MaxUsersLimit
        if (settings.MaxUsersLimit.HasValue && settings.MaxUsersLimit <= 0)
        {
            errors.Add("MaxUsersLimit must be greater than 0 if specified");
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
    }

    public async Task<IReadOnlyList<TenantSettings>> GetAllTenantSettingsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting all tenant settings");

        IReadOnlyList<TenantSettings> allSettings = await repository.GetAllTenantSettingsAsync(cancellationToken);

        logger.LogDebug("Retrieved {Count} tenant settings", allSettings.Count);

        return allSettings;
    }
}