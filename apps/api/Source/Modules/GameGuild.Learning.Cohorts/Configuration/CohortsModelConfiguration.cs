using System.Text.Json;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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

        modelBuilder.Entity<CohortSchedule>(entity =>
        {
            entity.ToTable("learning_cohort_schedules");
            entity.HasKey(schedule => schedule.Id);
            entity.Property(schedule => schedule.TimezoneId).HasMaxLength(100).IsRequired();
            entity.Property(schedule => schedule.MeetingStartTime).IsRequired();
            entity.Property(schedule => schedule.MeetingDurationMinutes).IsRequired();
            entity.Property(schedule => schedule.PacingMode).HasConversion<int>();
            entity.Property(schedule => schedule.ReleasePolicy).HasConversion<int>();
            entity.Property(schedule => schedule.MeetingDays)
                .HasConversion(
                    meetingDays => SerializeMeetingDays(meetingDays),
                    serialized => DeserializeMeetingDays(serialized))
                .Metadata.SetValueComparer(MeetingDaysComparer);

            entity.HasIndex(schedule => schedule.CohortId).IsUnique();
            entity.HasIndex(schedule => schedule.TenantId);

            entity.HasOne<Cohort>()
                .WithOne()
                .HasForeignKey<CohortSchedule>(schedule => schedule.CohortId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CohortScheduleItem>(entity =>
        {
            entity.ToTable("learning_cohort_schedule_items");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Type).HasConversion<int>();
            entity.Property(item => item.Status).HasConversion<int>();
            entity.Property(item => item.VisibilityOverride).HasConversion<int>();
            entity.Property(item => item.Title).HasMaxLength(500);
            entity.Property(item => item.Location).HasMaxLength(500);
            entity.Property(item => item.MeetingUrl).HasMaxLength(2000);

            entity.HasIndex(item => new { item.CohortId, item.InstructionalWeek, item.SortOrder });
            entity.HasIndex(item => item.ProgramContentId);
            entity.HasIndex(item => item.AssessmentId);
            entity.HasIndex(item => item.TenantId);

            entity.HasOne<CohortSchedule>()
                .WithMany()
                .HasForeignKey(item => item.CohortId)
                .HasPrincipalKey(schedule => schedule.CohortId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<ProgramContent>()
                .WithMany()
                .HasForeignKey(item => item.ProgramContentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static readonly ValueComparer<DayOfWeek[]> MeetingDaysComparer = new(
        (left, right) => left != null && right != null && left.SequenceEqual(right),
        value => value.Aggregate(0, (hash, day) => HashCode.Combine(hash, day)),
        value => value.ToArray());

    private static string SerializeMeetingDays(DayOfWeek[] meetingDays) =>
        JsonSerializer.Serialize(meetingDays);

    private static DayOfWeek[] DeserializeMeetingDays(string serialized) =>
        JsonSerializer.Deserialize<DayOfWeek[]>(serialized) ?? [];
}
