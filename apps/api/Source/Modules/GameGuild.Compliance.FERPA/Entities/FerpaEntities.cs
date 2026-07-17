using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Compliance.FERPA;

public enum EducationRecordKind
{
    CourseEnrollment,
    AssessmentSubmission,
    Grade,
    Certificate,
    Attendance,
    Communication,
    SupportCase,
    Custom
}

public enum FerpaRecordProtectionLevel
{
    DirectoryInformation,
    EducationRecord,
    SensitiveEducationRecord,
    Restricted
}

public enum FerpaDisclosureBasis
{
    StudentConsent,
    GuardianConsent,
    SchoolOfficial,
    FinancialAid,
    HealthOrSafetyEmergency,
    AuditOrEvaluation,
    CourtOrder,
    DirectoryInformation,
    Other
}

public enum FerpaRequestStatus
{
    Pending,
    InReview,
    Completed,
    Denied,
    Expired
}

[Table("ferpa_education_records")]
[Index(nameof(StudentUserId))]
[Index(nameof(TenantId))]
[Index(nameof(RecordKind))]
[Index(nameof(ExternalRecordId))]
public class FerpaEducationRecord : EntityBase
{
    public Guid StudentUserId { get; set; }

    public EducationRecordKind RecordKind { get; set; }

    [MaxLength(200)]
    public string ExternalRecordId { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    public FerpaRecordProtectionLevel ProtectionLevel { get; set; } = FerpaRecordProtectionLevel.EducationRecord;

    public bool IsDirectoryInformation { get; set; }

    public DateTime? RetentionUntil { get; set; }

    public string MetadataJson { get; set; } = "{}";

    public FerpaEducationRecordDto ToDto() => new(
        Id,
        StudentUserId,
        RecordKind,
        ExternalRecordId,
        Title,
        ProtectionLevel,
        IsDirectoryInformation,
        RetentionUntil,
        MetadataJson,
        CreatedAt);
}

[Table("ferpa_directory_information_policies")]
[Index(nameof(TenantId), IsUnique = true)]
public class FerpaDirectoryInformationPolicy : EntityBase
{
    public string AllowedFieldsJson { get; set; } = "[]";

    public bool OptOutEnabled { get; set; } = true;

    public DateTime? AnnualNoticeSentAt { get; set; }

    [MaxLength(500)]
    public string? NoticeUrl { get; set; }

    public void Update(string allowedFieldsJson, bool optOutEnabled, DateTime? annualNoticeSentAt, string? noticeUrl)
    {
        AllowedFieldsJson = allowedFieldsJson;
        OptOutEnabled = optOutEnabled;
        AnnualNoticeSentAt = annualNoticeSentAt;
        NoticeUrl = noticeUrl;
        Touch();
    }

    public FerpaDirectoryInformationPolicyDto ToDto() => new(
        Id,
        TenantId,
        AllowedFieldsJson,
        OptOutEnabled,
        AnnualNoticeSentAt,
        NoticeUrl);
}

[Table("ferpa_disclosure_consents")]
[Index(nameof(StudentUserId))]
[Index(nameof(StudentUserId), nameof(Recipient), nameof(Scope))]
public class FerpaDisclosureConsent : EntityBase
{
    public Guid StudentUserId { get; set; }

    public Guid? GuardianUserId { get; set; }

    [MaxLength(250)]
    public string Recipient { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Purpose { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Scope { get; set; } = string.Empty;

    public DateTime EffectiveFrom { get; set; } = SystemClock.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public bool IsActiveAt(DateTime instant)
        => RevokedAt is null && EffectiveFrom <= instant && (!ExpiresAt.HasValue || ExpiresAt.Value >= instant);

    public void Revoke()
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = SystemClock.UtcNow;
        Touch();
    }

    public FerpaDisclosureConsentDto ToDto() => new(
        Id,
        StudentUserId,
        GuardianUserId,
        Recipient,
        Purpose,
        Scope,
        EffectiveFrom,
        ExpiresAt,
        RevokedAt,
        IsActiveAt(SystemClock.UtcNow));
}

[Table("ferpa_disclosure_logs")]
[Index(nameof(StudentUserId))]
[Index(nameof(DisclosedAt))]
public class FerpaDisclosureLog : EntityBase
{
    public Guid StudentUserId { get; set; }

