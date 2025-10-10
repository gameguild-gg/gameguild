namespace GameGuild.Modules.Users;

/// <summary>
/// Represents a GDPR personal data export request and its status
/// </summary>
[Table("personal_data_exports")]
public sealed class PersonalDataExport : EntityBase
{
    /// <summary>
    /// User who requested the data export
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Current status of the export request
    /// </summary>
    [Required]
    public DataExportStatus Status { get; set; } = DataExportStatus.Pending;

    /// <summary>
    /// Date and time when the export was requested
    /// </summary>
    [Required]
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time when the export was completed (if applicable)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// URL or path to the exported data file
    /// </summary>
    [MaxLength(500)]
    public string? ExportFilePath { get; set; }

    /// <summary>
    /// Size of the export file in bytes
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Format of the export (JSON, CSV, XML, etc.)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Format { get; set; } = "JSON";

    /// <summary>
    /// Date and time when the export file will expire and be deleted
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Error message if the export failed
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of data entities included in the export
    /// </summary>
    public int EntityCount { get; set; }

    /// <summary>
    /// Mark the export as completed
    /// </summary>
    public void MarkCompleted(string filePath, long fileSize, int entityCount)
    {
        Status = DataExportStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        ExportFilePath = filePath;
        FileSizeBytes = fileSize;
        EntityCount = entityCount;
        ExpiresAt = DateTime.UtcNow.AddDays(30); // GDPR requirement: data available for 30 days
        Touch();
    }

    /// <summary>
    /// Mark the export as failed
    /// </summary>
    public void MarkFailed(string errorMessage)
    {
        Status = DataExportStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        Touch();
    }

    /// <summary>
    /// Mark the export as expired
    /// </summary>
    public void MarkExpired()
    {
        Status = DataExportStatus.Expired;
        Touch();
    }
}

/// <summary>
/// Status of a personal data export request
/// </summary>
public enum DataExportStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Expired = 4
}
