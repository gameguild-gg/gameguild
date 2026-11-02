namespace GameGuild.Compliance.Audit;

/// <summary>Categories of data recognized by the sensitive-field classification system (issue #186).</summary>
public enum SensitiveDataCategory
{
    None = 0,
    PII = 1,
    PHI = 2,
    Financial = 3,
    Credentials = 4
}

/// <summary>
/// Auto-detected or configured classification of one entity field. The classifier scans persisted
/// field access and entity-change values and upserts one row per (entity type, field name); the row
/// records the detected category, sensitivity, matcher that fired and confidence.
/// </summary>
public sealed class SensitiveFieldClassification : EntityBase
{
    public new Guid? TenantId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public string FieldName { get; private set; } = string.Empty;
    public SensitiveDataCategory Category { get; private set; }
    public SensitivityLevel Sensitivity { get; private set; }
    public string DetectionRule { get; private set; } = string.Empty;
    /// <summary>0..1 confidence produced by the matcher that classified the field.</summary>
    public double Confidence { get; private set; }
    /// <summary>True when an administrator confirmed or edited the classification; auto scans never overwrite it.</summary>
    public bool IsConfirmed { get; private set; }
    public int ObservationCount { get; private set; }
    public DateTime FirstSeenAt { get; private set; }
    public DateTime LastSeenAt { get; private set; }

    private SensitiveFieldClassification() { }

    public static SensitiveFieldClassification Create(
        Guid? tenantId,
        string entityType,
        string fieldName,
        SensitiveDataCategory category,
        SensitivityLevel sensitivity,
        string detectionRule,
        double confidence,
        DateTime seenAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        return new SensitiveFieldClassification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntityType = entityType,
            FieldName = fieldName,
            Category = category,
            Sensitivity = sensitivity,
            DetectionRule = detectionRule,
            Confidence = confidence,
            IsConfirmed = false,
            ObservationCount = 1,
            FirstSeenAt = seenAt,
            LastSeenAt = seenAt,
            CreatedAt = seenAt,
            UpdatedAt = seenAt
        };
    }

    public void ObserveAuto(
        SensitiveDataCategory category,
        SensitivityLevel sensitivity,
        string detectionRule,
        double confidence,
        DateTime seenAt)
    {
        if (IsConfirmed)
        {
            // Confirmed classifications are authoritative; auto scans only refresh observation counters.
            ObservationCount++;
            LastSeenAt = seenAt;
            UpdatedAt = seenAt;
            return;
        }

        if (confidence > Confidence || category != Category)
        {
            Category = category;
            Sensitivity = sensitivity;
            DetectionRule = detectionRule;
            Confidence = confidence;
        }
        ObservationCount++;
        LastSeenAt = seenAt;
        UpdatedAt = seenAt;
    }

    public void Confirm(SensitiveDataCategory category, SensitivityLevel sensitivity, DateTime confirmedAt)
    {
        Category = category;
        Sensitivity = sensitivity;
        IsConfirmed = true;
        UpdatedAt = confirmedAt;
    }
}

/// <summary>Kind of unusual data-access behaviour identified by the access-pattern analysis.</summary>
public enum FieldAccessAnomalyType
{
    UnusualVolume = 1,
    ExcessiveDistinctEntities = 2,
    SensitiveReadRatio = 3,
    OffHoursAccess = 4
}

public enum FieldAccessAnomalyStatus
{
    Open = 1,
    Acknowledged = 2,
    Resolved = 3
}

/// <summary>
/// One detected access-pattern anomaly for a user in an analysis window: the observed metric, the
/// baseline it is compared against, the risk score and the investigation lifecycle.
/// </summary>
public sealed class FieldAccessAnomaly : EntityBase
{
    public new Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public FieldAccessAnomalyType AnomalyType { get; private set; }
    public double ObservedValue { get; private set; }
    public double BaselineValue { get; private set; }
    public double Score { get; private set; }
    public string? EntityType { get; private set; }
    public string Detail { get; private set; } = string.Empty;
    public FieldAccessAnomalyStatus Status { get; private set; }
    public DateTime WindowStartUtc { get; private set; }
    public DateTime WindowEndUtc { get; private set; }
    public DateTime DetectedAtUtc { get; private set; }
    public Guid? AcknowledgedByUserId { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public string? ResolutionNotes { get; private set; }

    private FieldAccessAnomaly() { }

    public static FieldAccessAnomaly Detect(
        Guid tenantId,
        Guid userId,
        FieldAccessAnomalyType anomalyType,
        double observedValue,
        double baselineValue,
        double score,
        string detail,
        DateTime windowStartUtc,
        DateTime windowEndUtc,
        DateTime detectedAtUtc,
        string? entityType = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);
        return new FieldAccessAnomaly
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            AnomalyType = anomalyType,
            ObservedValue = observedValue,
            BaselineValue = baselineValue,
            Score = score,
            EntityType = entityType,
            Detail = detail,
            Status = FieldAccessAnomalyStatus.Open,
            WindowStartUtc = windowStartUtc,
            WindowEndUtc = windowEndUtc,
            DetectedAtUtc = detectedAtUtc,
            CreatedAt = detectedAtUtc,
            UpdatedAt = detectedAtUtc
        };
    }

    public void Acknowledge(Guid userId, DateTime atUtc)
    {
        Status = FieldAccessAnomalyStatus.Acknowledged;
        AcknowledgedByUserId = userId;
        UpdatedAt = atUtc;
    }

    public void Resolve(string? notes, DateTime atUtc)
    {
        Status = FieldAccessAnomalyStatus.Resolved;
        ResolutionNotes = notes;
        ResolvedAtUtc = atUtc;
        UpdatedAt = atUtc;
    }
}

