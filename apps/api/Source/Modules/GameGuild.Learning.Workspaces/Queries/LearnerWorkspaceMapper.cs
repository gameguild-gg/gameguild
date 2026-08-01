using GameGuild.Learning.Assessments;
using GameGuild.Learning.Certificates;
using GameGuild.Learning.Cohorts;
using GameGuild.Learning.Courses;
using Program = GameGuild.Learning.Courses.Program;

namespace GameGuild.Learning.Workspaces;

internal static class LearnerWorkspaceMapper
{
    public static LearnerCourseSummaryDto MapCourse(
        Program course,
        ProgramEnrollment enrollment,
        IReadOnlyList<ProgramContent> content,
        IReadOnlyList<ContentProgress> progress)
    {
        var learningItems = content
            .Where(item => item.Type != ProgramContentType.Module)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.CreatedAt)
            .ToArray();
        var progressByContent = progress
            .GroupBy(item => item.ContentId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.UpdatedAt).First());
        var completedItems = learningItems.Count(item =>
            progressByContent.TryGetValue(item.Id, out var itemProgress) &&
            itemProgress.CompletionStatus == ContentCompletionStatus.Completed);
        var current = learningItems
            .Where(item =>
                progressByContent.TryGetValue(item.Id, out var itemProgress) &&
                itemProgress.CompletionStatus == ContentCompletionStatus.InProgress)
            .OrderByDescending(item => progressByContent[item.Id].LastAccessedAt)
            .FirstOrDefault();
        current ??= learningItems.FirstOrDefault(item =>
            !progressByContent.TryGetValue(item.Id, out var itemProgress) ||
            itemProgress.CompletionStatus != ContentCompletionStatus.Completed);

        var remainingMinutes = learningItems
            .Where(item =>
                !progressByContent.TryGetValue(item.Id, out var itemProgress) ||
                itemProgress.CompletionStatus != ContentCompletionStatus.Completed)
            .Sum(item => item.EstimatedMinutes ?? 0);

        return new LearnerCourseSummaryDto(
            course.Id,
            enrollment.Id,
            course.Title,
            course.Slug ?? course.Id.ToString(),
            course.Description ?? string.Empty,
            course.Thumbnail,
            course.Category.ToString(),
            course.Difficulty.ToString(),
            course.EstimatedHours,
            enrollment.EnrollmentStatus.ToString(),
            enrollment.CompletionStatus.ToString(),
            enrollment.ProgressPercentage,
            enrollment.FinalGrade,
            enrollment.EnrolledAt,
            learningItems.Length,
            completedItems,
            remainingMinutes,
            current?.Id,
            current?.Title,
            current?.Type.ToString());
    }

    public static LearnerScheduleEntryDto MapSchedule(
        CohortScheduleItem item,
        Cohort cohort,
        Program course)
    {
        return new LearnerScheduleEntryDto(
            course.Id,
            course.Title,
            course.Slug ?? course.Id.ToString(),
            cohort.Id,
            cohort.Name,
            item.Id,
            item.ProgramContentId,
            item.AssessmentId,
            item.Type.ToString(),
            item.Title ?? string.Empty,
            item.StartsAt,
            item.EndsAt,
            item.AvailableFrom,
            item.AvailableUntil,
            item.DueAt,
            item.Location,
            item.MeetingUrl,
            item.Status.ToString());
    }

    public static LearnerCertificateDto MapCertificate(Certificate certificate)
    {
        return new LearnerCertificateDto(
            certificate.Id,
            certificate.EnrollmentId,
            certificate.CourseId,
            certificate.CourseName,
            certificate.CertificateNumber,
            certificate.RecipientName,
            certificate.IssuedAt,
            certificate.ExpiresAt,
            certificate.VerificationUrl,
            certificate.Status.ToString());
    }

    public static LearnerAssessmentSubmissionDto MapSubmission(AssessmentSubmission submission)
    {
        return new LearnerAssessmentSubmissionDto(
            submission.Id,
            submission.AssessmentId,
            submission.EnrollmentId,
            submission.AttemptNumber,
            submission.Score,
            submission.Passed,
            submission.StartedAt,
            submission.SubmittedAt,
            submission.GradedAt,
            submission.Feedback,
            submission.Status.ToString(),
            submission.IsLate);
    }
}
