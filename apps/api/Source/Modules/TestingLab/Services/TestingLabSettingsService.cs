using GameGuild.Database;
using GameGuild.Modules.TestingLab.Dtos;


namespace GameGuild.Modules.TestingLab.Services;

/// <summary> Implementation of the TestingLabSettings service </summary>
internal class TestingLabSettingsService : ITestingLabSettingsService {
  private readonly ApplicationDbContext _dbContext;

  public TestingLabSettingsService(ApplicationDbContext dbContext) { _dbContext = dbContext; }

  /// <inheritdoc />
  public async Task<TestingLabSettings> GetTestingLabSettingsAsync(Guid? tenantId = null) {
    var settings = await _dbContext.TestingLabSettings
                                   .Include(s => s.Tenant)
                                   .FirstOrDefaultAsync(s => tenantId == null
                                                               ? s.Tenant == null
                                                               : s.Tenant != null && s.Tenant.Id == tenantId
                                   )
                                   .ConfigureAwait(false);

    if (settings == null) {
      // Create default settings
      settings = new TestingLabSettings {
        Tenant = tenantId.HasValue ? await _dbContext.Tenants.FindAsync(tenantId.Value).ConfigureAwait(false) : null,
        LabName = "Testing Lab",
        Description = "Default testing lab settings",
        Timezone = "UTC",
        DefaultSessionDuration = 60,
        AllowPublicSignups = true,
        RequireApproval = true,
        EnableNotifications = true,
        MaxSimultaneousSessions = 10,
      };

      _dbContext.TestingLabSettings.Add(settings);
      await _dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    return settings;
  }

  /// <inheritdoc />
  public async Task<TestingLabSettingsDto> GetTestingLabSettingsDtoAsync(Guid? tenantId = null) {
    var settings = await GetTestingLabSettingsAsync(tenantId).ConfigureAwait(false);

    return MapToDto(settings);
  }

  /// <inheritdoc />
  public async Task<TestingLabSettings> CreateOrUpdateTestingLabSettingsAsync(Guid? tenantId, CreateTestingLabSettingsDto dto) {
    ArgumentNullException.ThrowIfNull(dto);

    var settings = await _dbContext.TestingLabSettings
                                   .Include(s => s.Tenant)
                                   .FirstOrDefaultAsync(s => tenantId == null
                                                               ? s.Tenant == null
                                                               : s.Tenant != null && s.Tenant.Id == tenantId
                                   )
                                   .ConfigureAwait(false);

    if (settings == null) {
      // Create new settings
      settings = new TestingLabSettings {
        Tenant = tenantId.HasValue ? await _dbContext.Tenants.FindAsync(tenantId.Value).ConfigureAwait(false) : null,
      };
      _dbContext.TestingLabSettings.Add(settings);
    }

    // Update properties
    settings.LabName = dto.LabName;
    settings.Description = dto.Description;
    settings.Timezone = dto.Timezone;
    settings.DefaultSessionDuration = dto.DefaultSessionDuration;
    settings.AllowPublicSignups = dto.AllowPublicSignups;
    settings.RequireApproval = dto.RequireApproval;
    settings.EnableNotifications = dto.EnableNotifications;
    settings.MaxSimultaneousSessions = dto.MaxSimultaneousSessions;

    await _dbContext.SaveChangesAsync().ConfigureAwait(false);

    return settings;
  }

  /// <inheritdoc />
  public async Task<TestingLabSettings> UpdateTestingLabSettingsAsync(Guid? tenantId, UpdateTestingLabSettingsDto dto) {
    ArgumentNullException.ThrowIfNull(dto);

    var settings = await GetTestingLabSettingsAsync(tenantId).ConfigureAwait(false);

    // Update only non-null properties
    if (dto.LabName != null) settings.LabName = dto.LabName;

    if (dto.Description != null) settings.Description = dto.Description;

    if (dto.Timezone != null) settings.Timezone = dto.Timezone;

    if (dto.DefaultSessionDuration.HasValue) settings.DefaultSessionDuration = dto.DefaultSessionDuration.Value;

    if (dto.AllowPublicSignups.HasValue) settings.AllowPublicSignups = dto.AllowPublicSignups.Value;

    if (dto.RequireApproval.HasValue) settings.RequireApproval = dto.RequireApproval.Value;

    if (dto.EnableNotifications.HasValue) settings.EnableNotifications = dto.EnableNotifications.Value;

    if (dto.MaxSimultaneousSessions.HasValue) settings.MaxSimultaneousSessions = dto.MaxSimultaneousSessions.Value;

    await _dbContext.SaveChangesAsync().ConfigureAwait(false);

    return settings;
  }

  /// <inheritdoc />
  public async Task<TestingLabSettings> ResetTestingLabSettingsAsync(Guid? tenantId = null) {
    var settings = await _dbContext.TestingLabSettings
                                   .Include(s => s.Tenant)
                                   .FirstOrDefaultAsync(s => tenantId == null
                                                               ? s.Tenant == null
                                                               : s.Tenant != null && s.Tenant.Id == tenantId
                                   )
                                   .ConfigureAwait(false);

    if (settings != null) {
      // Reset to defaults
      settings.LabName = "Testing Lab";
      settings.Description = "Default testing lab settings";
      settings.Timezone = "UTC";
      settings.DefaultSessionDuration = 60;
      settings.AllowPublicSignups = true;
      settings.RequireApproval = true;
      settings.EnableNotifications = true;
      settings.MaxSimultaneousSessions = 10;

      await _dbContext.SaveChangesAsync().ConfigureAwait(false);

      return settings;
    }

    // If no settings exist, create default ones
    return await GetTestingLabSettingsAsync(tenantId).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task<bool> TestingLabSettingsExistAsync(Guid? tenantId = null) {
    return await _dbContext.TestingLabSettings
                           .Include(s => s.Tenant)
                           .AnyAsync(s => tenantId == null
                                            ? s.Tenant == null
                                            : s.Tenant != null && s.Tenant.Id == tenantId
                           )
                           .ConfigureAwait(false);
  }

  // Helper method to map entity to DTO
  private static TestingLabSettingsDto MapToDto(TestingLabSettings settings) {
    return new TestingLabSettingsDto {
      Id = settings.Id,
      LabName = settings.LabName,
      Description = settings.Description,
      Timezone = settings.Timezone,
      DefaultSessionDuration = settings.DefaultSessionDuration,
      AllowPublicSignups = settings.AllowPublicSignups,
      RequireApproval = settings.RequireApproval,
      EnableNotifications = settings.EnableNotifications,
      MaxSimultaneousSessions = settings.MaxSimultaneousSessions,
      TenantId = settings.Tenant?.Id,
      CreatedAt = settings.CreatedAt,
      UpdatedAt = settings.UpdatedAt,
    };
  }
}
