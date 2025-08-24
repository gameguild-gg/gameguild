using GameGuild.Common;
using GameGuild.Modules.Tags.Models;


namespace GameGuild.Modules.Certificates;

[Table("certificate_tags")]
[Index(nameof(CertificateId), nameof(TagId), IsUnique = true)]
[Index(nameof(CertificateId))]
[Index(nameof(TagId))]
[Index(nameof(RelationshipType))]
public class CertificateTag : Entity {
  public Guid CertificateId { get; set; }

  public Guid TagId { get; set; }

  public CertificateTagRelationshipType RelationshipType { get; set; }

  // Navigation properties
  public virtual Certificate Certificate { get; set; } = null!;

  public virtual TagProficiency Tag { get; set; } = null!;
}
