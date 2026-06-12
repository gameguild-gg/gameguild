using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Cohorts;

/// <summary>
///     EF Core model configuration for course cohorts and scheduling.
/// </summary>
public sealed class CohortsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cohort>(entity =>
        {
            entity.ToTable("learning_cohorts");
            entity.HasKey(cohort => cohort.Id);
            entity.Property(cohort => cohort.Name).HasMaxLength(250).IsRequired();
            entity.Property(cohort => cohort.Description).HasMaxLength(2000);
            entity.Property(cohort => cohort.MeetingSchedule).HasMaxLength(4000);
            entity.Property(cohort => cohort.Status).HasConversion<string>().HasMaxLength(40);
            entity.HasIndex(cohort => cohort.CourseId);
            entity.HasIndex(cohort => new { cohort.CourseId, cohort.Status, cohort.IsOpen });
            entity.HasIndex(cohort => cohort.InstructorId);
            entity.HasIndex(cohort => cohort.TenantId);
        });
    }
}
