using GameGuild.Domain.Common;
using GameGuild.Modules.Programs.Entities;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Users.Entities;

namespace GameGuild.Modules.Certificates.Entities;

/// <summary>
/// Represents a certificate issued to a user upon program completion
/// </summary>
[Table("user_certificates")]
[Index(nameof(UserId), nameof(CertificateId), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(CertificateId))]
[Index(nameof(ProgramUserId))]
[Index(nameof(IssuedAt))]
[Index(nameof(ExpiresAt))]
[Index(nameof(CertificateNumber))]
[Index(nameof(TenantId))]
public class UserCertificate : EntityBase
{
    /// <summary>
    /// User who earned the certificate
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Certificate template
    /// </summary>
    [Required]
    public Guid CertificateId { get; set; }

    /// <summary>
    /// Program enrollment this certificate is for
    /// </summary>
    [Required]
    public Guid ProgramUserId { get; set; }

    /// <summary>
    /// Unique certificate number
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string CertificateNumber { get; set; } = string.Empty;

    /// <summary>
    /// When the certificate was issued
    /// </summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// When the certificate expires (if applicable)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// User's final grade when earning the certificate
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? FinalGrade { get; set; }

    /// <summary>
    /// Completion percentage when certificate was earned
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal CompletionPercentage { get; set; }

    /// <summary>
    /// Whether the certificate is active/valid
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Certificate verification hash
    /// </summary>
    [MaxLength(128)]
    public string? VerificationHash { get; set; }

    /// <summary>
    /// Digital signature data
    /// </summary>
    public string? DigitalSignature { get; set; }

    /// <summary>
    /// Certificate metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Date when certificate was revoked (if applicable)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Reason for revocation
    /// </summary>
    [MaxLength(500)]
    public string? RevocationReason { get; set; }

    // Navigation Properties
    /// <summary>
    /// User who earned the certificate
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Certificate template
    /// </summary>
    public virtual Certificate Certificate { get; set; } = null!;

    /// <summary>
    /// Program enrollment
    /// </summary>
    public virtual ProgramUser ProgramUser { get; set; } = null!;

    // Computed Properties
    /// <summary>
    /// Whether this certificate is global (tenant-independent)
    /// </summary>
    public bool IsGlobal => TenantId == null;

    /// <summary>
    /// Whether the certificate is currently valid
    /// </summary>
    public bool IsValid => IsActive && !IsRevoked && !IsExpired;

    /// <summary>
    /// Whether the certificate is expired
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;

    /// <summary>
    /// Whether the certificate is revoked
    /// </summary>
    public bool IsRevoked => RevokedAt.HasValue;

    /// <summary>
    /// Days until expiration (negative if expired)
    /// </summary>
    public int? DaysUntilExpiration => ExpiresAt.HasValue
        ? (ExpiresAt.Value - DateTime.UtcNow).Days
        : null;

    /// <summary>
    /// Age of certificate in days
    /// </summary>
    public int AgeInDays => (DateTime.UtcNow - IssuedAt).Days;

    // Domain Methods
    /// <summary>
    /// Revokes the certificate
    /// </summary>
    public void Revoke(string reason)
    {
        RevokedAt = DateTime.UtcNow;
        RevocationReason = reason;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reinstates a revoked certificate
    /// </summary>
    public void Reinstate()
    {
        if (IsRevoked)
        {
            RevokedAt = null;
            RevocationReason = null;
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Extends the certificate expiration date
    /// </summary>
    public void Extend(int additionalDays)
    {
        if (ExpiresAt.HasValue)
        {
            ExpiresAt = ExpiresAt.Value.AddDays(additionalDays);
        }
        else if (Certificate.ValidityDays.HasValue)
        {
            ExpiresAt = DateTime.UtcNow.AddDays(Certificate.ValidityDays.Value + additionalDays);
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Generates a verification URL for this certificate
    /// </summary>
    public string GenerateVerificationUrl(string baseUrl)
    {
        return $"{baseUrl.TrimEnd('/')}/certificates/verify/{CertificateNumber}";
    }

    /// <summary>
    /// Generates verification hash
    /// </summary>
    public void GenerateVerificationHash()
    {
        var data = $"{UserId}|{CertificateId}|{CertificateNumber}|{IssuedAt:yyyy-MM-dd}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        VerificationHash = Convert.ToBase64String(hashBytes);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates the certificate data integrity
    /// </summary>
    public bool ValidateIntegrity()
    {
        if (string.IsNullOrEmpty(VerificationHash))
            return false;

        var data = $"{UserId}|{CertificateId}|{CertificateNumber}|{IssuedAt:yyyy-MM-dd}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        var computedHash = Convert.ToBase64String(hashBytes);

        return computedHash == VerificationHash;
    }

    /// <summary>
    /// Creates certificate display data
    /// </summary>
    public object GetDisplayData()
    {
        return new
        {
            CertificateNumber,
            RecipientName = User?.FullName ?? "Unknown",
            ProgramTitle = ProgramUser?.Program?.Title ?? "Unknown Program",
            IssuedDate = IssuedAt.ToString("MMMM dd, yyyy"),
            ExpirationDate = ExpiresAt?.ToString("MMMM dd, yyyy"),
            FinalGrade = FinalGrade?.ToString("F2"),
            CompletionPercentage = CompletionPercentage.ToString("F1") + "%",
            IsValid,
            VerificationUrl = GenerateVerificationUrl("https://gameguild.gg")
        };
    }
}