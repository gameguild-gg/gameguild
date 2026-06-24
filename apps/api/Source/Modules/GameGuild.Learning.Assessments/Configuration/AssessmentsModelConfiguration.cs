using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Assessments;

/// <summary>
///     EF Core model configuration for the Learning.Assessments module.
///     Discovered by ApplicationDbContext via assembly scanning.
/// </summary>
public sealed class AssessmentsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.ToTable("Assessments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.HasIndex(e => e.CourseId);
            entity.HasIndex(e => e.AssessmentGroupId);
            entity.HasOne(e => e.AssessmentGroup)
                .WithMany()
                .HasForeignKey(e => e.AssessmentGroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AssessmentGroup>(entity =>
        {
            entity.ToTable("AssessmentGroups");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(160).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.WeightPercent).HasPrecision(5, 2);
            entity.HasIndex(e => e.CourseId);
            entity.HasIndex(e => new { e.CourseId, e.Order });
        });

        modelBuilder.Entity<AssessmentSubmission>(entity =>
        {
            entity.ToTable("AssessmentSubmissions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AssessmentId);
            entity.HasIndex(e => e.EnrollmentId);
            entity.HasIndex(e => e.UserId);
        });
    }
}
