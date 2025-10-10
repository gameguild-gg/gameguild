using GameGuild.Core.Domain;

namespace GameGuild.Modules.Audit.Entities;

/// <summary>
/// Scheduled audit export job configuration for automated export to SFTP, S3, Azure Blob Storage, etc.
/// Supports cron-based scheduling and compliance framework-specific templates.
/// </summary>
public sealed class ScheduledAuditExport : EntityBase
{
    public Guid TenantId { get; private set; }
    public string JobName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsEnabled { get; private set; }

    // Schedule configuration
    public string CronExpression { get; private set; } = string.Empty;
    public string Timezone { get; private set; } = "UTC";
    public DateTime? NextRunAt { get; private set; }
    public DateTime? LastRunAt { get; private set; }

    // Export destination
    public ExportDestinationType DestinationType { get; private set; }
    public string DestinationUrl { get; private set; } = string.Empty;
    public string? DestinationPath { get; private set; }
    public string? CredentialKeyName { get; private set; }

    // Export configuration
    public ExportFormat ExportFormat { get; private set; }
    public ComplianceFramework? ComplianceFramework { get; private set; }
    public string? ExportTemplate { get; private set; }
    public string[] IncludeEventTypes { get; private set; } = Array.Empty<string>();
    public string[] ExcludeEventTypes { get; private set; } = Array.Empty<string>();
    public int RetentionDays { get; private set; } = 30;

    // Filtering
    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public string? RiskLevelFilter { get; private set; }
    public string? UserIdFilter { get; private set; }

    // Encryption and security
    public bool EncryptExport { get; private set; }
    public string? EncryptionKeyId { get; private set; }
    public bool SignExport { get; private set; }

    // Execution history
    public int SuccessCount { get; private set; }
    public int FailureCount { get; private set; }
    public DateTime? LastSuccessAt { get; private set; }
    public DateTime? LastFailureAt { get; private set; }
    public string? LastErrorMessage { get; private set; }

    // Notification
    public bool NotifyOnSuccess { get; private set; }
    public bool NotifyOnFailure { get; private set; }
    public string[] NotificationEmails { get; private set; } = Array.Empty<string>();

    private ScheduledAuditExport() { }

    public static ScheduledAuditExport Create(
        Guid tenantId,
        string jobName,
        string cronExpression,
        ExportDestinationType destinationType,
        string destinationUrl,
        ExportFormat exportFormat,
        ComplianceFramework? complianceFramework = null)
    {
        return new ScheduledAuditExport
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            JobName = jobName,
            CronExpression = cronExpression,
            DestinationType = destinationType,
            DestinationUrl = destinationUrl,
            ExportFormat = exportFormat,
            ComplianceFramework = complianceFramework,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Enable() { IsEnabled = true; UpdatedAt = DateTime.UtcNow; }
    public void Disable() { IsEnabled = false; UpdatedAt = DateTime.UtcNow; }

    public void RecordSuccess(DateTime executedAt)
    {
        SuccessCount++;
        LastSuccessAt = executedAt;
        LastRunAt = executedAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordFailure(DateTime executedAt, string errorMessage)
    {
        FailureCount++;
        LastFailureAt = executedAt;
        LastErrorMessage = errorMessage;
        LastRunAt = executedAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNextRunTime(DateTime nextRunAt)
    {
        NextRunAt = nextRunAt;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum ExportDestinationType
{
    Sftp = 0,
    S3 = 1,
    AzureBlobStorage = 2,
    GoogleCloudStorage = 3,
    LocalFileSystem = 4,
    Https = 5
}

public enum ExportFormat
{
    Json = 0,
    Csv = 1,
    Xml = 2,
    Parquet = 3
}

public enum ComplianceFramework
{
    SOC2 = 0,
    ISO27001 = 1,
    GDPR = 2,
    HIPAA = 3,
    PCI_DSS = 4,
    CCPA = 5,
    FedRAMP = 6,
    NIST = 7
}

/// <summary>
/// Export execution history and results.
/// </summary>
public sealed class AuditExportHistory : EntityBase
{
    public Guid ScheduledExportId { get; private set; }
    public Guid TenantId { get; private set; }
    public DateTime ExecutedAt { get; private set; }
    public ExportStatus Status { get; private set; }
    public int RecordCount { get; private set; }
    public long FileSizeBytes { get; private set; }
    public string? ExportPath { get; private set; }
    public string? FileChecksum { get; private set; }
    public string? ErrorMessage { get; private set; }
    public TimeSpan ExecutionDuration { get; private set; }

    private AuditExportHistory() { }

    public static AuditExportHistory Create(Guid scheduledExportId, Guid tenantId)
    {
        return new AuditExportHistory
        {
            Id = Guid.NewGuid(),
            ScheduledExportId = scheduledExportId,
            TenantId = tenantId,
            ExecutedAt = DateTime.UtcNow,
            Status = ExportStatus.InProgress,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Complete(int recordCount, long fileSizeBytes, string exportPath, string fileChecksum, TimeSpan duration)
    {
        Status = ExportStatus.Completed;
        RecordCount = recordCount;
        FileSizeBytes = fileSizeBytes;
        ExportPath = exportPath;
        FileChecksum = fileChecksum;
        ExecutionDuration = duration;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage, TimeSpan duration)
    {
        Status = ExportStatus.Failed;
        ErrorMessage = errorMessage;
        ExecutionDuration = duration;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum ExportStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
