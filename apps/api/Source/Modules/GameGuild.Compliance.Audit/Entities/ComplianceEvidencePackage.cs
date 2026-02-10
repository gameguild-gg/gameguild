
namespace GameGuild.Compliance.Audit;

/// <summary>
/// Compliance evidence package for audit compliance reports (SOC2, ISO 27001, GDPR, HIPAA).
/// Bundles audit logs, cryptographic proofs, and metadata for regulatory submissions.
/// </summary>
public sealed class ComplianceEvidencePackage : EntityBase {
    public string PackageName { get; private set; } = string.Empty;
    public ComplianceFramework Framework { get; private set; }
    public string PackageVersion { get; private set; } = string.Empty;
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public CompliancePackageStatus Status { get; private set; }

    // Package contents
    public int TotalAuditLogs { get; private set; }
    public int TotalAnomalies { get; private set; }
    public int TotalAccessLogs { get; private set; }
    public string PackageHash { get; private set; } = string.Empty;
    public string DigitalSignature { get; private set; } = string.Empty;
    public long PackageSizeBytes { get; private set; }

    // Storage and delivery
    public string? StoragePath { get; private set; }
    public string? DeliveryMethod { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public string? DeliveredTo { get; private set; }
    public string? DeliveryTrackingId { get; private set; }

    // Metadata
    public string? PreparedBy { get; private set; }
    public string? ReviewedBy { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? Notes { get; private set; }
    public string? AttachmentMetadata { get; private set; }

    private ComplianceEvidencePackage() { }

    public static ComplianceEvidencePackage Create(
        Guid tenantId,
        string packageName,
        ComplianceFramework framework,
        string version,
        DateTime periodStart,
        DateTime periodEnd,
        string preparedBy) {
        return new ComplianceEvidencePackage {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PackageName = packageName,
            Framework = framework,
            PackageVersion = version,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Status = CompliancePackageStatus.Draft,
            PreparedBy = preparedBy
        };
    }

    public void SetPackageContents(int auditLogs, int anomalies, int accessLogs, long sizeBytes) {
        TotalAuditLogs = auditLogs;
        TotalAnomalies = anomalies;
        TotalAccessLogs = accessLogs;
        PackageSizeBytes = sizeBytes;
    }

    public void Sign(string packageHash, string digitalSignature) {
        PackageHash = packageHash;
        DigitalSignature = digitalSignature;
        Status = CompliancePackageStatus.Signed;
    }

    public void MarkAsReviewed(string reviewedBy, string? notes = null) {
        ReviewedBy = reviewedBy;
        ReviewedAt = SystemClock.UtcNow;
        Status = CompliancePackageStatus.Reviewed;
        if (!string.IsNullOrEmpty(notes))
            Notes = notes;
    }

    public void Approve(string approvedBy) {
        ApprovedBy = approvedBy;
        ApprovedAt = SystemClock.UtcNow;
        Status = CompliancePackageStatus.Approved;
    }

    public void SetStoragePath(string storagePath) {
        StoragePath = storagePath;
    }

    public void MarkAsDelivered(string deliveryMethod, string deliveredTo, string trackingId) {
        DeliveryMethod = deliveryMethod;
        DeliveredTo = deliveredTo;
        DeliveryTrackingId = trackingId;
        DeliveredAt = SystemClock.UtcNow;
        Status = CompliancePackageStatus.Delivered;
    }

    public void SetAttachmentMetadata(string metadata) {
        AttachmentMetadata = metadata;
    }
}

public enum CompliancePackageStatus {
    Draft,
    InProgress,
    Signed,
    Reviewed,
    Approved,
    Delivered,
    Archived,
    Rejected
}
