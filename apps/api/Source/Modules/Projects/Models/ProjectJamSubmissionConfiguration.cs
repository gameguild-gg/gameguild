using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Modules.GameJams.Models;


namespace GameGuild.Modules.Projects;

internal sealed class ProjectJamSubmissionConfiguration : IEntityTypeConfiguration<ProjectJamSubmission> {
  public void Configure(EntityTypeBuilder<ProjectJamSubmission> builder) {
    ArgumentNullException.ThrowIfNull(builder);
    
    // Configure the relationship with Jam as optional to avoid query filter warnings
    builder.HasOne(pjs => pjs.Jam)
           .WithMany()
           .HasForeignKey(pjs => pjs.JamId)
           .IsRequired(false) // Make the relationship optional
           .OnDelete(DeleteBehavior.SetNull);

    // Configure the relationship with Project
    // This explicitly maps to the JamSubmissions collection on Project
    builder.HasOne(pjs => pjs.Project)
           .WithMany(p => p.JamSubmissions)
           .HasForeignKey(pjs => pjs.ProjectId)
           .OnDelete(DeleteBehavior.Cascade);

    // Configure unique constraint
    builder.HasIndex(pjs => new { pjs.ProjectId, pjs.JamId })
           .IsUnique()
           .HasDatabaseName("IX_ProjectJamSubmissions_Project_Jam");

    // Additional indexes
    builder.HasIndex(pjs => pjs.JamId)
           .HasDatabaseName("IX_ProjectJamSubmissions_Jam");
           
    builder.HasIndex(pjs => pjs.SubmittedAt)
           .HasDatabaseName("IX_ProjectJamSubmissions_Date");
           
    builder.HasIndex(pjs => pjs.FinalScore)
           .HasDatabaseName("IX_ProjectJamSubmissions_Score");

    // Configure properties
    builder.Property(pjs => pjs.SubmittedAt)
           .IsRequired();
           
    builder.Property(pjs => pjs.IsEligible)
           .HasDefaultValue(true);
  }
}
