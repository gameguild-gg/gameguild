using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectFollowerEntity = GameGuild.Projects.ProjectFollower;

namespace GameGuild.Projects;

internal sealed class ProjectFollowerConfiguration : IEntityTypeConfiguration<ProjectFollowerEntity> {
  public void Configure(EntityTypeBuilder<ProjectFollowerEntity> builder) {
    ArgumentNullException.ThrowIfNull(builder);

    // Configure the relationship with User as optional to avoid query filter warnings
    builder.HasOne(pf => pf.User)
           .WithMany()
           .HasForeignKey(pf => pf.UserId)
           .IsRequired(false) // Make the relationship optional
           .OnDelete(DeleteBehavior.SetNull);

    // Configure the relationship with Project
    // This explicitly maps to the Followers collection on Project
    builder.HasOne(pf => pf.Project).WithMany(p => p.Followers).HasForeignKey(pf => pf.ProjectId).OnDelete(DeleteBehavior.Cascade);

    // Configure unique constraint
    builder.HasIndex(pf => new { pf.ProjectId, pf.UserId }).IsUnique().HasDatabaseName("IX_ProjectFollowers_Project_User");

    // Additional indexes
    builder.HasIndex(pf => pf.UserId).HasDatabaseName("IX_ProjectFollowers_User");

    builder.HasIndex(pf => pf.FollowedAt).HasDatabaseName("IX_ProjectFollowers_Date");

    // Configure properties
    builder.Property(pf => pf.FollowedAt).IsRequired();

    builder.Property(pf => pf.NotificationSettings).HasMaxLength(1000);
  }
}
