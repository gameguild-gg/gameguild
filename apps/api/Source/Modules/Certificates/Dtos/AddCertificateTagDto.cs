using System.ComponentModel.DataAnnotations;
using GameGuild.Common;


namespace GameGuild.Source.Modules.Certificates.Dtos;

public class AddCertificateTagDto {
  [Required]
  public Guid TagId { get; set; }

  [Required]
  public CertificateTagRelationshipType RelationshipType { get; set; } = CertificateTagRelationshipType.Demonstrates;
}
