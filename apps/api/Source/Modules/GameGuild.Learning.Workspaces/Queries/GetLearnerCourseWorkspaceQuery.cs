using GameGuild.CQRS;
using GameGuild.Learning.Assessments;
using GameGuild.Learning.Certificates;
using GameGuild.Learning.Cohorts;
using GameGuild.Learning.Courses;
using GameGuild.Learning.Experience.Social;
using Microsoft.EntityFrameworkCore;
using LearningEnrollment = GameGuild.Learning.Enrollments.Enrollment;
using Program = GameGuild.Learning.Courses.Program;

namespace GameGuild.Learning.Workspaces;

public sealed record GetLearnerCourseWorkspaceQuery(Guid UserId, Guid CourseId)
    : IQuery<LearnerCourseWorkspaceDto?>;

public sealed class GetLearnerCourseWorkspaceQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetLearnerCourseWorkspaceQuery, LearnerCourseWorkspaceDto?>
{
    public async Task<LearnerCourseWorkspaceDto?> Handle(
        GetLearnerCourseWorkspaceQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty || request.CourseId == Guid.Empty)
        {
            return null;
        }

        var enrollment = await context.Set<ProgramEnrollment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                    item.UserId == request.UserId &&
                    item.ProgramId == request.CourseId &&
                    item.DeletedAt == null &&
                    item.EnrollmentStatus != GameGuild.Learning.Courses.EnrollmentStatus.Cancelled &&
                    item.EnrollmentStatus != GameGuild.Learning.Courses.EnrollmentStatus.Expired,
                cancellationToken)
            .ConfigureAwait(false);
        if (enrollment is null)
        {
            return null;
        }

        var course = await context.Set<Program>()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.CourseId && item.DeletedAt == null, cancellationToken)
            .ConfigureAwait(false);
        if (course is null)
        {
            return null;
        }

        var content = await context.Set<ProgramContent>()
            .AsNoTracking()
            .Where(item => item.ProgramId == request.CourseId && item.DeletedAt == null)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.CreatedAt)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        var progress = await context.Set<ContentProgress>()
            .AsNoTracking()
            .Where(item =>
                item.UserId == request.UserId &&
                item.ProgramEnrollmentId == enrollment.Id &&
                item.DeletedAt == null)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        var learningEnrollment = await context.Set<LearningEnrollment>()
            .AsNoTracking()
            .Where(item =>
                item.UserId == request.UserId &&
                item.CourseId == request.CourseId &&
                item.DeletedAt == null)
            .OrderByDescending(item => item.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        var cohort = learningEnrollment?.CohortId is Guid cohortId
            ? await context.Set<Cohort>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == cohortId && item.DeletedAt == null, cancellationToken)
                .ConfigureAwait(false)
            : null;
        var schedule = cohort is null
            ? Array.Empty<CohortScheduleItem>()
            : await context.Set<CohortScheduleItem>()
                .AsNoTracking()
                .Where(item => item.CohortId == cohort.Id && item.DeletedAt == null)
                .OrderBy(item => item.StartsAt ?? item.AvailableFrom ?? item.DueAt)
                .ThenBy(item => item.SortOrder)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);
        var groups = await context.Set<AssessmentGroup>()
            .AsNoTracking()
            .Where(item => item.CourseId == request.CourseId && item.DeletedAt == null)
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Name)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        var assessments = await context.Set<Assessment>()
            .AsNoTracking()
            .Where(item => item.CourseId == request.CourseId && item.DeletedAt == null)
            .OrderBy(item => item.Order)
            .ThenBy(item => item.DueAt)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        var assessmentIds = assessments.Select(item => item.Id).ToArray();
        var submissions = assessmentIds.Length == 0
            ? Array.Empty<AssessmentSubmission>()
            : await context.Set<AssessmentSubmission>()
                .AsNoTracking()
                .Where(item =>
                    item.UserId == request.UserId &&
                    assessmentIds.Contains(item.AssessmentId) &&
                    item.DeletedAt == null)
                .OrderBy(item => item.AssessmentId)
                .ThenByDescending(item => item.AttemptNumber)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);
        var discussions = await context.Set<CourseDiscussion>()
            .AsNoTracking()
            .Where(item => item.CourseId == request.CourseId && item.DeletedAt == null)
            .OrderByDescending(item => item.IsPinned)
            .ThenByDescending(item => item.LastActivityAt ?? item.CreatedAt)
            .Take(100)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        var certificates = await context.Set<Certificate>()
            .AsNoTracking()
            .Where(item =>
                item.UserId == request.UserId &&
                item.CourseId == request.CourseId &&
                item.DeletedAt == null)
            .OrderByDescending(item => item.IssuedAt)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return new LearnerCourseWorkspaceDto(
            LearnerWorkspaceMapper.MapCourse(course, enrollment, content, progress),
            content.Select(MapContent).ToArray(),
            progress.Select(MapProgress).ToArray(),
            cohort is null ? null : MapCohort(cohort),
            cohort is null
                ? []
                : schedule.Select(item => LearnerWorkspaceMapper.MapSchedule(item, cohort, course)).ToArray(),
            groups.Select(MapGroup).ToArray(),
            assessments.Select(MapAssessment).ToArray(),
            submissions.Select(LearnerWorkspaceMapper.MapSubmission).ToArray(),
            discussions.Select(MapDiscussion).ToArray(),
            certificates.Select(LearnerWorkspaceMapper.MapCertificate).ToArray());
    }

    private static LearnerContentDto MapContent(ProgramContent item)
    {
        return new LearnerContentDto(
            item.Id,
            item.ParentId,
            item.Title,
            item.Description ?? string.Empty,
            item.Type.ToString(),
            item.Body,
            item.LessonFormat?.ToString(),
            item.ActivitySettingsData,
            item.SortOrder,
            item.IsRequired,
            item.EstimatedMinutes,
            item.Visibility.ToString());
    }

    private static LearnerContentProgressDto MapProgress(ContentProgress item)
    {
        return new LearnerContentProgressDto(
            item.ContentId,
            item.CompletionStatus.ToString(),
            item.ProgressPercentage,
            item.FirstAccessedAt,
            item.LastAccessedAt,
            item.CompletedAt,
            item.TimeSpentSeconds,
            item.Score,
            item.MaxScore,
            item.Attempts);
    }

    private static LearnerCohortDto MapCohort(Cohort cohort)
    {
        return new LearnerCohortDto(
            cohort.Id,
            cohort.Name,
            cohort.Description,
            cohort.StartDate,
            cohort.EndDate,
            cohort.MaxCapacity,
            cohort.CurrentEnrollmentCount,
            cohort.Status.ToString(),
            cohort.InstructorId,
            cohort.MeetingSchedule);
    }

    private static LearnerAssessmentGroupDto MapGroup(AssessmentGroup group)
    {
        return new LearnerAssessmentGroupDto(
            group.Id,
            group.Name,
            group.Description,
            group.WeightPercent,
            group.Order);
    }

    private static LearnerAssessmentDto MapAssessment(Assessment assessment)
    {
        return new LearnerAssessmentDto(
            assessment.Id,
            assessment.ContentId,
            assessment.AssessmentGroupId,
            assessment.Title,
            assessment.Description,
            assessment.Type.ToString(),
            assessment.MaxScore,
            assessment.TimeLimitMinutes,
            assessment.MaxAttempts,
            assessment.IsRequired,
            assessment.Order,
            assessment.AvailableFrom,
            assessment.AvailableUntil,
            assessment.DueAt,
            assessment.AllowLateSubmissions,
            assessment.LateSubmissionDeadline,
            assessment.SubmissionModalities.ToString(),
            assessment.PresentationMode.ToString());
    }

    private static LearnerDiscussionDto MapDiscussion(CourseDiscussion discussion)
    {
        return new LearnerDiscussionDto(
            discussion.Id,
            discussion.ContentId,
            discussion.AuthorId,
            discussion.Title,
            discussion.Content,
            discussion.IsPinned,
            discussion.IsResolved,
            discussion.ReplyCount,
            discussion.ViewCount,
            discussion.LastActivityAt,
            discussion.CreatedAt);
    }
}
