using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;
using GameGuild.Modules.Program.Models;

namespace GameGuild.Modules.Certificate.Models;

[Table("user_certificates")]
[Index(nameof(UserId))]
[Index(nameof(CertificateId))]
[Index(nameof(ProgramId))]
[Index(nameof(ProductId))]
[Index(nameof(ProgramUserId))]
[Index(nameof(VerificationCode), IsUnique = true)]
[Index(nameof(Status))]
[Index(nameof(IssuedAt))]
public class UserCertificate : BaseEntity
{
    private Guid _userId;

    private Guid _certificateId;

    private Guid? _programId;

    private Guid? _productId;

    private Guid? _programUserId;

    private string _verificationCode = string.Empty;

    private decimal? _finalGrade;

    private string? _metadata;

    private CertificateStatus _status = CertificateStatus.Active;

    private DateTime _issuedAt;

    private DateTime? _expiresAt;

    private DateTime? _revokedAt;

    private string? _revocationReason;

    private User.Models.User _user = null!;

    private Certificate _certificate = null!;

    private Program.Models.Program? _program;

    private Product.Models.Product? _product;

    private ProgramUser? _programUser;

    private ICollection<CertificateBlockchainAnchor> _blockchainAnchors = new List<CertificateBlockchainAnchor>();

    [Required]
    [ForeignKey(nameof(User))]
    public Guid UserId
    {
        get => _userId;
        set => _userId = value;
    }

    [Required]
    [ForeignKey(nameof(Certificate))]
    public Guid CertificateId
    {
        get => _certificateId;
        set => _certificateId = value;
    }

    /// <summary>
    /// Program associated with this certificate issuance (null for non-program certificates)
    /// </summary>
    public Guid? ProgramId
    {
        get => _programId;
        set => _programId = value;
    }

    /// <summary>
    /// Product associated with this certificate issuance (null for non-product certificates)
    /// </summary>
    public Guid? ProductId
    {
        get => _productId;
        set => _productId = value;
    }

    /// <summary>
    /// Program user record for program-based certificates
    /// </summary>
    public Guid? ProgramUserId
    {
        get => _programUserId;
        set => _programUserId = value;
    }

    /// <summary>
    /// Unique verification code for this certificate
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string VerificationCode
    {
        get => _verificationCode;
        set => _verificationCode = value;
    }

    /// <summary>
    /// Final grade or score achieved for this certificate
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? FinalGrade
    {
        get => _finalGrade;
        set => _finalGrade = value;
    }

    /// <summary>
    /// Additional metadata about the certificate issuance
    /// </summary>
    [MaxLength(1000)]
    public string? Metadata
    {
        get => _metadata;
        set => _metadata = value;
    }

    public CertificateStatus Status
    {
        get => _status;
        set => _status = value;
    }

    /// <summary>
    /// Date when the certificate was issued
    /// </summary>
    public DateTime IssuedAt
    {
        get => _issuedAt;
        set => _issuedAt = value;
    }

    /// <summary>
    /// Date when the certificate expires (null if never expires)
    /// </summary>
    public DateTime? ExpiresAt
    {
        get => _expiresAt;
        set => _expiresAt = value;
    }

    /// <summary>
    /// Date when the certificate was revoked (null if not revoked)
    /// </summary>
    public DateTime? RevokedAt
    {
        get => _revokedAt;
        set => _revokedAt = value;
    }

    /// <summary>
    /// Reason for revocation (null if not revoked)
    /// </summary>
    [MaxLength(500)]
    public string? RevocationReason
    {
        get => _revocationReason;
        set => _revocationReason = value;
    }

    // Navigation properties
    public virtual User.Models.User User
    {
        get => _user;
        set => _user = value;
    }

    public virtual Certificate Certificate
    {
        get => _certificate;
        set => _certificate = value;
    }

    public virtual Program.Models.Program? Program
    {
        get => _program;
        set => _program = value;
    }

    public virtual Product.Models.Product? Product
    {
        get => _product;
        set => _product = value;
    }

    public virtual Program.Models.ProgramUser? ProgramUser
    {
        get => _programUser;
        set => _programUser = value;
    }

    public virtual ICollection<CertificateBlockchainAnchor> BlockchainAnchors
    {
        get => _blockchainAnchors;
        set => _blockchainAnchors = value;
    }
}

public class UserCertificateConfiguration : IEntityTypeConfiguration<UserCertificate>
{
    public void Configure(EntityTypeBuilder<UserCertificate> builder)
    {
        // Configure relationship with Certificate (can't be done with annotations)
        builder.HasOne(uc => uc.Certificate)
            .WithMany()
            .HasForeignKey(uc => uc.CertificateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure relationship with User (can't be done with annotations)
        builder.HasOne(uc => uc.User)
            .WithMany()
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure optional relationship with Program (can't be done with annotations)
        builder.HasOne(uc => uc.Program)
            .WithMany()
            .HasForeignKey(uc => uc.ProgramId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure optional relationship with Product (can't be done with annotations)
        builder.HasOne(uc => uc.Product)
            .WithMany()
            .HasForeignKey(uc => uc.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure optional relationship with ProgramUser (can't be done with annotations)
        builder.HasOne(uc => uc.ProgramUser)
            .WithMany(pu => pu.UserCertificates)
            .HasForeignKey(uc => uc.ProgramUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
