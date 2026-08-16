using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Assessments;

/// <summary>
/// Service implementation for the instructor grading queue (SpeedGrader navigation bundle).
/// </summary>
public class GradingQueueService : IGradingQueueService
{
    private readonly IApplicationDbContext _context;
    private readonly IRubricService _rubricService;
    private readonly ILogger<GradingQueueService> _logger;

    public GradingQueueService(
        IApplicationDbContext context,
        IRubricService rubricService,
        ILogger<GradingQueueService> logger)
    {
        _context = context;
        _rubricService = rubricService;
        _logger = logger;
    }

    public async Task<Result<GradingQueueDto>> GetQueueAsync(Guid assessmentId)
    {
        // ponytail: single load, no pagination — add paging when a course exceeds ~500 items.
        var assessment = await _context.Set<Assessment>()
            .FirstOrDefaultAsync(a => a.Id == assessmentId && a.DeletedAt == null)
            .ConfigureAwait(false);
        if (assessment == null)
        {
            return Result.Failure<GradingQueueDto>(Error.NotFound("Assessment", "Assessment not found"));
        }

        RubricDto? rubric = null;
        if (assessment.RubricId != null)
        {
            var rubricResult = await _rubricService.GetAsync(assessmentId).ConfigureAwait(false);
            if (rubricResult.IsSuccess)
            {
                rubric = rubricResult.Value;
            }
        }

        var rows = await _context.Set<AssessmentSubmission>()
            .Where(s => s.AssessmentId == assessmentId && s.DeletedAt == null)
            .ToListAsync().ConfigureAwait(false);

        // One bucket per (student | group) × attempt, keeping only gradeable rows:
        // attempts whose only rows are InProgress are excluded from the queue entirely.
        var attempts = new List<(List<AssessmentSubmission> Gradeable, Guid? GroupId)>();
        foreach (var userRows in rows.Where(r => r.CourseGroupId == null)
                     .GroupBy(r => (r.UserId, r.AttemptNumber)))
        {
            var gradeable = userRows.Where(r => r.Status != SubmissionStatus.InProgress).ToList();
            if (gradeable.Count > 0)
            {
                attempts.Add((gradeable, null));
            }
        }

        var groupIds = rows.Where(r => r.CourseGroupId != null)
            .Select(r => r.CourseGroupId!.Value)
            .Distinct()
            .ToList();
        var groups = groupIds.Count == 0
            ? []
            : await _context.Set<CourseGroup>()
                .Where(g => groupIds.Contains(g.Id) && g.DeletedAt == null)
                .ToDictionaryAsync(g => g.Id)
                .ConfigureAwait(false);

        foreach (var groupRows in rows.Where(r => r.CourseGroupId != null)
                     .GroupBy(r => (GroupId: r.CourseGroupId!.Value, r.AttemptNumber)))
        {
            var gradeable = groupRows.Where(r => r.Status != SubmissionStatus.InProgress).ToList();
            if (gradeable.Count > 0)
            {
                attempts.Add((gradeable, groupRows.Key.GroupId));
            }
        }

        // Group members are resolved at query time (active membership), matching GroupSetService.
        var members = groupIds.Count == 0
            ? []
            : await _context.Set<CourseGroupMember>()
                .Where(m => groupIds.Contains(m.GroupId) && m.DeletedAt == null)
                .ToListAsync().ConfigureAwait(false);

        // Batched display-name lookup for every user the queue can name (GroupSetService pattern).
        var userIds = attempts
            .Where(a => a.GroupId == null)
            .SelectMany(a => a.Gradeable.Select(r => r.UserId))
            .Concat(members.Select(m => m.UserId))
            .Distinct()
            .ToList();
        var namesById = (await _context.Set<User>()
                .Where(u => userIds.Contains(u.Id) && u.DeletedAt == null)
                .ToListAsync().ConfigureAwait(false))
            .ToDictionary(u => u.Id, u => u.Name);
        string DisplayName(Guid userId) =>
            namesById.TryGetValue(userId, out var name) && !string.IsNullOrWhiteSpace(name)
                ? name
                : userId.ToString();

        var items = new List<GradingQueueItemDto>();
        foreach (var (gradeable, groupId) in attempts)
        {
            if (groupId == null)
            {
                var row = gradeable[0];
                items.Add(new GradingQueueItemDto(
                    row.Id,
                    row.Id,
                    row.AttemptNumber,
                    AggregateStatus(gradeable),
                    row.Score,
                    gradeable.Any(r => r.IsLate),
                    gradeable.Max(r => r.SubmittedAt),
                    IsGroup: false,
                    UserId: row.UserId,
                    DisplayName: DisplayName(row.UserId)));
            }
            else
            {
                // Canonical row rule shared with peer-review assignment: Min(Id) among the rows
                // sharing (CourseGroupId, AttemptNumber) — clones share timestamps, so Id is the
                // only deterministic tiebreak.
                var canonical = PeerReviewAssignmentService.CanonicalRow(gradeable);
                var group = groups.GetValueOrDefault(groupId.Value);
                items.Add(new GradingQueueItemDto(
                    canonical.Id,
                    canonical.Id,
                    canonical.AttemptNumber,
                    AggregateStatus(gradeable),
                    canonical.Score,
                    gradeable.Any(r => r.IsLate),
                    gradeable.Max(r => r.SubmittedAt),
                    IsGroup: true,
                    GroupId: groupId,
                    GroupName: group?.Name ?? groupId.Value.ToString(),
                    MemberNames: members
                        .Where(m => m.GroupId == groupId)
                        .Select(m => m.UserId)
                        .Distinct()
                        .Select(DisplayName)
                        .ToList()));
            }
        }

        // DisplayName ASC, then attempt DESC: instructors grade the student's/group's LATEST
        // submission first (one grade per assignment; regrades land on a fresh attempt row),
        // so nav=0 is the most recent gradeable attempt.
        var sorted = items
            .OrderBy(i => i.DisplayName ?? i.GroupName, StringComparer.Ordinal)
            .ThenByDescending(i => i.AttemptNumber)
            .ToList();

        return Result.Success(new GradingQueueDto(
            new GradingQueueAssessmentDto(
                assessment.Id,
                assessment.Title,
                assessment.Type,
                assessment.MaxScore,
                assessment.GradingMethods.ToString(),
                assessment.GroupSetId,
                assessment.PeerReviewsRequiredCount,
                assessment.RubricId != null,
                rubric),
            sorted,
            Total: sorted.Count,
            NeedsGrading: sorted.Count(i =>
                i.Status is SubmissionStatus.Submitted or SubmissionStatus.Late)));
    }

    /// <summary>
    /// Status precedence (deterministic, worst-pending-first): an attempt shows its
    /// least-advanced row status — Submitted &lt; Late &lt; Graded — so a group attempt with
    /// any ungraded row stays pending in the queue.
    /// </summary>
    private static SubmissionStatus AggregateStatus(IReadOnlyList<AssessmentSubmission> gradeable) =>
        gradeable.Any(r => r.Status == SubmissionStatus.Submitted) ? SubmissionStatus.Submitted
        : gradeable.Any(r => r.Status == SubmissionStatus.Late) ? SubmissionStatus.Late
        : SubmissionStatus.Graded;
}
