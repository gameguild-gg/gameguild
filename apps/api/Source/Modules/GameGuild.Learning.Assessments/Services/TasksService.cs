using System.Text.RegularExpressions;
using GameGuild.Identity.Authorization;
using GameGuild.Learning.Courses;
using GameGuild.Learning.Enrollments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Assessments;

/// <summary>
/// Cross-course task aggregation: grade items for managed courses, do/review items for active enrollments.
/// All counts are computed live per request from submission/review rows.
/// </summary>
public class TasksService(
    IApplicationDbContext context,
    IPermissionQueryService permissionQueryService,
    ILogger<TasksService> logger) : ITasksService
{
    // Managed-course permission names mirror AssessmentsController.CanManageCourseAsync exactly:
    // Program.{courseId}.{Edit|Create|Delete} in the actor's tenant, plus program creators and system admins.
    private static readonly Regex ProgramPermissionPattern = new(
        @"^Program\.(?<courseId>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\.(Edit|Create|Delete)$",
        RegexOptions.Compiled);

    public async Task<TasksDto> GetTasksAsync(Guid actorUserId, Guid? tenantId, bool isSystemAdmin)
    {
        var managedCourses = await GetManagedCoursesAsync(actorUserId, tenantId, isSystemAdmin).ConfigureAwait(false);

        var enrollments = await context.Set<Enrollment>()
            .Where(e => e.UserId == actorUserId &&
                        e.Status == GameGuild.Learning.Enrollments.EnrollmentStatus.Active &&
                        e.DeletedAt == null)
            .ToListAsync().ConfigureAwait(false);
        var enrolledCourseIds = enrollments.Select(e => e.CourseId).Distinct().ToList();

        var courseIds = managedCourses.Keys.Concat(enrolledCourseIds).Distinct().ToList();
        var assessments = courseIds.Count == 0
            ? []
            : await context.Set<Assessment>()
                .Where(a => courseIds.Contains(a.CourseId) && a.DeletedAt == null)
                .ToListAsync().ConfigureAwait(false);

        var assessmentIds = assessments.Select(a => a.Id).ToList();
        var submissions = assessmentIds.Count == 0
            ? []
            : await context.Set<AssessmentSubmission>()
                .Where(s => assessmentIds.Contains(s.AssessmentId) && s.DeletedAt == null)
                .ToListAsync().ConfigureAwait(false);

        var actorReviews = assessmentIds.Count == 0
            ? []
            : await context.Set<AssessmentPeerReview>()
                .Where(r => r.ReviewerUserId == actorUserId &&
                            r.DeletedAt == null &&
                            assessmentIds.Contains(r.AssessmentId))
                .ToListAsync().ConfigureAwait(false);

        var items = new List<TaskItemDto>();

        foreach (var assessment in assessments.Where(a => managedCourses.ContainsKey(a.CourseId)))
        {
            var pending = CountPendingGradeTargets(submissions.Where(s => s.AssessmentId == assessment.Id));
            if (pending > 0)
            {
                items.Add(new TaskItemDto(
                    "grade",
                    assessment.CourseId,
                    managedCourses[assessment.CourseId],
                    assessment.Id,
                    assessment.Title,
                    assessment.DueAt,
                    CountSubmitted: pending));
            }
        }

        var seenAssessmentIds = new HashSet<Guid>();
        foreach (var enrollment in enrollments)
        {
            foreach (var assessment in assessments.Where(a => a.CourseId == enrollment.CourseId))
            {
                if (!seenAssessmentIds.Add(assessment.Id))
                {
                    continue;
                }

                var own = submissions.Where(s => s.AssessmentId == assessment.Id && s.EnrollmentId == enrollment.Id).ToList();
                var latest = own.OrderByDescending(r => r.AttemptNumber).FirstOrDefault();
                var latestOpen = latest is null || latest.Status == SubmissionStatus.InProgress;
                var attemptsRemain = assessment.MaxAttempts is null || own.Count < assessment.MaxAttempts.Value;
                if (assessment.IsAvailable() && latestOpen && attemptsRemain)
                {
                    items.Add(new TaskItemDto(
                        "do",
                        assessment.CourseId,
                        managedCourses.GetValueOrDefault(assessment.CourseId) ?? assessment.CourseId.ToString(),
                        assessment.Id,
                        assessment.Title,
                        assessment.DueAt));
                }

                var hasPeerReview = (assessment.GradingMethods & AssessmentGradingMethod.PeerReview) != 0;
                var reviews = actorReviews.Where(r => r.AssessmentId == assessment.Id).ToList();
                if (hasPeerReview &&
                    assessment.PeerReviewsRequiredCount > 0 &&
                    reviews.Count < assessment.PeerReviewsRequiredCount)
                {
                    items.Add(new TaskItemDto(
                        "review",
                        assessment.CourseId,
                        managedCourses.GetValueOrDefault(assessment.CourseId) ?? assessment.CourseId.ToString(),
                        assessment.Id,
                        assessment.Title,
                        // Reviews run to the assessment close — same asymmetry as the todo-8 read endpoints.
                        assessment.DueAt ?? assessment.AvailableUntil ?? assessment.LateSubmissionDeadline,
                        ReviewsCompleted: reviews.Count(r => r.Status == PeerReviewStatus.Submitted),
                        ReviewsRequired: assessment.PeerReviewsRequiredCount));
                }
            }
        }

        logger.LogDebug("Aggregated {Count} tasks for actor {ActorUserId}", items.Count, actorUserId);
        return new TasksDto(items);
    }

    /// <summary>
    /// Distinct targets awaiting a grade: one per group-attempt, one per (user, attempt) for individual
    /// submissions — the same collapse the grading queue applies. InProgress rows never count.
    /// Shared with the submit-notification hook (AssessmentService).
    /// </summary>
    internal static int CountPendingGradeTargets(IEnumerable<AssessmentSubmission> rows) =>
        rows.Where(r => r.Status is SubmissionStatus.Submitted or SubmissionStatus.Late)
            .GroupBy(r => r.CourseGroupId.HasValue
                ? ("group", r.CourseGroupId.Value, r.AttemptNumber)
                : ("user", r.UserId, r.AttemptNumber))
            .Count();

    private async Task<Dictionary<Guid, string>> GetManagedCoursesAsync(Guid actorUserId, Guid? tenantId, bool isSystemAdmin)
    {
        var programs = await context.Set<Program>()
            .Where(p => p.DeletedAt == null)
            .ToListAsync().ConfigureAwait(false);

        // Tenant compatibility mirrors CanManageCourseAsync: system admins manage everything;
        // everyone else needs a tenant, and a program's tenant must be null or match the actor's.
        // Compatibility is a GUARD, not a grant — only creator or explicit permission manages.
        var managed = new Dictionary<Guid, string>();
        if (isSystemAdmin)
        {
            foreach (var program in programs)
            {
                managed.TryAdd(program.Id, program.Title);
            }
        }
        else if (tenantId.HasValue)
        {
            foreach (var program in programs.Where(p => p.CreatorId == actorUserId))
            {
                managed.TryAdd(program.Id, program.Title);
            }
        }

        if (!isSystemAdmin && tenantId.HasValue)
        {
            var permissions = await permissionQueryService
                .GetEffectivePermissionsAsync(actorUserId, tenantId)
                .ConfigureAwait(false);
            var permissionCourseIds = permissions
                .Select(p => ProgramPermissionPattern.Match(p))
                .Where(m => m.Success && Guid.TryParse(m.Groups["courseId"].Value, out _))
                .Select(m => Guid.Parse(m.Groups["courseId"].Value))
                .ToHashSet();
            foreach (var program in programs.Where(p => permissionCourseIds.Contains(p.Id)))
            {
                managed.TryAdd(program.Id, program.Title);
            }
        }

        return managed;
    }
}
