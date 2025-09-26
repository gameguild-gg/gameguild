using GameGuild.Database;
using GameGuild.Modules.Localization;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Repository implementation for tenant settings data access operations
///     Follows hexagonal architecture principles as an adapter (implementation)
/// </summary>
public class TenantSettingsRepository(ApplicationDbContext context, ILanguageRepository? languageRepository = null) : ITenantSettingsRepository
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILanguageRepository _languageRepository = languageRepository ?? new LanguageRepository(context);

    public async Task<TenantSettings?> GetTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.TenantSettings
            .FirstOrDefaultAsync(settings => settings.TenantId == tenantId, cancellationToken);
    }

    public async Task<TenantSettings> CreateTenantSettingsAsync(TenantSettings settings, CancellationToken cancellationToken = default)
    {
        // Ensure default language is set if not provided
        if (settings.DefaultLanguageId == Guid.Empty)
        {
            Language? defaultLanguage = await _languageRepository.GetDefaultAsync(cancellationToken);
            if (defaultLanguage != null)
            {
                settings.DefaultLanguageId = defaultLanguage.Id;
            }
        }

        settings.Id = Guid.NewGuid();
        settings.CreatedAt = DateTime.UtcNow;
        settings.UpdatedAt = DateTime.UtcNow;

        _ = _context.TenantSettings.Add(settings);
        _ = await _context.SaveChangesAsync(cancellationToken);

        return settings;
    }

    public async Task<TenantSettings> UpdateTenantSettingsAsync(TenantSettings settings, CancellationToken cancellationToken = default)
    {
        // Ensure default language is set if not provided
        if (settings.DefaultLanguageId == Guid.Empty)
        {
            Language? defaultLanguage = await _languageRepository.GetDefaultAsync(cancellationToken);
            if (defaultLanguage != null)
            {
                settings.DefaultLanguageId = defaultLanguage.Id;
            }
        }

        settings.UpdatedAt = DateTime.UtcNow;

        _ = _context.TenantSettings.Update(settings);
        _ = await _context.SaveChangesAsync(cancellationToken);

        return settings;
    }

    public async Task<TenantSettings> CreateOrUpdateTenantSettingsAsync(Guid tenantId, TenantSettings settings, CancellationToken cancellationToken = default)
    {
        settings.TenantId = tenantId;

        TenantSettings? existingSettings = await GetTenantSettingsAsync(tenantId, cancellationToken);

        if (existingSettings != null)
        {
            // Update existing settings
            existingSettings.DefaultLanguageId = settings.DefaultLanguageId;
            existingSettings.TimeZone = settings.TimeZone;
            existingSettings.DateFormat = settings.DateFormat;
            existingSettings.TimeFormat = settings.TimeFormat;
            existingSettings.Currency = settings.Currency;
            existingSettings.EnableRegistration = settings.EnableRegistration;
            existingSettings.RequireEmailVerification = settings.RequireEmailVerification;
            existingSettings.MaxUsersLimit = settings.MaxUsersLimit;
            existingSettings.IsMaintenanceMode = settings.IsMaintenanceMode;
            existingSettings.MaintenanceMessage = settings.MaintenanceMessage;

            return await UpdateTenantSettingsAsync(existingSettings, cancellationToken);
        }
        else
        {
            // Create new settings
            return await CreateTenantSettingsAsync(settings, cancellationToken);
        }
    }

    public async Task<bool> DeleteTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        TenantSettings? settings = await GetTenantSettingsAsync(tenantId, cancellationToken);

        if (settings == null)
        {
            return false;
        }

        _ = _context.TenantSettings.Remove(settings);
        int changesCount = await _context.SaveChangesAsync(cancellationToken);

        return changesCount > 0;
    }

    public async Task<IReadOnlyList<TenantSettings>> GetAllTenantSettingsAsync(CancellationToken cancellationToken = default)
    {
        List<TenantSettings> settings = await _context.TenantSettings
            .AsNoTracking()
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        return settings.AsReadOnly();
    }
}