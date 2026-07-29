using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Assessments;

/// <summary>
/// Service implementation for assessment management and submission processing
/// </summary>
public class AssessmentService : IAssessmentService
{
    private readonly IApplicationDbContext _context;
    private readonly IProgramContentService _programContentService;
    private readonly ILogger<AssessmentService> _logger;

    public AssessmentService(
        IApplicationDbContext context,
        IProgramContentService programContentService,
        ILogger<AssessmentService> logger)
    {
        _context = context;
        _programContentService = programContentService;
        _logger = logger;
    }

    // ===== ASSESSMENT MANAGEMENT =====

    public async Task<Result<Assessment>> CreateAssessmentAsync(CreateAssessmentRequest request)
    {
        try
        {
            var assessment = Assessment.Create(
                request.CourseId,
                request.Title,
                request.Type,
                request.MaxScore,
                request.PassingScore,
                request.IsRequired,
                request.AssessmentGroupId);

            // Set optional properties using internal setters
            assessment.SetDescription(request.Description);
            assessment.SetTimeLimit(request.TimeLimitMinutes);
            assessment.SetMaxAttempts(request.MaxAttempts);
            assessment.SetDeliveryContract(request.SubmissionModalities, request.PresentationMode);
            assessment.SetDeliverySchedule(
                request.AvailableFrom,
                request.AvailableUntil,
                request.DueAt,
                request.AllowLateSubmissions,
                request.LateSubmissionDeadline);

            var groupValidation = await EnsureGroupMatchesCourseAsync(request.AssessmentGroupId, request.CourseId).ConfigureAwait(false);
            if (!groupValidation.IsSuccess)
            {
                return Result.Failure<Assessment>(groupValidation.Error);
            }

            _context.Set<Assessment>().Add(assessment);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Assessment created: {AssessmentId} for course {CourseId}", assessment.Id, request.CourseId);

            return Result.Success(assessment);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<Assessment>(Error.Validation("Assessment.Invalid", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assessment for course {CourseId}", request.CourseId);
            return Result.Failure<Assessment>(Error.Failure("CreateAssessment", "Failed to create assessment"));
        }
    }

    public async Task<Assessment?> GetAssessmentByIdAsync(Guid id)
    {
        return await _context.Set<Assessment>()
            .Include(a => a.AssessmentGroup)
            .FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Assessment>> GetCourseAssessmentsAsync(Guid courseId)
    {
        return await _context.Set<Assessment>()
            .Include(a => a.AssessmentGroup)
            .Where(a => a.CourseId == courseId && a.DeletedAt == null)
            .OrderBy(a => a.AssessmentGroup == null ? int.MaxValue : a.AssessmentGroup.Order)
            .ThenBy(a => a.Order)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<CourseAssessmentAnalyticsDto> GetCourseAssessmentAnalyticsAsync(Guid courseId)
    {
        var assessments = await _context.Set<Assessment>()
            .Include(a => a.AssessmentGroup)
            .Where(a => a.CourseId == courseId && a.DeletedAt == null)
            .ToListAsync().ConfigureAwait(false);

        var assessmentIds = assessments.Select(a => a.Id).ToArray();
        var submissions = assessmentIds.Length == 0
            ? new List<AssessmentSubmission>()
            : await _context.Set<AssessmentSubmission>()
                .Where(s => assessmentIds.Contains(s.AssessmentId) && s.DeletedAt == null)
                .ToListAsync().ConfigureAwait(false);

        var scored = BuildScoreFacts(assessments, submissions);
        var groups = assessments
            .GroupBy(a => new
            {
                GroupId = a.AssessmentGroupId,
                GroupName = a.AssessmentGroup?.Name ?? "Ungrouped",
                a.AssessmentGroup?.WeightPercent,
                GroupOrder = a.AssessmentGroup?.Order ?? int.MaxValue
            })
            .OrderBy(g => g.Key.GroupOrder)
            .ThenBy(g => g.Key.GroupName)
            .Select(g =>
            {
                var groupAssessmentIds = g.Select(a => a.Id).ToHashSet();
                var groupScored = scored.Where(f => groupAssessmentIds.Contains(f.AssessmentId)).ToList();
                return new AssessmentGroupAnalyticsDto(
                    g.Key.GroupId,
                    g.Key.GroupName,
                    g.Key.WeightPercent,
                    g.Count(),
                    groupScored.Count,
                    g.Count(a => !groupScored.Any(f => f.AssessmentId == a.Id)),
                    AveragePercent(groupScored),
                    PassRate(groupScored),
                    BuildDistribution(groupScored));
            })
            .ToArray();

        return new CourseAssessmentAnalyticsDto(
            courseId,
            assessments.Count,
            scored.Count,
            assessments.Count(a => !scored.Any(f => f.AssessmentId == a.Id)),
            AveragePercent(scored),
            PassRate(scored),
            BuildDistribution(scored),
            groups);
    }

    public async Task<Result<Assessment>> UpdateAssessmentAsync(Guid id, UpdateAssessmentRequest request)
    {
        try
        {
            var assessment = await GetAssessmentByIdAsync(id).ConfigureAwait(false);
            if (assessment == null)
            {
                return Result.Failure<Assessment>(Error.NotFound("Assessment", "Assessment not found"));
            }

            if (request.MaxScore.HasValue &&
                await _context.Set<AssessmentSubmission>()
                    .AnyAsync(submission => submission.AssessmentId == id &&
                                              submission.Score.HasValue &&
                                              submission.Score.Value > request.MaxScore.Value)
                    .ConfigureAwait(false))
            {
                return Result.Failure<Assessment>(Error.Validation(
                    "Assessment.ScoreRange",
                    "Maximum score cannot be lower than an assigned submission score."));
            }

            assessment.Update(
                request.Title,
                request.Description,
                request.MaxScore,
                request.PassingScore,
                request.TimeLimitMinutes,
                request.MaxAttempts,
                request.IsRequired,
                request.AvailableFrom,
                request.AvailableUntil,
                request.ContentId,
                request.ClearContentId,
                request.AssessmentGroupId,
                request.ClearAssessmentGroupId,
                request.SubmissionModalities,
                request.PresentationMode,
                request.DueAt,
                request.ClearDueAt,
                request.AllowLateSubmissions,
                request.LateSubmissionDeadline,
                request.ClearLateSubmissionDeadline);

            var groupValidation = await EnsureGroupMatchesCourseAsync(assessment.AssessmentGroupId, assessment.CourseId).ConfigureAwait(false);
            if (!groupValidation.IsSuccess)
            {
                return Result.Failure<Assessment>(groupValidation.Error);
            }

            _context.Set<Assessment>().Update(assessment);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Assessment updated: {AssessmentId}", id);

            return Result.Success(assessment);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<Assessment>(Error.Validation("Assessment.Invalid", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assessment {AssessmentId}", id);
            return Result.Failure<Assessment>(Error.Failure("UpdateAssessment", "Failed to update assessment"));
        }
    }

    public async Task<Result> DeleteAssessmentAsync(Guid id)
    {
        try
        {
            await using var lifecycleTransaction = await AssessmentLifecycleDatabaseLock
                .AcquireAsync(_context, id)
                .ConfigureAwait(false);
            var assessment = await GetAssessmentByIdAsync(id).ConfigureAwait(false);
            if (assessment == null)
            {
                return Result.Failure(Error.NotFound("Assessment", "Assessment not found"));
            }

            assessment.SoftDelete();
            var activeCues = await _context.Set<InteractiveVideoAssessmentCue>()
                .Where(cue => cue.AssessmentId == id && cue.DeletedAt == null)
                .ToListAsync()
                .ConfigureAwait(false);
            await using var contentLifecycleTransaction = await ProgramContentLifecycleDatabaseLock
                .AcquireAsync(_context, activeCues.Select(cue => cue.ContentId))
                .ConfigureAwait(false);
            foreach (var cue in activeCues)
            {
                cue.SoftDelete();
            }

            _context.Set<Assessment>().Update(assessment);
            _context.Set<InteractiveVideoAssessmentCue>().UpdateRange(activeCues);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            await AssessmentLifecycleDatabaseLock.CommitAsync(lifecycleTransaction).ConfigureAwait(false);

            _logger.LogInformation("Assessment deleted: {AssessmentId}", id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assessment {AssessmentId}", id);
            return Result.Failure(Error.Failure("DeleteAssessment", "Failed to delete assessment"));
        }
    }

    public async Task<IEnumerable<AssessmentGroup>> GetCourseAssessmentGroupsAsync(Guid courseId)
    {
        return await _context.Set<AssessmentGroup>()
            .Where(g => g.CourseId == courseId && g.DeletedAt == null)
            .OrderBy(g => g.Order)
            .ThenBy(g => g.Name)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<AssessmentGroup?> GetAssessmentGroupByIdAsync(Guid id)
    {
        return await _context.Set<AssessmentGroup>()
            .FirstOrDefaultAsync(group => group.Id == id && group.DeletedAt == null)
            .ConfigureAwait(false);
    }

    public async Task<Result<AssessmentGroup>> CreateAssessmentGroupAsync(CreateAssessmentGroupRequest request)
    {
        try
        {
            var group = AssessmentGroup.Create(
                request.CourseId,
                request.Name,
                request.WeightPercent,
                request.Order,
                request.Description);

            _context.Set<AssessmentGroup>().Add(group);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Assessment group created: {AssessmentGroupId} for course {CourseId}", group.Id, request.CourseId);

            return Result.Success(group);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<AssessmentGroup>(Error.Validation("AssessmentGroup.Invalid", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assessment group for course {CourseId}", request.CourseId);
            return Result.Failure<AssessmentGroup>(Error.Failure("CreateAssessmentGroup", "Failed to create assessment group"));
        }
    }

    public async Task<Result<AssessmentGroup>> UpdateAssessmentGroupAsync(Guid id, UpdateAssessmentGroupRequest request)
    {
        try
        {
            var group = await _context.Set<AssessmentGroup>().FirstOrDefaultAsync(g => g.Id == id && g.DeletedAt == null).ConfigureAwait(false);
            if (group == null)
            {
                return Result.Failure<AssessmentGroup>(Error.NotFound("AssessmentGroup", "Assessment group not found"));
            }

            group.Update(request.Name, request.Description, request.WeightPercent, request.Order);
            _context.Set<AssessmentGroup>().Update(group);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Assessment group updated: {AssessmentGroupId}", id);

            return Result.Success(group);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<AssessmentGroup>(Error.Validation("AssessmentGroup.Invalid", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assessment group {AssessmentGroupId}", id);
            return Result.Failure<AssessmentGroup>(Error.Failure("UpdateAssessmentGroup", "Failed to update assessment group"));
        }
    }

    public async Task<Result> DeleteAssessmentGroupAsync(Guid id)
    {
        try
        {
            var group = await _context.Set<AssessmentGroup>().FirstOrDefaultAsync(g => g.Id == id && g.DeletedAt == null).ConfigureAwait(false);
            if (group == null)
            {
                return Result.Failure(Error.NotFound("AssessmentGroup", "Assessment group not found"));
            }

            var groupedAssessments = await _context.Set<Assessment>()
                .Where(a => a.AssessmentGroupId == id)
                .ToListAsync().ConfigureAwait(false);

            foreach (var assessment in groupedAssessments)
            {
                assessment.AssignToGroup(null);
            }

            group.SoftDelete();
            _context.Set<Assessment>().UpdateRange(groupedAssessments);
            _context.Set<AssessmentGroup>().Update(group);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Assessment group deleted: {AssessmentGroupId}", id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assessment group {AssessmentGroupId}", id);
            return Result.Failure(Error.Failure("DeleteAssessmentGroup", "Failed to delete assessment group"));
        }
    }

    public async Task<Result<Assessment>> AssignAssessmentToGroupAsync(Guid assessmentId, AssignAssessmentGroupRequest request)
    {
        try
        {
            var assessment = await GetAssessmentByIdAsync(assessmentId).ConfigureAwait(false);
            if (assessment == null)
            {
                return Result.Failure<Assessment>(Error.NotFound("Assessment", "Assessment not found"));
            }

            if (!request.ClearAssessmentGroup && !request.AssessmentGroupId.HasValue)
            {
                return Result.Failure<Assessment>(Error.Validation("AssessmentGroup.Required", "Assessment group is required unless the assignment is being cleared."));
            }

            var nextGroupId = request.ClearAssessmentGroup ? null : request.AssessmentGroupId;
            var groupValidation = await EnsureGroupMatchesCourseAsync(nextGroupId, assessment.CourseId).ConfigureAwait(false);
            if (!groupValidation.IsSuccess)
            {
                return Result.Failure<Assessment>(groupValidation.Error);
            }

            assessment.AssignToGroup(nextGroupId);
            _context.Set<Assessment>().Update(assessment);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Assessment {AssessmentId} assigned to group {AssessmentGroupId}", assessment.Id, nextGroupId);

            return Result.Success(assessment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning assessment {AssessmentId} to group", assessmentId);
            return Result.Failure<Assessment>(Error.Failure("AssignAssessmentGroup", "Failed to assign assessment group"));
        }
    }

    public async Task<Result<InteractiveVideoAssessmentCue>> LinkInteractiveVideoCueAsync(
        Guid assessmentId,
        LinkInteractiveVideoCueRequest request)
    {
        try
        {
            // Assessment locks are acquired before content locks in both cue-link and assessment-delete flows.
            await using var assessmentLifecycleTransaction = await AssessmentLifecycleDatabaseLock
                .AcquireAsync(_context, assessmentId)
                .ConfigureAwait(false);
            var assessment = await GetAssessmentByIdAsync(assessmentId).ConfigureAwait(false);
            if (assessment == null)
            {
                return Result.Failure<InteractiveVideoAssessmentCue>(Error.NotFound("Assessment", "Assessment not found"));
            }

            await using var lifecycleTransaction = await ProgramContentLifecycleDatabaseLock
                .AcquireAsync(_context, [request.ContentId])
                .ConfigureAwait(false);
            var content = await _programContentService.GetContentByIdAsync(request.ContentId).ConfigureAwait(false);
            if (content == null)
            {
                return Result.Failure<InteractiveVideoAssessmentCue>(Error.NotFound("ProgramContent", "Interactive-video lesson content not found"));
            }

            if (content.ProgramId != assessment.CourseId)
            {
                return Result.Failure<InteractiveVideoAssessmentCue>(
                    Error.Validation("AssessmentCue.CourseMismatch", "Interactive-video content must belong to the assessment course."));
            }

            if (content.Type != ProgramContentType.Lesson || content.LessonFormat != LessonContentFormat.Video)
            {
                return Result.Failure<InteractiveVideoAssessmentCue>(
                    Error.Validation("AssessmentCue.NotVideoLesson", "Interactive-video content must be a video lesson."));
            }

            var cueId = request.CueId ?? string.Empty;
            var normalizedCueId = cueId.Trim();
            var duplicate = await _context.Set<InteractiveVideoAssessmentCue>()
                .AnyAsync(cue =>
                    cue.AssessmentId == assessmentId &&
                    cue.ContentId == request.ContentId &&
                    cue.CueId == normalizedCueId &&
                    cue.DeletedAt == null)
                .ConfigureAwait(false);
            if (duplicate)
            {
                return Result.Failure<InteractiveVideoAssessmentCue>(
                    Error.Validation("AssessmentCue.Duplicate", "The interactive-video cue is already linked to this assessment."));
            }

            var cue = assessment.AddInteractiveVideoCue(request.ContentId, cueId, request.CuePositionSeconds);
            _context.Set<InteractiveVideoAssessmentCue>().Add(cue);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            await AssessmentLifecycleDatabaseLock.CommitAsync(assessmentLifecycleTransaction).ConfigureAwait(false);

            return Result.Success(cue);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<InteractiveVideoAssessmentCue>(Error.Validation("AssessmentCue.Invalid", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking interactive-video cue to assessment {AssessmentId}", assessmentId);
            return Result.Failure<InteractiveVideoAssessmentCue>(Error.Failure("LinkAssessmentCue", "Failed to link interactive-video assessment cue"));
        }
    }

    public async Task<IEnumerable<InteractiveVideoAssessmentCue>> GetInteractiveVideoCuesAsync(Guid assessmentId)
    {
        var assessment = await GetAssessmentByIdAsync(assessmentId).ConfigureAwait(false);
        if (assessment == null) return Array.Empty<InteractiveVideoAssessmentCue>();

        var cues = await _context.Set<InteractiveVideoAssessmentCue>()
            .Where(cue => cue.AssessmentId == assessmentId && cue.DeletedAt == null)
            .OrderBy(cue => cue.CuePositionSeconds)
            .ThenBy(cue => cue.CueId)
            .ToListAsync().ConfigureAwait(false);

        var activeCues = new List<InteractiveVideoAssessmentCue>();
        foreach (var cue in cues)
        {
            var content = await _programContentService.GetContentByIdAsync(cue.ContentId).ConfigureAwait(false);
            if (content?.ProgramId == assessment.CourseId &&
                content.Type == ProgramContentType.Lesson &&
                content.LessonFormat == LessonContentFormat.Video)
            {
                activeCues.Add(cue);
            }
        }

        return activeCues;
    }

    public async Task<IEnumerable<InteractiveVideoAssessmentCue>> GetInteractiveVideoCuesForContentAsync(
        Guid assessmentId,
        Guid contentId)
    {
        var cues = await GetInteractiveVideoCuesAsync(assessmentId).ConfigureAwait(false);
        return cues.Where(cue => cue.ContentId == contentId).ToList();
    }

    public async Task<Result> UnlinkInteractiveVideoCueAsync(Guid assessmentId, Guid cueId)
    {
        var assessment = await GetAssessmentByIdAsync(assessmentId).ConfigureAwait(false);
        if (assessment == null)
        {
            return Result.Failure(Error.NotFound("Assessment", "Assessment not found"));
        }

        var cue = await _context.Set<InteractiveVideoAssessmentCue>()
            .FirstOrDefaultAsync(candidate => candidate.Id == cueId &&
                                              candidate.AssessmentId == assessmentId &&
                                              candidate.DeletedAt == null)
            .ConfigureAwait(false);
        if (cue == null)
        {
            return Result.Failure(Error.NotFound("AssessmentCue", "Interactive-video assessment cue not found"));
        }

        _context.Set<InteractiveVideoAssessmentCue>().Remove(cue);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return Result.Success();
    }

    private async Task<Result> EnsureGroupMatchesCourseAsync(Guid? groupId, Guid courseId)
    {
        if (!groupId.HasValue)
        {
            return Result.Success();
        }

        var group = await _context.Set<AssessmentGroup>()
            .FirstOrDefaultAsync(g => g.Id == groupId.Value && g.DeletedAt == null)
            .ConfigureAwait(false);

        if (group == null)
        {
            return Result.Failure(Error.NotFound("AssessmentGroup", "Assessment group not found"));
        }

        return group.CourseId == courseId
            ? Result.Success()
            : Result.Failure(Error.Validation("AssessmentGroup.CourseMismatch", "Assessment group belongs to another course"));
    }

    private static List<AssessmentScoreFact> BuildScoreFacts(
        IReadOnlyCollection<Assessment> assessments,
        IReadOnlyCollection<AssessmentSubmission> submissions)
    {
        var assessmentsById = assessments.ToDictionary(a => a.Id);
        return submissions
            .Where(s => s.Score.HasValue && assessmentsById.ContainsKey(s.AssessmentId))
            .Select(s =>
            {
                var assessment = assessmentsById[s.AssessmentId];
                var percent = assessment.MaxScore <= 0
                    ? 0
                    : Math.Clamp((decimal)s.Score!.Value / assessment.MaxScore * 100m, 0m, 100m);

                return new AssessmentScoreFact(
                    assessment.Id,
                    percent,
                    s.Passed ?? percent >= PassingPercent(assessment));
            })
            .ToList();
    }

    private static decimal PassingPercent(Assessment assessment)
    {
        return assessment.MaxScore <= 0
            ? 0
            : Math.Clamp((decimal)assessment.PassingScore / assessment.MaxScore * 100m, 0m, 100m);
    }

    private static decimal AveragePercent(IReadOnlyCollection<AssessmentScoreFact> facts)
    {
        return facts.Count == 0 ? 0 : Math.Round(facts.Average(f => f.Percent), 2);
    }

    private static decimal PassRate(IReadOnlyCollection<AssessmentScoreFact> facts)
    {
        return facts.Count == 0 ? 0 : Math.Round((decimal)facts.Count(f => f.Passed) / facts.Count * 100m, 2);
    }

    private static IReadOnlyCollection<AssessmentScoreBucketDto> BuildDistribution(IReadOnlyCollection<AssessmentScoreFact> facts)
    {
        return new[]
        {
            BuildBucket("0-59", 0, 59, facts),
            BuildBucket("60-69", 60, 69, facts),
            BuildBucket("70-79", 70, 79, facts),
            BuildBucket("80-89", 80, 89, facts),
            BuildBucket("90-100", 90, 100, facts)
        };
    }

    private static AssessmentScoreBucketDto BuildBucket(
        string label,
        int minPercent,
        int maxPercent,
        IReadOnlyCollection<AssessmentScoreFact> facts)
    {
        var count = facts.Count(f => f.Percent >= minPercent && f.Percent <= maxPercent);
        return new AssessmentScoreBucketDto(label, minPercent, maxPercent, count);
    }

    private sealed record AssessmentScoreFact(Guid AssessmentId, decimal Percent, bool Passed);

    // ===== SUBMISSION MANAGEMENT =====

    public async Task<Result<AssessmentSubmission>> StartSubmissionAsync(Guid assessmentId, Guid enrollmentId, Guid userId)
    {
        try
        {
            if (_context is DbContext dbContext &&
                dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL" &&
                dbContext.Database.CurrentTransaction is null)
            {
                var executionStrategy = dbContext.Database.CreateExecutionStrategy();
                return await executionStrategy.ExecuteAsync(
                        () => StartSubmissionCoreAsync(assessmentId, enrollmentId, userId))
                    .ConfigureAwait(false);
            }

            return await StartSubmissionCoreAsync(assessmentId, enrollmentId, userId).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrent submission start detected for assessment {AssessmentId}", assessmentId);
            return Result.Failure<AssessmentSubmission>(Error.Conflict("AssessmentSubmission.AttemptConflict", "A concurrent submission attempt was detected. Please retry."));
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            _logger.LogWarning(ex, "Duplicate submission attempt detected for assessment {AssessmentId}", assessmentId);
            return Result.Failure<AssessmentSubmission>(Error.Conflict("AssessmentSubmission.AttemptConflict", "A concurrent submission attempt was detected. Please retry."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting submission for assessment {AssessmentId}", assessmentId);
            return Result.Failure<AssessmentSubmission>(Error.Failure("StartSubmission", "Failed to start submission"));
        }
    }

    private async Task<Result<AssessmentSubmission>> StartSubmissionCoreAsync(
        Guid assessmentId,
        Guid enrollmentId,
        Guid userId)
    {
        await using var attemptTransaction = await AssessmentSubmissionDatabaseLock
            .AcquireAsync(_context, assessmentId, enrollmentId)
            .ConfigureAwait(false);
        var assessment = await GetAssessmentByIdAsync(assessmentId).ConfigureAwait(false);
        if (assessment == null)
        {
            return Result.Failure<AssessmentSubmission>(Error.NotFound("Assessment", "Assessment not found"));
        }

        if (!assessment.IsAvailable())
        {
            return Result.Failure<AssessmentSubmission>(Error.Validation("Assessment", "Assessment is not currently available"));
        }

        var attemptCount = await GetAttemptCountAsync(assessmentId, enrollmentId).ConfigureAwait(false);
        if (assessment.MaxAttempts.HasValue && attemptCount >= assessment.MaxAttempts.Value)
        {
            return Result.Failure<AssessmentSubmission>(Error.Validation("Assessment.MaxAttemptsReached", "Maximum attempts reached"));
        }

        var highestAttemptNumber = await GetHighestAttemptNumberAsync(assessmentId, enrollmentId).ConfigureAwait(false);
        var submission = AssessmentSubmission.Start(assessmentId, enrollmentId, userId, highestAttemptNumber + 1);

        _context.Set<AssessmentSubmission>().Add(submission);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        await AssessmentSubmissionDatabaseLock.CommitAsync(attemptTransaction).ConfigureAwait(false);

        _logger.LogInformation("Submission started: {SubmissionId} for assessment {AssessmentId}", submission.Id, assessmentId);
        return Result.Success(submission);
    }
    public async Task<Result<AssessmentSubmission>> SubmitAsync(Guid submissionId, SubmitAssessmentRequest? request = null)
    {
        try
        {
            var submission = await GetSubmissionByIdAsync(submissionId).ConfigureAwait(false);
            if (submission == null)
            {
                return Result.Failure<AssessmentSubmission>(Error.NotFound("Submission", "Submission not found"));
            }

            if (submission.Status != SubmissionStatus.InProgress)
            {
                return Result.Failure<AssessmentSubmission>(Error.Validation("Submission", "Submission is not in progress"));
            }

            var assessment = await GetAssessmentByIdAsync(submission.AssessmentId).ConfigureAwait(false);
            if (assessment == null)
            {
                return Result.Failure<AssessmentSubmission>(Error.NotFound("Assessment", "Assessment not found"));
            }

            var submittedAt = SystemClock.UtcNow;
            if (!assessment.TryGetSubmissionTiming(submittedAt, out var isLate))
            {
                return Result.Failure<AssessmentSubmission>(Error.Validation("Submission.Unavailable", "Assessment is not accepting submissions at this time"));
            }

            if (request != null)
            {
                submission.SetPayload(request, assessment.SubmissionModalities);
            }

            submission.Submit(isLate, submittedAt);
            _context.Set<AssessmentSubmission>().Update(submission);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Submission submitted: {SubmissionId}", submissionId);

            return Result.Success(submission);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<AssessmentSubmission>(Error.Validation("Submission.Invalid", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting {SubmissionId}", submissionId);
            return Result.Failure<AssessmentSubmission>(Error.Failure("Submit", "Failed to submit"));
        }
    }

    public async Task<Result<AssessmentSubmission>> GradeSubmissionAsync(Guid submissionId, GradeSubmissionRequest request)
    {
        try
        {
            var submission = await GetSubmissionByIdAsync(submissionId).ConfigureAwait(false);
            if (submission == null)
            {
                return Result.Failure<AssessmentSubmission>(Error.NotFound("Submission", "Submission not found"));
            }

            var assessment = await GetAssessmentByIdAsync(submission.AssessmentId).ConfigureAwait(false);
            if (assessment == null)
            {
                return Result.Failure<AssessmentSubmission>(Error.NotFound("Assessment", "Assessment not found"));
            }

            submission.Grade(request.Score, assessment.PassingScore, assessment.MaxScore, request.GradedBy, request.Feedback);
            _context.Set<AssessmentSubmission>().Update(submission);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Submission graded: {SubmissionId} with score {Score}", submissionId, request.Score);

            return Result.Success(submission);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<AssessmentSubmission>(Error.Validation("Submission.InvalidScore", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error grading submission {SubmissionId}", submissionId);
            return Result.Failure<AssessmentSubmission>(Error.Failure("GradeSubmission", "Failed to grade submission"));
        }
    }

    public async Task<AssessmentSubmission?> GetSubmissionByIdAsync(Guid id)
    {
        return await _context.Set<AssessmentSubmission>()
            .FirstOrDefaultAsync(s => s.Id == id).ConfigureAwait(false);
    }

    public async Task<IEnumerable<AssessmentSubmission>> GetAssessmentSubmissionsAsync(Guid assessmentId)
    {
        return await _context.Set<AssessmentSubmission>()
            .Where(s => s.AssessmentId == assessmentId)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<IEnumerable<AssessmentSubmission>> GetUserSubmissionsAsync(Guid enrollmentId)
    {
        return await _context.Set<AssessmentSubmission>()
            .Where(s => s.EnrollmentId == enrollmentId)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<IEnumerable<AssessmentSubmission>> GetUserSubmissionsAsync(Guid enrollmentId, Guid userId)
    {
        return await _context.Set<AssessmentSubmission>()
            .Where(s => s.EnrollmentId == enrollmentId && s.UserId == userId)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<int> GetAttemptCountAsync(Guid assessmentId, Guid enrollmentId)
    {
        return await _context.Set<AssessmentSubmission>()
            .CountAsync(s => s.AssessmentId == assessmentId && s.EnrollmentId == enrollmentId).ConfigureAwait(false);
    }

    private async Task<int> GetHighestAttemptNumberAsync(Guid assessmentId, Guid enrollmentId)
    {
        return await _context.Set<AssessmentSubmission>()
            .Where(s => s.AssessmentId == assessmentId && s.EnrollmentId == enrollmentId)
            .Select(s => (int?)s.AttemptNumber)
            .MaxAsync()
            .ConfigureAwait(false) ?? 0;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (string.Equals(current.GetType().GetProperty("SqlState")?.GetValue(current) as string, "23505", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<Result<bool>> CanAttemptAsync(Guid assessmentId, Guid enrollmentId)
    {
        try
        {
            var assessment = await GetAssessmentByIdAsync(assessmentId).ConfigureAwait(false);
            if (assessment == null)
            {
                return Result.Failure<bool>(Error.NotFound("Assessment", "Assessment not found"));
            }

            if (!assessment.IsAvailable())
            {
                return Result.Success(false);
            }

            if (assessment.MaxAttempts.HasValue)
            {
                var attemptCount = await GetAttemptCountAsync(assessmentId, enrollmentId).ConfigureAwait(false);
                return Result.Success(attemptCount < assessment.MaxAttempts.Value);
            }

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking attempt eligibility for assessment {AssessmentId}", assessmentId);
            return Result.Failure<bool>(Error.Failure("CanAttempt", "Failed to check attempt eligibility"));
        }
    }
}
