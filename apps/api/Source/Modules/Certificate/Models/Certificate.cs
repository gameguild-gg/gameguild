using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;

namespace GameGuild.Modules.Certificate.Models;

[Table("certificates")]
[Index(nameof(Type))]
[Index(nameof(ProgramId))]
[Index(nameof(ProductId))]
[Index(nameof(CompletionPercentage))]
[Index(nameof(TenantId))]
public class Certificate : BaseEntity, ITenantable {
  private string _name = string.Empty;

  private string _description = string.Empty;

  private CertificateType _type;

  private Guid? _programId;

  private Guid? _productId;

  private decimal _completionPercentage = 100;

  private decimal? _minimumGrade;

  private bool _requiresFeedback = false;

  private bool _requiresRating = false;

  private decimal? _minimumRating;

  private int? _validityDays;

  private VerificationMethod _verificationMethod = VerificationMethod.Code;

  private string? _certificateTemplate;

  private bool _isActive = true;

  private Guid? _tenantId;

  private Program.Models.Program? _program;

  private Product.Models.Product? _product;

  private ICollection<UserCertificate> _userCertificates = new List<UserCertificate>();

  private ICollection<CertificateTag> _certificateTags = new List<CertificateTag>();

  [Required]
  [MaxLength(255)]
  public string Name {
    get => _name;
    set => _name = value;
  }

  public string Description {
    get => _description;
    set => _description = value;
  }

  public CertificateType Type {
    get => _type;
    set => _type = value;
  }

  /// <summary>
  /// Program that this certificate is associated with (null for non-program certificates)
  /// </summary>
  public Guid? ProgramId {
    get => _programId;
    set => _programId = value;
  }

  /// <summary>
  /// Product that this certificate is associated with (null for non-product certificates)
  /// </summary>
  public Guid? ProductId {
    get => _productId;
    set => _productId = value;
  }

  /// <summary>
  /// Required completion percentage for program-based certificates (0-100)
  /// </summary>
  [Column(TypeName = "decimal(5,2)")]
  public decimal CompletionPercentage {
    get => _completionPercentage;
    set => _completionPercentage = value;
  }

  /// <summary>
  /// Minimum grade required for certificate issuance (0-100, null = no minimum)
  /// </summary>
  [Column(TypeName = "decimal(5,2)")]
  public decimal? MinimumGrade {
    get => _minimumGrade;
    set => _minimumGrade = value;
  }

  /// <summary>
  /// Whether feedback submission is required for certificate issuance
  /// </summary>
  public bool RequiresFeedback {
    get => _requiresFeedback;
    set => _requiresFeedback = value;
  }

  /// <summary>
  /// Whether rating submission is required for certificate issuance
  /// </summary>
  public bool RequiresRating {
    get => _requiresRating;
    set => _requiresRating = value;
  }

  /// <summary>
  /// Minimum rating required if rating is required (1-5, null = any rating accepted)
  /// </summary>
  [Column(TypeName = "decimal(2,1)")]
  public decimal? MinimumRating {
    get => _minimumRating;
    set => _minimumRating = value;
  }

  /// <summary>
  /// How long the certificate remains valid (in days, null = never expires)
  /// </summary>
  public int? ValidityDays {
    get => _validityDays;
    set => _validityDays = value;
  }

  public VerificationMethod VerificationMethod {
    get => _verificationMethod;
    set => _verificationMethod = value;
  }

  /// <summary>
  /// Template for certificate design/layout
  /// </summary>
  [MaxLength(500)]
  public string? CertificateTemplate {
    get => _certificateTemplate;
    set => _certificateTemplate = value;
  }

  public bool IsActive {
    get => _isActive;
    set => _isActive = value;
  }

  public Guid? TenantId {
    get => _tenantId;
    set => _tenantId = value;
  }

  // Navigation properties
  public virtual Program.Models.Program? Program {
    get => _program;
    set => _program = value;
  }

  public virtual Product.Models.Product? Product {
    get => _product;
    set => _product = value;
  }

  public virtual ICollection<UserCertificate> UserCertificates {
    get => _userCertificates;
    set => _userCertificates = value;
  }

  public virtual ICollection<CertificateTag> CertificateTags {
    get => _certificateTags;
    set => _certificateTags = value;
  }
}

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate> {
  public void Configure(EntityTypeBuilder<Certificate> builder) {
    // Configure relationship with Program (can't be done with annotations)
    builder.HasOne(c => c.Program).WithMany().HasForeignKey(c => c.ProgramId).OnDelete(DeleteBehavior.SetNull);

    // Configure relationship with Product (can't be done with annotations)
    builder.HasOne(c => c.Product).WithMany().HasForeignKey(c => c.ProductId).OnDelete(DeleteBehavior.SetNull);
  }
}
