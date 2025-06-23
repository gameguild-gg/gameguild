using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;

namespace GameGuild.Modules.Tag.Models;

[Table("tag_relationships")]
[Index(nameof(SourceId), nameof(TargetId), IsUnique = true)]
[Index(nameof(SourceId))]
[Index(nameof(TargetId))]
[Index(nameof(Type))]
public class TagRelationship : BaseEntity
{
    private Guid _sourceId;

    private Guid _targetId;

    private TagRelationshipType _type;

    private decimal? _weight;

    private string? _metadata;

    private Tag _source = null!;

    private Tag _target = null!;

    public Guid SourceId
    {
        get => _sourceId;
        set => _sourceId = value;
    }

    public Guid TargetId
    {
        get => _targetId;
        set => _targetId = value;
    }

    public TagRelationshipType Type
    {
        get => _type;
        set => _type = value;
    }

    /// <summary>
    /// Weight or strength of the relationship (optional)
    /// </summary>
    [Column(TypeName = "decimal(3,2)")]
    public decimal? Weight
    {
        get => _weight;
        set => _weight = value;
    }

    /// <summary>
    /// Additional metadata about the relationship
    /// </summary>
    [MaxLength(500)]
    public string? Metadata
    {
        get => _metadata;
        set => _metadata = value;
    }

    // Navigation properties
    [ForeignKey(nameof(SourceId))]
    public virtual Tag Source
    {
        get => _source;
        set => _source = value;
    }

    [ForeignKey(nameof(TargetId))]
    public virtual Tag Target
    {
        get => _target;
        set => _target = value;
    }
}

public class TagRelationshipConfiguration : IEntityTypeConfiguration<TagRelationship>
{
    public void Configure(EntityTypeBuilder<TagRelationship> builder)
    {
        // Configure relationship with Source (can't be done with annotations)
        builder.HasOne(tr => tr.Source)
            .WithMany(t => t.SourceRelationships)
            .HasForeignKey(tr => tr.SourceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure relationship with Target (can't be done with annotations)
        builder.HasOne(tr => tr.Target)
            .WithMany(t => t.TargetRelationships)
            .HasForeignKey(tr => tr.TargetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Check constraint to prevent self-referencing (can't be done with annotations)
        builder.ToTable(t => t.HasCheckConstraint(
                "CK_TagRelationships_NoSelfReference",
                "\"SourceId\" != \"TargetId\""
            )
        );
    }
}
