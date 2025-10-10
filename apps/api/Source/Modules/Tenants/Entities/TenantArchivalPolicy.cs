using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// Represents an archival policy for inactive tenants.
/// </summary>
[Table("TenantArchivalPolicies")]
[Index(nameof(TenantId), IsUnique = true)]
public class TenantArchivalPolicy
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Whether automatic archival is enabled for this tenant.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Number of days of inactivity before archival is triggered.
    /// </summary>
    public int InactivityThresholdDays { get; set; } = 90;

    /// <summary>
    /// Whether to send warning notifications before archival.
    /// </summary>
    public bool SendWarningNotifications { get; set; } = true;

    /// <summary>
    /// Days before archival to send warning (e.g., 7, 3, 1).
    /// </summary>
    [Column(TypeName = "jsonb")]
    public List<int> WarningDaysBeforeArchival { get; set; } = new() { 30, 7, 1 };

    /// <summary>
    /// Whether to automatically purge (delete) after archival.
    /// </summary>
    public bool AutoPurgeEnabled { get; set; } = false;

    /// <summary>
    /// Days to retain archived data before purging.
    /// </summary>
    public int PurgeRetentionDays { get; set; } = 365;

    /// <summary>
    /// Data to include in the archive.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public List<string> ArchiveDataTypes { get; set; } = new() { "Users", "Content", "Settings" };

    /// <summary>
    /// Metadata about the archival policy.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// When the policy was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the policy was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Enables the archival policy.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disables the archival policy.
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates the archival policy configuration.
    /// </summary>
    public void Validate()
    {
        if (InactivityThresholdDays < 1)
            throw new InvalidOperationException("Inactivity threshold must be at least 1 day");

        if (AutoPurgeEnabled && PurgeRetentionDays < 1)
            throw new InvalidOperationException("Purge retention must be at least 1 day");

        if (ArchiveDataTypes == null || ArchiveDataTypes.Count == 0)
            throw new InvalidOperationException("At least one data type must be specified for archival");
    }
}

/// <summary>
/// Represents an archived tenant record.
/// </summary>
[Table("TenantArchiveRecords")]
[Index(nameof(TenantId), IsUnique = false)]
[Index(nameof(ArchivalStatus), IsUnique = false)]
public class TenantArchiveRecord
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Reason for archival.
    /// </summary>
    [Required]
    public TenantArchivalReason Reason { get; set; }

    /// <summary>
    /// Current status of the archival process.
    /// </summary>
    [Required]
    public TenantArchivalStatus ArchivalStatus { get; set; } = TenantArchivalStatus.Pending;

    /// <summary>
    /// Last activity date that triggered archival.
    /// </summary>
    public DateTime LastActivityDate { get; set; }

    /// <summary>
    /// When the archival was initiated.
    /// </summary>
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the archival was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Location where archived data is stored (e.g., blob storage path).
    /// </summary>
    [MaxLength(500)]
    public string? ArchiveLocation { get; set; }

    /// <summary>
    /// Size of archived data in bytes.
    /// </summary>
    public long ArchiveSizeBytes { get; set; }

    /// <summary>
    /// Checksum of archived data for integrity verification.
    /// </summary>
    [MaxLength(100)]
    public string? ArchiveChecksum { get; set; }

    /// <summary>
    /// When the archived data will be purged.
    /// </summary>
    public DateTime? ScheduledPurgeDate { get; set; }

    /// <summary>
    /// Whether notifications were sent.
    /// </summary>
    public bool NotificationsSent { get; set; } = false;

    /// <summary>
    /// Dates when notifications were sent.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public List<DateTime> NotificationDates { get; set; } = new();

    /// <summary>
    /// Additional metadata about the archive.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Error message if archival failed.
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Marks the archival as in progress.
    /// </summary>
    public void MarkInProgress()
    {
        ArchivalStatus = TenantArchivalStatus.InProgress;
    }

    /// <summary>
    /// Marks the archival as completed.
    /// </summary>
    public void MarkCompleted(string archiveLocation, long sizeBytes, string checksum)
    {
        ArchivalStatus = TenantArchivalStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        ArchiveLocation = archiveLocation;
        ArchiveSizeBytes = sizeBytes;
        ArchiveChecksum = checksum;
    }

    /// <summary>
    /// Marks the archival as failed.
    /// </summary>
    public void MarkFailed(string errorMessage)
    {
        ArchivalStatus = TenantArchivalStatus.Failed;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Marks the archive as purged.
    /// </summary>
    public void MarkPurged()
    {
        ArchivalStatus = TenantArchivalStatus.Purged;
    }

    /// <summary>
    /// Marks the tenant as restored from archive.
    /// </summary>
    public void MarkRestored()
    {
        ArchivalStatus = TenantArchivalStatus.Restored;
    }

    /// <summary>
    /// Records that a notification was sent.
    /// </summary>
    public void RecordNotification()
    {
        NotificationsSent = true;
        NotificationDates.Add(DateTime.UtcNow);
    }
}

/// <summary>
/// Reasons for tenant archival.
/// </summary>
public enum TenantArchivalReason
{
    Inactivity = 1,
    ManualRequest = 2,
    PolicyViolation = 3,
    PaymentFailure = 4,
    SubscriptionExpired = 5,
    Other = 6
}

/// <summary>
/// Status of tenant archival process.
/// </summary>
public enum TenantArchivalStatus
{
    Pending = 1,
    WarningsSent = 2,
    InProgress = 3,
    Completed = 4,
    Failed = 5,
    Restored = 6,
    Purged = 7
}
