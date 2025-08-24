namespace GameGuild.Modules.TestingLab.Dtos;

/// <summary> Data Transfer Object for creating TestingLabSettings </summary>
public class CreateTestingLabSettingsDto {
  /// <summary> Name of the testing lab </summary>
  [Required]
  [MaxLength(255)]
  public string LabName { get; set; } = "Testing Lab";

  /// <summary> Description of the testing lab </summary>
  [MaxLength(1000)]
  public string? Description { get; set; }

  /// <summary> Timezone for the testing lab (e.g., "UTC", "America/New_York") </summary>
  [Required]
  [MaxLength(50)]
  public string Timezone { get; set; } = "UTC";

  /// <summary> Default session duration in minutes </summary>
  [Required]
  [Range(15, 480)] // 15 minutes to 8 hours
  public int DefaultSessionDuration { get; set; } = 60;

  /// <summary> Whether to allow public signups for testing sessions </summary>
  public bool AllowPublicSignups { get; set; } = true;

  /// <summary> Whether manager approval is required for new testing participants </summary>
  public bool RequireApproval { get; set; } = true;

  /// <summary> Whether to enable email notifications for session updates </summary>
  public bool EnableNotifications { get; set; } = true;

  /// <summary> Maximum number of simultaneous sessions allowed </summary>
  [Required]
  [Range(1, 100)]
  public int MaxSimultaneousSessions { get; set; } = 10;
}

/// <summary> Data Transfer Object for updating TestingLabSettings </summary>
public class UpdateTestingLabSettingsDto {
  /// <summary> Name of the testing lab </summary>
  [MaxLength(255)]
  public string? LabName { get; set; }

  /// <summary> Description of the testing lab </summary>
  [MaxLength(1000)]
  public string? Description { get; set; }

  /// <summary> Timezone for the testing lab (e.g., "UTC", "America/New_York") </summary>
  [MaxLength(50)]
  public string? Timezone { get; set; }

  /// <summary> Default session duration in minutes </summary>
  [Range(15, 480)] // 15 minutes to 8 hours
  public int? DefaultSessionDuration { get; set; }

  /// <summary> Whether to allow public signups for testing sessions </summary>
  public bool? AllowPublicSignups { get; set; }

  /// <summary> Whether manager approval is required for new testing participants </summary>
  public bool? RequireApproval { get; set; }

  /// <summary> Whether to enable email notifications for session updates </summary>
  public bool? EnableNotifications { get; set; }

  /// <summary> Maximum number of simultaneous sessions allowed </summary>
  [Range(1, 100)]
  public int? MaxSimultaneousSessions { get; set; }
}

/// <summary> Data Transfer Object for TestingLabSettings responses </summary>
public class TestingLabSettingsDto {
  /// <summary> Unique identifier for the settings </summary>
  public Guid Id { get; set; }

  /// <summary> Name of the testing lab </summary>
  public string LabName { get; set; } = string.Empty;

  /// <summary> Description of the testing lab </summary>
  public string? Description { get; set; }

  /// <summary> Timezone for the testing lab </summary>
  public string Timezone { get; set; } = string.Empty;

  /// <summary> Default session duration in minutes </summary>
  public int DefaultSessionDuration { get; set; }

  /// <summary> Whether to allow public signups for testing sessions </summary>
  public bool AllowPublicSignups { get; set; }

  /// <summary> Whether manager approval is required for new testing participants </summary>
  public bool RequireApproval { get; set; }

  /// <summary> Whether to enable email notifications for session updates </summary>
  public bool EnableNotifications { get; set; }

  /// <summary> Maximum number of simultaneous sessions allowed </summary>
  public int MaxSimultaneousSessions { get; set; }

  /// <summary> ID of the tenant this settings belongs to </summary>
  public Guid? TenantId { get; set; }

  /// <summary> Timestamp when the settings were created </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary> Timestamp when the settings were last updated </summary>
  public DateTime UpdatedAt { get; set; }
}
