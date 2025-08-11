using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Projects;

internal sealed class ProjectCollaboratorConfiguration : IEntityTypeConfiguration<ProjectCollaborator> {
  public void Configure(EntityTypeBuilder<ProjectCollaborator> builder) {
    ArgumentNullException.ThrowIfNull(builder);
    
    // Configure the relationship with User as optional to avoid query filter warnings
    builder.HasOne(pc => pc.User)
           .WithMany()
           .HasForeignKey(pc => pc.UserId)
           .IsRequired(false) // Make the relationship optional
           .OnDelete(DeleteBehavior.SetNull);

    // Configure the relationship with Project
    builder.HasOne(pc => pc.Project)
           .WithMany()
           .HasForeignKey(pc => pc.ProjectId)
           .OnDelete(DeleteBehavior.Cascade);

    // Configure unique constraint
    builder.HasIndex(pc => new { pc.ProjectId, pc.UserId })
           .IsUnique()
           .HasDatabaseName("IX_ProjectCollaborators_Project_User");

    // Additional indexes
    builder.HasIndex(pc => pc.UserId)
           .HasDatabaseName("IX_ProjectCollaborators_User");

    // Configure properties
    builder.Property(pc => pc.Role)
           .HasMaxLength(100)
           .IsRequired();

    builder.Property(pc => pc.Permissions)
           .HasMaxLength(500)
           .IsRequired();
  }
}