public enum BulkExportScope
{
    AuditLogExport = 1,
    FieldAccessExport = 2,
    QueryDslExport = 3,
    ScheduledExport = 4,
    Other = 5
}

/// <summary>
/// One observed bulk data export with the tenant threshold it was evaluated against. Exceeding rows
/// are flagged and raise a security alert; every export is recorded so threshold changes can be
/// investigated against historical volumes.
/// </summary>
public sealed class BulkExportEvent : EntityBase
{
    public new Guid TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public BulkExportScope Scope { get; private set; }
    public long RecordCount { get; private set; }
    public long Threshold { get; private set; }
    public bool ThresholdExceeded { get; private set; }
    public string? IpAddress { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? Destination { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public bool AlertRaised { get; private set; }

    private BulkExportEvent() { }

    public static BulkExportEvent Record(
        Guid tenantId,
        Guid? userId,
        BulkExportScope scope,
        long recordCount,
        long threshold,
        bool thresholdExceeded,
        DateTime occurredAtUtc,
        string? ipAddress = null,
        string? correlationId = null,
        string? destination = null)
    {
        return new BulkExportEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Scope = scope,
            RecordCount = recordCount,
            Threshold = threshold,
            ThresholdExceeded = thresholdExceeded,
            IpAddress = ipAddress,
            CorrelationId = correlationId,
            Destination = destination,
            OccurredAtUtc = occurredAtUtc,
            AlertRaised = false,
            CreatedAt = occurredAtUtc,
            UpdatedAt = occurredAtUtc
        };
    }

    public void MarkAlertRaised(DateTime atUtc)
    {
        AlertRaised = true;
        UpdatedAt = atUtc;
    }
}

/// <summary>Outcome of cross-border transfer validation.</summary>
public enum CrossBorderTransferStatus
{
    Allowed = 1,
    Blocked = 2,
    RequiresReview = 3
}

/// <summary>
/// Log of one cross-border data movement with the jurisdiction validation outcome. Rows are written
/// for every evaluated transfer attempt, allowed or blocked, so regulators can reconstruct the
/// transfer history (issue #186 cross-border criterion).
/// </summary>
public sealed class CrossBorderTransferLog : EntityBase
{
    public new Guid TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public string DestinationJurisdiction { get; private set; } = string.Empty;
    public string DataCategories { get; private set; } = string.Empty;
    public long RecordCount { get; private set; }
    public string Purpose { get; private set; } = string.Empty;
    public CrossBorderTransferStatus Status { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string? CorrelationId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    private CrossBorderTransferLog() { }

    public static CrossBorderTransferLog Record(
        Guid tenantId,
        Guid? userId,
        string destinationJurisdiction,
        string dataCategories,
        long recordCount,
        string purpose,
        CrossBorderTransferStatus status,
        string reason,
        DateTime occurredAtUtc,
        string? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationJurisdiction);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        return new CrossBorderTransferLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            DestinationJurisdiction = destinationJurisdiction,
            DataCategories = string.IsNullOrWhiteSpace(dataCategories) ? "unclassified" : dataCategories,
            RecordCount = recordCount,
            Purpose = purpose,
            Status = status,
            Reason = reason,
            CorrelationId = correlationId,
            OccurredAtUtc = occurredAtUtc,
            CreatedAt = occurredAtUtc,
            UpdatedAt = occurredAtUtc
        };
    }
}

/// <summary>
/// One GDPR Article 30 processing activity record: purposes, categories of data subjects and personal
/// data, recipients, transfers, retention and security measures for a processing activity.
/// </summary>
public sealed class GdprProcessingActivity : EntityBase
{
    public new Guid? TenantId { get; private set; }
    public string ActivityName { get; private set; } = string.Empty;
    public string Purpose { get; private set; } = string.Empty;
    public string LegalBasis { get; private set; } = string.Empty;
    public string CategoriesOfDataSubjects { get; private set; } = string.Empty;
    public string CategoriesOfPersonalData { get; private set; } = string.Empty;
    public string Recipients { get; private set; } = string.Empty;
    public bool InvolvesCrossBorderTransfers { get; private set; }
    public string? TransferDestinations { get; private set; }
    public string RetentionPeriod { get; private set; } = string.Empty;
    public string SecurityMeasures { get; private set; } = string.Empty;
    /// <summary>Auto entries are derived from the platform processing catalog and can be regenerated; manual entries survive reseeds.</summary>
    public bool IsAutoDerived { get; private set; }
    public Guid? LastReviewedByUserId { get; private set; }
    public DateTime? LastReviewedAtUtc { get; private set; }

