using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Tags.Models;

public class TagRelationshipConfiguration : IEntityTypeConfiguration<TagRelationship> {
  public void Configure(EntityTypeBuilder<TagRelationship> builder) {
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
    builder.ToTable(t => t.HasCheckConstraint("CK_TagRelationships_NoSelfReference", "\"SourceId\" != \"TargetId\""));
  }
}
