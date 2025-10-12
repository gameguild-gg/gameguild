namespace GameGuild.Modules.Certificates;

public class AddCertificateTagDto {
  [Required] public Guid TagId { get; set; }

  [Required] public CertificateTagRelationshipType RelationshipType { get; set; } = CertificateTagRelationshipType.Demonstrates;
}
