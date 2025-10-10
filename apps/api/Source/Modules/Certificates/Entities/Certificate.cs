using GameGuild.Modules.Users;
using GameGuild.Domain.Common;
using GameGuild.Modules.Programs.Entities;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Users.Entities;

namespace GameGuild.Modules.Certificates.Entities;

/// <summary>
/// Represents a certificate template that can be earned by completing programs
/// </summary>
[Table("certificates")]
[Index(nameof(ProgramId))]
[Index(nameof(CertificateType))]
[Index(nameof(IsActive))]
[Index(nameof(TenantId))]
public class Certificate : EntityBase
{
    /// <summary>
    /// Program this certificate is associated with
    /// </summary>
    [Required]
    public Guid ProgramId { get; set; }

    /// <summary>
    /// Certificate name/title
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Certificate description
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Type of certificate
    /// </summary>
    public CertificateType CertificateType { get; set; } = CertificateType.Completion;

    /// <summary>
    /// Whether this certificate is active and can be earned
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Template design data (JSON)
    /// </summary>
    public string? TemplateDesign { get; set; }

    /// <summary>
    /// Required completion percentage to earn this certificate
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal RequiredCompletionPercentage { get; set; } = 100m;

    /// <summary>
    /// Minimum grade required to earn this certificate
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? MinimumGrade { get; set; }

    /// <summary>
    /// Certificate validity period in days (null = never expires)
    /// </summary>
    public int? ValidityDays { get; set; }

    /// <summary>
    /// Skills or competencies this certificate represents
    /// </summary>
    [MaxLength(1000)]
    public string? Skills { get; set; }

    /// <summary>
    /// Certificate code/identifier template
    /// </summary>
    [MaxLength(100)]
    public string? CertificateCode { get; set; }

    // Navigation Properties
    /// <summary>
    /// Associated program
    /// </summary>
    public virtual Program Program { get; set; } = null!;

    /// <summary>
    /// User certificates issued from this template
    /// </summary>
    public virtual ICollection<UserCertificate> UserCertificates { get; set; } = new List<UserCertificate>();

    /// <summary>
    /// Certificate tags for skills/competencies
    /// </summary>
    public virtual ICollection<CertificateTag> CertificateTags { get; set; } = new List<CertificateTag>();

    // Computed Properties
    /// <summary>
    /// Whether this certificate is global (tenant-independent)
    /// </summary>
    public bool IsGlobal => TenantId == null;

    /// <summary>
    /// Number of certificates issued
    /// </summary>
    public int IssuedCount => UserCertificates?.Count ?? 0;

    /// <summary>
    /// Whether certificate has expiration
    /// </summary>
    public bool HasExpiration => ValidityDays.HasValue;

    // Domain Methods
    /// <summary>
    /// Activates the certificate
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the certificate
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates certificate requirements
    /// </summary>
    public void UpdateRequirements(decimal completionPercentage, decimal? minimumGrade = null)
    {
        RequiredCompletionPercentage = Math.Max(0, Math.Min(100, completionPercentage));
        MinimumGrade = minimumGrade;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets certificate validity period
    /// </summary>
    public void SetValidityPeriod(int? days)
    {
        ValidityDays = days;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a program enrollment qualifies for this certificate
    /// </summary>
    public bool QualifiesForCertificate(ProgramUser programUser)
    {
        if (!IsActive)
            return false;

        // Check completion percentage
        if (programUser.CompletionPercentage < RequiredCompletionPercentage)
            return false;

        // Check minimum grade if required
        if (MinimumGrade.HasValue &&
            (!programUser.FinalGrade.HasValue || programUser.FinalGrade.Value < MinimumGrade.Value))
            return false;

        return true;
    }
}

/// <summary>
/// Certificate types
/// </summary>
public enum CertificateType
{
    Completion = 0,
    Achievement = 1,
    Proficiency = 2,
    Mastery = 3,
    Participation = 4,
    Excellence = 5
}