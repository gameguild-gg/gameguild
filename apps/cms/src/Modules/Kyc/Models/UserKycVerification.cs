using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;

namespace GameGuild.Modules.Kyc.Models;

[Table("user_kyc_verifications")]
[Index(nameof(UserId))]
[Index(nameof(Provider))]
[Index(nameof(Status))]
[Index(nameof(SubmittedAt))]
[Index(nameof(ExternalVerificationId))]
public class UserKycVerification : BaseEntity
{
    private Guid _userId;

    private KycProvider _provider;

    private KycVerificationStatus _status = KycVerificationStatus.Pending;

    private string? _externalVerificationId;

    private string? _verificationLevel;

    private string? _documentTypes;

    private string? _documentCountry;

    private DateTime _submittedAt;

    private DateTime? _completedAt;

    private DateTime? _expiresAt;

    private string? _notes;

    private string? _providerData;

    private User.Models.User _user = null!;

    [Required]
    public Guid UserId
    {
        get => _userId;
        set => _userId = value;
    }

    [Required]
    public KycProvider Provider
    {
        get => _provider;
        set => _provider = value;
    }

    [Required]
    public KycVerificationStatus Status
    {
        get => _status;
        set => _status = value;
    }

    /// <summary>
    /// External verification ID from the KYC provider
    /// </summary>
    [MaxLength(255)]
    public string? ExternalVerificationId
    {
        get => _externalVerificationId;
        set => _externalVerificationId = value;
    }

    /// <summary>
    /// Verification level (basic, enhanced, full)
    /// </summary>
    [MaxLength(50)]
    public string? VerificationLevel
    {
        get => _verificationLevel;
        set => _verificationLevel = value;
    }

    /// <summary>
    /// Document types submitted for verification
    /// </summary>
    [MaxLength(500)]
    public string? DocumentTypes
    {
        get => _documentTypes;
        set => _documentTypes = value;
    }

    /// <summary>
    /// Country of the submitted documents
    /// </summary>
    [MaxLength(2)]
    public string? DocumentCountry
    {
        get => _documentCountry;
        set => _documentCountry = value;
    }

    /// <summary>
    /// Date when verification was submitted
    /// </summary>
    public DateTime SubmittedAt
    {
        get => _submittedAt;
        set => _submittedAt = value;
    }

    /// <summary>
    /// Date when verification was completed (approved/rejected)
    /// </summary>
    public DateTime? CompletedAt
    {
        get => _completedAt;
        set => _completedAt = value;
    }

    /// <summary>
    /// Date when verification expires and needs renewal
    /// </summary>
    public DateTime? ExpiresAt
    {
        get => _expiresAt;
        set => _expiresAt = value;
    }

    /// <summary>
    /// Reason for rejection or additional notes
    /// </summary>
    [MaxLength(1000)]
    public string? Notes
    {
        get => _notes;
        set => _notes = value;
    }

    /// <summary>
    /// Additional metadata from the KYC provider
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? ProviderData
    {
        get => _providerData;
        set => _providerData = value;
    }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User.Models.User User
    {
        get => _user;
        set => _user = value;
    }
}

public class UserKycVerificationConfiguration : IEntityTypeConfiguration<UserKycVerification>
{
    public void Configure(EntityTypeBuilder<UserKycVerification> builder)
    {
        // Configure relationship with User (can't be done with annotations)
        builder.HasOne(ukv => ukv.User)
            .WithMany()
            .HasForeignKey(ukv => ukv.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
