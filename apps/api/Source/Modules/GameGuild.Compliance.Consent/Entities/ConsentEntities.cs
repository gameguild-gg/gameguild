using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Compliance.Consent;

public enum PolicyType
{
    PrivacyPolicy,
    TermsOfService,
    CookiePolicy,
    DataProcessingAgreement,
    MarketingConsent,
    ThirdPartySharing,
    Custom
}

public enum ContentType
{
    PlainText,
    Html,
    Markdown,
    Url
}

/// <summary>
///     A consent policy that users can accept or reject (e.g. privacy policy, cookie policy).
/// </summary>
[Table("consent_policies")]
[Index(nameof(PolicyType))]
[Index(nameof(IsActive))]
public class ConsentPolicy : EntityBase
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public PolicyType PolicyType { get; set; }

    /// <summary>
    ///     Whether consent to this policy is mandatory for using the platform.
    /// </summary>
    public bool IsMandatory { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<PolicyVersion> Versions { get; set; } = new List<PolicyVersion>();
}

/// <summary>
///     A specific version of a consent policy.
/// </summary>
[Table("consent_policy_versions")]
[Index(nameof(ConsentPolicyId))]
[Index(nameof(VersionNumber))]
[Index(nameof(EffectiveFrom))]
public class PolicyVersion : EntityBase
{
    public Guid ConsentPolicyId { get; set; }

    [ForeignKey(nameof(ConsentPolicyId))]
    public ConsentPolicy? ConsentPolicy { get; set; }

    [Required]
    [MaxLength(50)]
    public string VersionNumber { get; set; } = "1.0";

    public ContentType ContentType { get; set; }

    /// <summary>
    ///     The policy content or URL pointing to the rendered policy.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

    public DateTime? EffectiveUntil { get; set; }

    public bool IsCurrent { get; set; } = true;
}

/// <summary>
///     Records a user's consent decision for a specific policy version.
/// </summary>
[Table("user_consents")]
[Index(nameof(UserId), nameof(PolicyVersionId), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(ConsentGivenAt))]
public class UserConsent : EntityBase
{
    public Guid UserId { get; set; }

    public Guid PolicyVersionId { get; set; }

    [ForeignKey(nameof(PolicyVersionId))]
    public PolicyVersion? PolicyVersion { get; set; }

    public bool IsGranted { get; set; }

    public DateTime ConsentGivenAt { get; set; } = DateTime.UtcNow;

    public DateTime? ConsentRevokedAt { get; set; }

    /// <summary>
    ///     IP address at the time of consent for audit purposes.
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    ///     Method of consent collection (e.g. "banner", "registration", "settings").
    /// </summary>
    [MaxLength(100)]
    public string? ConsentMethod { get; set; }

    public void Revoke()
    {
        IsGranted = false;
        ConsentRevokedAt = DateTime.UtcNow;
        Touch();
    }
}

/// <summary>
///     Tracks GDPR data subject rights requests (access, erasure, portability, rectification).
/// </summary>
[Table("data_subject_requests")]
[Index(nameof(UserId))]
[Index(nameof(RequestType))]
[Index(nameof(Status))]
public class DataSubjectRequest : EntityBase
{
    public Guid UserId { get; set; }

    public DataSubjectRequestType RequestType { get; set; }

    public DataSubjectRequestStatus Status { get; set; } = DataSubjectRequestStatus.Pending;

    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    ///     Admin notes on the request processing.
    /// </summary>
    [MaxLength(2000)]
    public string? ProcessingNotes { get; set; }

    public Guid? ProcessedByUserId { get; set; }

    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    ///     GDPR requires completion within 30 days.
    /// </summary>
    public DateTime Deadline { get; set; }

    public void Complete(Guid processedBy, string? notes = null)
    {
        Status = DataSubjectRequestStatus.Completed;
        ProcessedByUserId = processedBy;
        ProcessedAt = DateTime.UtcNow;
        ProcessingNotes = notes;
        Touch();
    }

    public void Reject(Guid processedBy, string reason)
    {
        Status = DataSubjectRequestStatus.Rejected;
        ProcessedByUserId = processedBy;
        ProcessedAt = DateTime.UtcNow;
        ProcessingNotes = reason;
        Touch();
    }
}

public enum DataSubjectRequestType
{
    Access,
    Erasure,
    Portability,
    Rectification,
    Restriction,
    Objection
}

public enum DataSubjectRequestStatus
{
    Pending,
    InProgress,
    Completed,
    Rejected,
    Expired
}