    private GdprProcessingActivity() { }

    public static GdprProcessingActivity Create(
        Guid? tenantId,
        string activityName,
        string purpose,
        string legalBasis,
        string categoriesOfDataSubjects,
        string categoriesOfPersonalData,
        string recipients,
        bool involvesCrossBorderTransfers,
        string? transferDestinations,
        string retentionPeriod,
        string securityMeasures,
        bool isAutoDerived,
        DateTime createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        return new GdprProcessingActivity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ActivityName = activityName,
            Purpose = purpose,
            LegalBasis = string.IsNullOrWhiteSpace(legalBasis) ? "legitimate-interest" : legalBasis,
            CategoriesOfDataSubjects = categoriesOfDataSubjects,
            CategoriesOfPersonalData = categoriesOfPersonalData,
            Recipients = recipients,
            InvolvesCrossBorderTransfers = involvesCrossBorderTransfers,
            TransferDestinations = transferDestinations,
            RetentionPeriod = retentionPeriod,
            SecurityMeasures = securityMeasures,
            IsAutoDerived = isAutoDerived,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void Update(
        string purpose,
        string legalBasis,
        string categoriesOfDataSubjects,
        string categoriesOfPersonalData,
        string recipients,
        bool involvesCrossBorderTransfers,
        string? transferDestinations,
        string retentionPeriod,
        string securityMeasures,
        DateTime updatedAt)
    {
        Purpose = purpose;
        LegalBasis = legalBasis;
        CategoriesOfDataSubjects = categoriesOfDataSubjects;
        CategoriesOfPersonalData = categoriesOfPersonalData;
        Recipients = recipients;
        InvolvesCrossBorderTransfers = involvesCrossBorderTransfers;
        TransferDestinations = transferDestinations;
        RetentionPeriod = retentionPeriod;
        SecurityMeasures = securityMeasures;
        IsAutoDerived = false;
        UpdatedAt = updatedAt;
    }

    public void MarkReviewed(Guid reviewerUserId, DateTime reviewedAtUtc)
    {
        LastReviewedByUserId = reviewerUserId;
        LastReviewedAtUtc = reviewedAtUtc;
        UpdatedAt = reviewedAtUtc;
    }
}

/// <summary>Lifecycle action applied when a sensitive-data retention window elapses.</summary>
public enum SensitiveDataRetentionAction
{
    Purge = 1,
    Mask = 2
}

/// <summary>
/// Per-category retention rule for sensitive field-access data: when the window elapses the
/// enforcement pass applies the configured action to expired records (issue #186 retention criterion).
/// </summary>
public sealed class SensitiveDataRetentionPolicy : EntityBase
{
    public new Guid? TenantId { get; private set; }
    public SensitiveDataCategory Category { get; private set; }
    public int RetentionDays { get; private set; }
    public SensitiveDataRetentionAction Action { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public DateTime ConfiguredAtUtc { get; private set; }
    public DateTime? LastEnforcedAtUtc { get; private set; }
    public int LastEnforcedCount { get; private set; }

    private SensitiveDataRetentionPolicy() { }

    public static SensitiveDataRetentionPolicy Create(
        Guid? tenantId,
        SensitiveDataCategory category,
        int retentionDays,
        SensitiveDataRetentionAction action,
        Guid? updatedByUserId,
        DateTime configuredAtUtc)
    {
        if (retentionDays < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionDays), "Sensitive data retention must be at least one day.");
        }
        return new SensitiveDataRetentionPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Category = category,
            RetentionDays = retentionDays,
            Action = action,
            UpdatedByUserId = updatedByUserId,
            ConfiguredAtUtc = configuredAtUtc,
            CreatedAt = configuredAtUtc,
            UpdatedAt = configuredAtUtc
        };
    }

    public void Update(int retentionDays, SensitiveDataRetentionAction action, Guid? updatedByUserId, DateTime updatedAtUtc)
    {
        if (retentionDays < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionDays), "Sensitive data retention must be at least one day.");
        }
        RetentionDays = retentionDays;
        Action = action;
        UpdatedByUserId = updatedByUserId;
        ConfiguredAtUtc = updatedAtUtc;
        UpdatedAt = updatedAtUtc;
    }

    public void MarkEnforced(int affectedCount, DateTime enforcedAtUtc)
    {
        LastEnforcedAtUtc = enforcedAtUtc;
        LastEnforcedCount = affectedCount;
        UpdatedAt = enforcedAtUtc;
    }
}
