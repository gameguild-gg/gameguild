using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Projects;

internal sealed class ProjectFeedbackConfiguration : IEntityTypeConfiguration<ProjectFeedback> {
  public void Configure(EntityTypeBuilder<ProjectFeedback> builder) {
    ArgumentNullException.ThrowIfNull(builder);
    
    // Configure the relationship with User as optional to avoid query filter warnings
    builder.HasOne(pf => pf.User)
           .WithMany()
           .HasForeignKey(pf => pf.UserId)
           .IsRequired(false) // Make the relationship optional
           .OnDelete(DeleteBehavior.SetNull);

    // Configure the relationship with Project
    // This explicitly maps to the Feedbacks collection on Project
    builder.HasOne(pf => pf.Project)
           .WithMany(p => p.Feedbacks)
           .HasForeignKey(pf => pf.ProjectId)
           .OnDelete(DeleteBehavior.Cascade);

    // Configure unique constraint
    builder.HasIndex(pf => new { pf.ProjectId, pf.UserId })
           .IsUnique()
           .HasDatabaseName("IX_ProjectFeedbacks_Project_User");

    // Additional indexes
    builder.HasIndex(pf => new { pf.ProjectId, pf.Rating })
           .HasDatabaseName("IX_ProjectFeedbacks_Project_Rating");
           
    builder.HasIndex(pf => pf.UserId)
           .HasDatabaseName("IX_ProjectFeedbacks_User");
           
    builder.HasIndex(pf => pf.CreatedAt)
           .HasDatabaseName("IX_ProjectFeedbacks_Date");

    // Configure properties
    builder.Property(pf => pf.Rating)
           .IsRequired();
  }
}
