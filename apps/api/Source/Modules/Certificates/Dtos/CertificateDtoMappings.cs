using GameGuild.Common;
using GameGuild.Modules.Certificates;


namespace GameGuild.Source.Modules.Certificates.Dtos;

public static class CertificateDtoMappings {
  public static Certificate ToEntity(this CreateCertificateDto dto, Guid programId) => new() {
    ProgramId = programId,
    Name = dto.Name,
    Description = dto.Description,
    Type = dto.Type,
    CompletionPercentage = dto.CompletionPercentage,
    MinimumGrade = dto.MinimumGrade,
    RequiresFeedback = dto.RequiresFeedback,
    RequiresRating = dto.RequiresRating,
    MinimumRating = dto.MinimumRating,
    ValidityDays = dto.ValidityDays,
    VerificationMethod = dto.VerificationMethod,
    CertificateTemplate = dto.CertificateTemplate,
    IsActive = dto.IsActive
  };

  public static void ApplyUpdates(this Certificate entity, UpdateCertificateDto dto) {
    entity.Name = dto.Name;
    entity.Description = dto.Description;
    entity.Type = dto.Type;
    entity.CompletionPercentage = dto.CompletionPercentage;
    entity.MinimumGrade = dto.MinimumGrade;
    entity.RequiresFeedback = dto.RequiresFeedback;
    entity.RequiresRating = dto.RequiresRating;
    entity.MinimumRating = dto.MinimumRating;
    entity.ValidityDays = dto.ValidityDays;
    entity.VerificationMethod = dto.VerificationMethod;
    entity.CertificateTemplate = dto.CertificateTemplate;
    entity.IsActive = dto.IsActive;
  }
}