    public Guid DisclosedByUserId { get; set; }

    [MaxLength(250)]
    public string Recipient { get; set; } = string.Empty;

    public FerpaDisclosureBasis Basis { get; set; }

    [MaxLength(500)]
    public string Purpose { get; set; } = string.Empty;

    public string RecordIdsJson { get; set; } = "[]";

    public DateTime DisclosedAt { get; set; } = SystemClock.UtcNow;

    public FerpaDisclosureLogDto ToDto() => new(
        Id,
        StudentUserId,
        DisclosedByUserId,
        Recipient,
        Basis,
        Purpose,
        RecordIdsJson,
        DisclosedAt);
}

[Table("ferpa_inspection_requests")]
[Index(nameof(StudentUserId))]
[Index(nameof(Status))]
[Index(nameof(Deadline))]
public class FerpaInspectionRequest : EntityBase
{
    public Guid StudentUserId { get; set; }

    public Guid RequestedByUserId { get; set; }

    public FerpaRequestStatus Status { get; set; } = FerpaRequestStatus.Pending;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public DateTime Deadline { get; set; } = SystemClock.UtcNow.AddDays(45);

    public Guid? ProcessedByUserId { get; set; }

    public DateTime? ProcessedAt { get; set; }

    [MaxLength(2000)]
    public string? ProcessingNotes { get; set; }

    public void Complete(Guid processedByUserId, string? notes)
    {
        EnsureCanBeProcessed();
        Status = FerpaRequestStatus.Completed;
        ProcessedByUserId = processedByUserId;
        ProcessedAt = SystemClock.UtcNow;
        ProcessingNotes = notes;
        Touch();
    }

    public void Deny(Guid processedByUserId, string reason)
    {
        EnsureCanBeProcessed();
        Status = FerpaRequestStatus.Denied;
        ProcessedByUserId = processedByUserId;
        ProcessedAt = SystemClock.UtcNow;
        ProcessingNotes = reason;
        Touch();
    }

    private void EnsureCanBeProcessed()
    {
        if (Status is not (FerpaRequestStatus.Pending or FerpaRequestStatus.InReview))
        {
            throw new InvalidOperationException($"FERPA inspection request cannot be processed in {Status} status.");
        }
    }

    public FerpaInspectionRequestDto ToDto() => new(
        Id,
        StudentUserId,
        RequestedByUserId,
        Status,
        Deadline,
        ProcessedByUserId,
        ProcessedAt,
        ProcessingNotes);
}

public sealed record FerpaEducationRecordDto(
    Guid Id,
    Guid StudentUserId,
    EducationRecordKind RecordKind,
    string ExternalRecordId,
    string Title,
    FerpaRecordProtectionLevel ProtectionLevel,
    bool IsDirectoryInformation,
    DateTime? RetentionUntil,
    string MetadataJson,
    DateTime CreatedAt);

public sealed record FerpaDirectoryInformationPolicyDto(
    Guid Id,
    Guid? TenantId,
    string AllowedFieldsJson,
    bool OptOutEnabled,
    DateTime? AnnualNoticeSentAt,
    string? NoticeUrl);

public sealed record FerpaDisclosureConsentDto(
    Guid Id,
    Guid StudentUserId,
    Guid? GuardianUserId,
    string Recipient,
    string Purpose,
    string Scope,
    DateTime EffectiveFrom,
    DateTime? ExpiresAt,
    DateTime? RevokedAt,
    bool IsActive);

public sealed record FerpaDisclosureLogDto(
    Guid Id,
    Guid StudentUserId,
    Guid DisclosedByUserId,
    string Recipient,
    FerpaDisclosureBasis Basis,
    string Purpose,
    string RecordIdsJson,
    DateTime DisclosedAt);

public sealed record FerpaInspectionRequestDto(
    Guid Id,
    Guid StudentUserId,
    Guid RequestedByUserId,
    FerpaRequestStatus Status,
    DateTime Deadline,
    Guid? ProcessedByUserId,
    DateTime? ProcessedAt,
    string? ProcessingNotes);
