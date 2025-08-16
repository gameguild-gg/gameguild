using System.ComponentModel.DataAnnotations;
using GameGuild.Common;


namespace GameGuild.Source.Modules.Certificates.Dtos;

public class UpdateCertificateDto {
  [Required]
  public Guid Id { get; set; }

  [Required]
  [MaxLength(255)]
  public string Name { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  [Required]
  public CertificateType Type { get; set; } = CertificateType.ProgramCompletion;

  [Range(0, 100)]
  public decimal CompletionPercentage { get; set; } = 100m;

  [Range(0, 100)]
  public decimal? MinimumGrade { get; set; } = null;

  public bool RequiresFeedback { get; set; } = false;

  public bool RequiresRating { get; set; } = false;

  [Range(1, 5)]
  public decimal? MinimumRating { get; set; } = null;

  public int? ValidityDays { get; set; } = null;

  public VerificationMethod VerificationMethod { get; set; } = VerificationMethod.Code;

  [MaxLength(500)]
  public string? CertificateTemplate { get; set; } = null;

  public bool IsActive { get; set; } = true;
}
