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
            entity.Property(e => e.SubmissionModalities).HasConversion<int>();
            entity.Property(e => e.PresentationMode).HasConversion<int>();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_Assessments_SubmissionModalities",
                    "\"SubmissionModalities\" > 0 AND (\"SubmissionModalities\" & ~127) = 0");
                table.HasCheckConstraint(
                    "CK_Assessments_PresentationMode",
                    "\"PresentationMode\" IN (0, 1)");
                table.HasCheckConstraint(
                    "CK_Assessments_DeliverySchedule",
                    "(\"AvailableFrom\" IS NULL OR \"AvailableUntil\" IS NULL OR \"AvailableFrom\" <= \"AvailableUntil\") AND " +
                    "(\"DueAt\" IS NULL OR \"AvailableFrom\" IS NULL OR \"DueAt\" >= \"AvailableFrom\") AND " +
                    "(\"DueAt\" IS NULL OR \"AvailableUntil\" IS NULL OR \"DueAt\" <= \"AvailableUntil\") AND " +
                    "(NOT \"AllowLateSubmissions\" OR \"DueAt\" IS NOT NULL) AND " +
                    "(\"LateSubmissionDeadline\" IS NULL OR (\"AllowLateSubmissions\" AND \"DueAt\" IS NOT NULL AND \"LateSubmissionDeadline\" > \"DueAt\" AND (\"AvailableUntil\" IS NULL OR \"LateSubmissionDeadline\" <= \"AvailableUntil\")))");
            });
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
            entity.Property(e => e.SubmittedModalities).HasConversion<int>();
            entity.Property(e => e.TextPayload).HasColumnType("text");
            entity.Property(e => e.FilePayload).HasMaxLength(2048);
            entity.Property(e => e.UrlPayload).HasMaxLength(2048);
            entity.Property(e => e.CodePayload).HasColumnType("text");
            entity.Property(e => e.MediaPayload).HasMaxLength(2048);
            entity.Property(e => e.ProjectPayload).HasMaxLength(2048);
            entity.Property(e => e.StructuredAnswerPayload).HasColumnType("jsonb");
        });

        modelBuilder.Entity<InteractiveVideoAssessmentCue>(entity =>
        {
            entity.ToTable("InteractiveVideoAssessmentCues");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CueId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.CuePositionSeconds).HasPrecision(12, 3);
            entity.HasIndex(e => new { e.AssessmentId, e.ContentId, e.CueId }).IsUnique();
            entity.HasIndex(e => e.ContentId);
            entity.HasOne(e => e.Assessment)
                .WithMany(e => e.InteractiveVideoCues)
                .HasForeignKey(e => e.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
