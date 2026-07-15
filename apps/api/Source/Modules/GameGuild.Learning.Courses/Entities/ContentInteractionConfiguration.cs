

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

 namespace GameGuild.Learning.Courses;

/// <summary> EntityBase Framework configuration for ContentInteraction entity </summary>
public class ContentInteractionConfiguration : IEntityTypeConfiguration<ContentInteraction> {
  public void Configure(EntityTypeBuilder<ContentInteraction> builder) {
    builder.ToTable(table => table.HasCheckConstraint(
      "CK_content_interactions_TimeSpentSeconds_NonNegative",
      "\"TimeSpentSeconds\" >= 0"));

    builder.HasIndex(interaction => new { interaction.UserId, interaction.ContentId })
      .HasDatabaseName("IX_content_interactions_UserId_ContentId")
      .IsUnique()
      .HasFilter("\"SubmittedAt\" IS NULL AND \"DeletedAt\" IS NULL");

    // Configure relationship with ProgramUser (can't be done with annotations)
    builder.HasOne(ci => ci.ProgramUser).WithMany(pu => pu.ContentInteractions).HasForeignKey(ci => ci.ProgramUserId).OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with Content (can't be done with annotations)
    builder.HasOne(ci => ci.Content).WithMany().HasForeignKey(ci => ci.ContentId).OnDelete(DeleteBehavior.Cascade);
  }
}
