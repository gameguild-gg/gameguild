using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Tags.Models;

[Table("tag_relationships")]
[Index(nameof(SourceId), nameof(TargetId), IsUnique = true)]
[Index(nameof(SourceId))]
[Index(nameof(TargetId))]
[Index(nameof(Type))]
public class TagRelationship : Entity {
  public Guid SourceId { get; set; }

  public Guid TargetId { get; set; }

  public TagRelationshipType Type { get; set; }

  /// <summary>
  /// Weight or strength of the relationship (optional)
  /// </summary>
  [Column(TypeName = "decimal(3,2)")]
  public decimal? Weight { get; set; }

  /// <summary>
  /// Additional metadata about the relationship
  /// </summary>
  [MaxLength(500)]
  public string? Metadata { get; set; }

  // Navigation properties
  [ForeignKey(nameof(SourceId))] public virtual Tag Source { get; set; } = null!;

  [ForeignKey(nameof(TargetId))] public virtual Tag Target { get; set; } = null!;
}
