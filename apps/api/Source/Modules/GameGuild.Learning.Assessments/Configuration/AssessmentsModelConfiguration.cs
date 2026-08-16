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
            entity.Property(e => e.DefinitionPayload).HasColumnType("jsonb");
            entity.Property(e => e.DefinitionSchemaVersion).HasDefaultValue(1);
            entity.Property(e => e.SubmissionModalities).HasConversion<int>();
            entity.Property(e => e.PresentationMode).HasConversion<int>();
            entity.Property(e => e.GradingMethods).HasConversion<int>();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_Assessments_SubmissionModalities",
                    "\"SubmissionModalities\" > 0 AND (\"SubmissionModalities\" & ~127) = 0");
                table.HasCheckConstraint(
                    "CK_Assessments_PresentationMode",
                    "\"PresentationMode\" IN (0, 1)");
                table.HasCheckConstraint(
                    "CK_Assessments_GradingMethods",
                    "\"GradingMethods\" >= 0 AND (\"GradingMethods\" & ~15) = 0");
                table.HasCheckConstraint(
                    "CK_Assessments_ScoreRange",
                    "\"MaxScore\" > 0 AND \"PassingScore\" >= 0 AND \"PassingScore\" <= \"MaxScore\"");
                table.HasCheckConstraint(
                    "CK_Assessments_DeliverySchedule",
                    "(\"AvailableFrom\" IS NULL OR \"AvailableUntil\" IS NULL OR \"AvailableFrom\" <= \"AvailableUntil\") AND " +
                    "(\"DueAt\" IS NULL OR \"AvailableFrom\" IS NULL OR \"DueAt\" >= \"AvailableFrom\") AND " +
                    "(\"DueAt\" IS NULL OR \"AvailableUntil\" IS NULL OR \"DueAt\" <= \"AvailableUntil\") AND " +
                    "(NOT \"AllowLateSubmissions\" OR (\"DueAt\" IS NOT NULL AND \"LateSubmissionDeadline\" IS NOT NULL AND \"LateSubmissionDeadline\" > \"DueAt\" AND (\"AvailableUntil\" IS NULL OR \"LateSubmissionDeadline\" <= \"AvailableUntil\"))) AND " +
                    "(\"AllowLateSubmissions\" OR \"LateSubmissionDeadline\" IS NULL)");
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
            entity.HasIndex(e => new { e.AssessmentId, e.EnrollmentId, e.AttemptNumber })
                .IsUnique()
                .HasDatabaseName("UX_AssessmentSubmissions_Assessment_Enrollment_Attempt");
            entity.Property(e => e.SubmittedModalities).HasConversion<int>();
            entity.Property(e => e.TextPayload).HasColumnType("text");
            entity.Property(e => e.FilePayload).HasMaxLength(2048);
            entity.Property(e => e.UrlPayload).HasMaxLength(2048);
            entity.Property(e => e.CodePayload).HasColumnType("text");
            entity.Property(e => e.MediaPayload).HasMaxLength(2048);
            entity.Property(e => e.ProjectPayload).HasMaxLength(2048);
            entity.Property(e => e.StructuredAnswerPayload).HasColumnType("jsonb");
            entity.Property(e => e.RubricScoresPayload).HasColumnType("jsonb");
            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_AssessmentSubmissions_SubmittedModalities",
                    "\"SubmittedModalities\" >= 0 AND (\"SubmittedModalities\" & ~127) = 0");
                table.HasCheckConstraint(
                    "CK_AssessmentSubmissions_ScoreNonNegative",
                    "\"Score\" IS NULL OR \"Score\" >= 0");
                table.HasCheckConstraint(
                    "CK_AssessmentSubmissions_AttemptNumberPositive",
                    "\"AttemptNumber\" > 0");
                table.HasCheckConstraint(
                    "CK_AssessmentSubmissions_PayloadConsistency",
                    "((\"SubmittedModalities\" & 1) = 0 OR \"TextPayload\" IS NOT NULL) AND " +
                    "((\"SubmittedModalities\" & 2) = 0 OR \"FilePayload\" IS NOT NULL) AND " +
                    "((\"SubmittedModalities\" & 4) = 0 OR \"UrlPayload\" IS NOT NULL) AND " +
                    "((\"SubmittedModalities\" & 8) = 0 OR \"CodePayload\" IS NOT NULL) AND " +
                    "((\"SubmittedModalities\" & 16) = 0 OR \"MediaPayload\" IS NOT NULL) AND " +
                    "((\"SubmittedModalities\" & 32) = 0 OR \"ProjectPayload\" IS NOT NULL) AND " +
                    "((\"SubmittedModalities\" & 64) = 0 OR \"StructuredAnswerPayload\" IS NOT NULL) AND " +
                    "(\"TextPayload\" IS NULL OR (\"SubmittedModalities\" & 1) <> 0) AND " +
                    "(\"FilePayload\" IS NULL OR (\"SubmittedModalities\" & 2) <> 0) AND " +
                    "(\"UrlPayload\" IS NULL OR (\"SubmittedModalities\" & 4) <> 0) AND " +
                    "(\"CodePayload\" IS NULL OR (\"SubmittedModalities\" & 8) <> 0) AND " +
                    "(\"MediaPayload\" IS NULL OR (\"SubmittedModalities\" & 16) <> 0) AND " +
                    "(\"ProjectPayload\" IS NULL OR (\"SubmittedModalities\" & 32) <> 0) AND " +
                    "(\"StructuredAnswerPayload\" IS NULL OR (\"SubmittedModalities\" & 64) <> 0)");
            });
        });

        modelBuilder.Entity<CourseGroupSet>(entity =>
        {
            entity.ToTable("CourseGroupSets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(160).IsRequired();
            entity.HasIndex(e => new { e.CourseId, e.Name }).IsUnique();
        });

        modelBuilder.Entity<CourseGroup>(entity =>
        {
            entity.ToTable("CourseGroups");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(160).IsRequired();
            entity.HasIndex(e => e.GroupSetId);
        });

        modelBuilder.Entity<CourseGroupMember>(entity =>
        {
            entity.ToTable("CourseGroupMembers");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.GroupId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<AssessmentRubric>(entity =>
        {
            entity.ToTable("AssessmentRubrics");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(160).IsRequired();
        });

        modelBuilder.Entity<RubricCriterion>(entity =>
        {
            entity.ToTable("RubricCriteria");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired();
            entity.HasIndex(e => e.RubricId);
        });

        modelBuilder.Entity<AssessmentPeerReview>(entity =>
        {
            entity.ToTable("AssessmentPeerReviews");
            entity.HasKey(e => e.Id);
            // Unique (ReviewerUserId, SubmissionId): race protection for peer-review claim (todo 7).
            entity.HasIndex(e => new { e.ReviewerUserId, e.SubmissionId }).IsUnique();
            entity.HasIndex(e => e.SubmissionId);
            entity.HasIndex(e => e.AssessmentId);
            entity.HasIndex(e => e.ReviewerUserId);
            entity.Property(e => e.Feedback).HasColumnType("text");
            entity.Property(e => e.RubricScoresPayload).HasColumnType("jsonb");
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
