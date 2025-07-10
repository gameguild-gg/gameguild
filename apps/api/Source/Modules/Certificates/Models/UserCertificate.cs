using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common;
using GameGuild.Modules.Products.Models;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Certificates.Models;

[Table("user_certificates")]
[Index(nameof(UserId))]
[Index(nameof(CertificateId))]
[Index(nameof(ProgramId))]
[Index(nameof(ProductId))]
[Index(nameof(ProgramUserId))]
[Index(nameof(VerificationCode), IsUnique = true)]
[Index(nameof(Status))]
[Index(nameof(IssuedAt))]
public class UserCertificate : Entity {
  [Required] [ForeignKey(nameof(User))] public Guid UserId { get; set; }

  [Required]
  [ForeignKey(nameof(Certificate))]
  public Guid CertificateId { get; set; }

  /// <summary>
  /// Program associated with this certificate issuance (null for non-program certificates)
  /// </summary>
  public Guid? ProgramId { get; set; }

  /// <summary>
  /// Product associated with this certificate issuance (null for non-product certificates)
  /// </summary>
  public Guid? ProductId { get; set; }

  /// <summary>
  /// Program user record for program-based certificates
  /// </summary>
  public Guid? ProgramUserId { get; set; }

  /// <summary>
  /// Unique verification code for this certificate
  /// </summary>
  [Required]
  [MaxLength(100)]
  public string VerificationCode { get; set; } = string.Empty;

  /// <summary>
  /// Final grade or score achieved for this certificate
  /// </summary>
  [Column(TypeName = "decimal(5,2)")]
  public decimal? FinalGrade { get; set; }

  /// <summary>
  /// Additional metadata about the certificate issuance
  /// </summary>
  [MaxLength(1000)]
  public string? Metadata { get; set; }

  public CertificateStatus Status { get; set; } = CertificateStatus.Active;

  /// <summary>
  /// Date when the certificate was issued
  /// </summary>
  public DateTime IssuedAt { get; set; }

  /// <summary>
  /// Date when the certificate expires (null if never expires)
  /// </summary>
  public DateTime? ExpiresAt { get; set; }

  /// <summary>
  /// Date when the certificate was revoked (null if not revoked)
  /// </summary>
  public DateTime? RevokedAt { get; set; }

  /// <summary>
  /// Reason for revocation (null if not revoked)
  /// </summary>
  [MaxLength(500)]
  public string? RevocationReason { get; set; }

  // Navigation properties
  public virtual User User { get; set; } = null!;

  public virtual Certificate Certificate { get; set; } = null!;

  public virtual Programs.Models.Program? Program { get; set; }

  public virtual Product? Product { get; set; }

  public virtual ProgramUser? ProgramUser { get; set; }

  public virtual ICollection<CertificateBlockchainAnchor> BlockchainAnchors { get; set; } = new List<CertificateBlockchainAnchor>();
}
