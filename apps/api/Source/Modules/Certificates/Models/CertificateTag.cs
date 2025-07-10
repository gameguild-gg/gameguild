using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common;
using GameGuild.Modules.Tags.Models;
using Microsoft.EntityFrameworkCore;


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
  [ForeignKey(nameof(CertificateId))] public virtual Certificate Certificate { get; set; } = null!;

  [ForeignKey(nameof(TagId))] public virtual TagProficiency Tag { get; set; } = null!;
}
