using GameGuild.Modules.TestingLab.Dtos;

namespace GameGuild.Modules.TestingLab.Abstractions;

/// <summary>
/// Service interface for TestingLabSettings operations
/// Supports both global settings (tenantId = null) and tenant-specific settings
/// </summary>
public interface ITestingLabSettingsService {
  /// <summary>
  /// Get testing lab settings for a specific tenant or global settings
  /// Creates default settings if none exist
  /// </summary>
  /// <param name="tenantId">Tenant ID for tenant-specific settings, or null for global settings</param>
  Task<TestingLabSettings> GetTestingLabSettingsAsync(Guid? tenantId = null);

  /// <summary>
  /// Get testing lab settings as DTO for a specific tenant or global settings
  /// Creates default settings if none exist
  /// </summary>
  /// <param name="tenantId">Tenant ID for tenant-specific settings, or null for global settings</param>
  Task<TestingLabSettingsDto> GetTestingLabSettingsDtoAsync(Guid? tenantId = null);

  /// <summary>
  /// Create or update testing lab settings for a specific tenant or global settings
  /// </summary>
  /// <param name="tenantId">Tenant ID for tenant-specific settings, or null for global settings</param>
  Task<TestingLabSettings> CreateOrUpdateTestingLabSettingsAsync(Guid? tenantId, CreateTestingLabSettingsDto dto);

  /// <summary>
  /// Update testing lab settings (partial update) for a specific tenant or global settings
  /// </summary>
  /// <param name="tenantId">Tenant ID for tenant-specific settings, or null for global settings</param>
  Task<TestingLabSettings> UpdateTestingLabSettingsAsync(Guid? tenantId, UpdateTestingLabSettingsDto dto);

  /// <summary>
  /// Reset testing lab settings to default values for a specific tenant or global settings
  /// </summary>
  /// <param name="tenantId">Tenant ID for tenant-specific settings, or null for global settings</param>
  Task<TestingLabSettings> ResetTestingLabSettingsAsync(Guid? tenantId = null);

  /// <summary>
  /// Check if testing lab settings exist for a specific tenant or global settings
  /// </summary>
  /// <param name="tenantId">Tenant ID for tenant-specific settings, or null for global settings</param>
  Task<bool> TestingLabSettingsExistAsync(Guid? tenantId = null);
}
