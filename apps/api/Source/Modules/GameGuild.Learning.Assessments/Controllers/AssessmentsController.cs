using Asp.Versioning;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Courses;
using GameGuild.Learning.Enrollments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Assessments;

/// <summary>
/// Controller for assessment management and submission handling
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/assessments")]
[Authorize]
public class AssessmentsController : BaseApiController
{
    private readonly IAssessmentService _assessmentService;
    private readonly IActorContextAccessor _actorContextAccessor;
    private readonly IProgramCrudService _programService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IPermissionQueryService _permissionQueryService;
    private readonly ILogger<AssessmentsController> _logger;

    public AssessmentsController(
        IAssessmentService assessmentService,
        IActorContextAccessor actorContextAccessor,
        IProgramCrudService programService,
        IEnrollmentService enrollmentService,
        IPermissionQueryService permissionQueryService,
        ILogger<AssessmentsController> logger)
    {
        _assessmentService = assessmentService;
        _actorContextAccessor = actorContextAccessor;
        _programService = programService;
        _enrollmentService = enrollmentService;
        _permissionQueryService = permissionQueryService;
        _logger = logger;
    }

    // ===== ASSESSMENT MANAGEMENT =====

    /// <summary>
    /// Create a new assessment for a course
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AssessmentDto>> CreateAssessment([FromBody] CreateAssessmentRequest request)
    {
        var program = await _programService.GetProgramByIdAsync(request.CourseId).ConfigureAwait(false);
        if (program == null) return NotFound();
        if (!await CanManageCourseAsync(program.Id).ConfigureAwait(false)) return Forbid();

        var result = await _assessmentService.CreateAssessmentAsync(request).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetAssessment), new { id = result.Value.Id }, AssessmentDto.FromEntity(result.Value));
    }

    /// <summary>
    /// Get an assessment by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssessmentDto>> GetAssessment(Guid id)
    {
        var assessment = await _assessmentService.GetAssessmentByIdAsync(id).ConfigureAwait(false);
        if (assessment == null)
        {
            return NotFound();
        }

        return Ok(AssessmentDto.FromEntity(assessment));
    }

    /// <summary>
    /// Get all assessments for a course
    /// </summary>
    [HttpGet("course/{courseId:guid}")]
    public async Task<ActionResult<IEnumerable<AssessmentDto>>> GetCourseAssessments(Guid courseId)
    {
        var assessments = await _assessmentService.GetCourseAssessmentsAsync(courseId).ConfigureAwait(false);
        return Ok(assessments.Select(AssessmentDto.FromEntity));
    }

    /// <summary>
    /// Get weighted assessment groups for a course.
    /// </summary>
    [HttpGet("course/{courseId:guid}/groups")]
    public async Task<ActionResult<IEnumerable<AssessmentGroupDto>>> GetCourseAssessmentGroups(Guid courseId)
    {
        var program = await _programService.GetProgramByIdAsync(courseId).ConfigureAwait(false);
        if (program == null) return NotFound();
        if (!await CanManageCourseAsync(program.Id).ConfigureAwait(false)) return Forbid();

        var groups = await _assessmentService.GetCourseAssessmentGroupsAsync(courseId).ConfigureAwait(false);
        return Ok(groups.Select(AssessmentGroupDto.FromEntity));
    }

    /// <summary>
    /// Get assessment score distribution and weighted group performance for a course.
    /// </summary>
    [HttpGet("course/{courseId:guid}/analytics")]
    public async Task<ActionResult<CourseAssessmentAnalyticsDto>> GetCourseAssessmentAnalytics(Guid courseId)
    {
        var program = await _programService.GetProgramByIdAsync(courseId).ConfigureAwait(false);
        if (program == null) return NotFound();
        if (!await CanManageCourseAsync(program.Id).ConfigureAwait(false)) return Forbid();

        var analytics = await _assessmentService.GetCourseAssessmentAnalyticsAsync(courseId).ConfigureAwait(false);
        return Ok(analytics);
    }

    /// <summary>
    /// Create a weighted assessment group.
    /// </summary>
    [HttpPost("groups")]
    public async Task<ActionResult<AssessmentGroupDto>> CreateAssessmentGroup([FromBody] CreateAssessmentGroupRequest request)
    {
        var program = await _programService.GetProgramByIdAsync(request.CourseId).ConfigureAwait(false);
        if (program == null) return NotFound();
        if (!await CanManageCourseAsync(program.Id).ConfigureAwait(false)) return Forbid();

        var result = await _assessmentService.CreateAssessmentGroupAsync(request).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(
            nameof(GetCourseAssessmentGroups),
            new { courseId = result.Value.CourseId },
            AssessmentGroupDto.FromEntity(result.Value));
    }

    /// <summary>
    /// Update a weighted assessment group.
    /// </summary>
    [HttpPut("groups/{id:guid}")]
    public async Task<ActionResult<AssessmentGroupDto>> UpdateAssessmentGroup(Guid id, [FromBody] UpdateAssessmentGroupRequest request)
    {
        var group = await _assessmentService.GetAssessmentGroupByIdAsync(id).ConfigureAwait(false);
        if (group == null) return NotFound();
        if (!await CanManageCourseAsync(group.CourseId).ConfigureAwait(false)) return Forbid();

        var result = await _assessmentService.UpdateAssessmentGroupAsync(id, request).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.Error.Type == ErrorType.NotFound
                ? NotFound(result.Error)
                : BadRequest(result.Error);
        }

        return Ok(AssessmentGroupDto.FromEntity(result.Value));
    }

    /// <summary>
    /// Delete a weighted assessment group.
    /// </summary>
    [HttpDelete("groups/{id:guid}")]
    public async Task<ActionResult> DeleteAssessmentGroup(Guid id)
    {
        var group = await _assessmentService.GetAssessmentGroupByIdAsync(id).ConfigureAwait(false);
        if (group == null) return NotFound();
        if (!await CanManageCourseAsync(group.CourseId).ConfigureAwait(false)) return Forbid();

        var result = await _assessmentService.DeleteAssessmentGroupAsync(id).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.Error.Type == ErrorType.NotFound
                ? NotFound(result.Error)
                : BadRequest(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Update an assessment
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AssessmentDto>> UpdateAssessment(Guid id, [FromBody] UpdateAssessmentRequest request)
    {
        var assessment = await _assessmentService.GetAssessmentByIdAsync(id).ConfigureAwait(false);
        if (assessment == null) return NotFound();
        if (!await CanManageCourseAsync(assessment.CourseId).ConfigureAwait(false)) return Forbid();

        var result = await _assessmentService.UpdateAssessmentAsync(id, request).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.Error.Type == ErrorType.NotFound 
                ? NotFound(result.Error) 
                : BadRequest(result.Error);
        }

        return Ok(AssessmentDto.FromEntity(result.Value));
    }

    /// <summary>
    /// Assign an assessment to a weighted group or clear the assignment.
    /// </summary>
    [HttpPut("{id:guid}/group")]
    public async Task<ActionResult<AssessmentDto>> AssignAssessmentToGroup(Guid id, [FromBody] AssignAssessmentGroupRequest request)
    {
        var assessment = await _assessmentService.GetAssessmentByIdAsync(id).ConfigureAwait(false);
        if (assessment == null) return NotFound();
        if (!await CanManageCourseAsync(assessment.CourseId).ConfigureAwait(false)) return Forbid();

        var result = await _assessmentService.AssignAssessmentToGroupAsync(id, request).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.Error.Type == ErrorType.NotFound
                ? NotFound(result.Error)
                : BadRequest(result.Error);
        }

        return Ok(AssessmentDto.FromEntity(result.Value));
    }

    /// <summary>
    /// Links this assessment to a cue in an interactive-video lesson.
    /// </summary>
    [HttpPost("{id:guid}/interactive-video-cues")]
    public async Task<ActionResult<InteractiveVideoAssessmentCueDto>> LinkInteractiveVideoCue(
        Guid id,
        [FromBody] LinkInteractiveVideoCueRequest request)
    {
        var assessment = await _assessmentService.GetAssessmentByIdAsync(id).ConfigureAwait(false);
        if (assessment == null) return NotFound();
        if (!await CanManageCourseAsync(assessment.CourseId).ConfigureAwait(false)) return Forbid();

        var result = await _assessmentService.LinkInteractiveVideoCueAsync(id, request).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.Error.Type == ErrorType.NotFound
                ? NotFound(result.Error)
                : BadRequest(result.Error);
        }

        return Ok(InteractiveVideoAssessmentCueDto.FromEntity(result.Value));
    }

    /// <summary>
    /// Gets the interactive-video cue links for this assessment.
    /// </summary>
    [HttpGet("{id:guid}/interactive-video-cues")]
    public async Task<ActionResult<IEnumerable<InteractiveVideoAssessmentCueDto>>> GetInteractiveVideoCues(Guid id)
    {
        var assessment = await _assessmentService.GetAssessmentByIdAsync(id).ConfigureAwait(false);
        if (assessment == null) return NotFound();
        if (!await CanManageCourseAsync(assessment.CourseId).ConfigureAwait(false)) return Forbid();

        var cues = await _assessmentService.GetInteractiveVideoCuesAsync(id).ConfigureAwait(false);
        return Ok(cues.Select(InteractiveVideoAssessmentCueDto.FromEntity));
    }

    /// <summary>
    /// Removes a manager-configured interactive-video cue link.
    /// </summary>
    [HttpDelete("{id:guid}/interactive-video-cues/{cueId:guid}")]
    public async Task<ActionResult> UnlinkInteractiveVideoCue(Guid id, Guid cueId)
    {
        var assessment = await _assessmentService.GetAssessmentByIdAsync(id).ConfigureAwait(false);
        if (assessment == null) return NotFound();
        if (!await CanManageCourseAsync(assessment.CourseId).ConfigureAwait(false)) return Forbid();

        var result = await _assessmentService.UnlinkInteractiveVideoCueAsync(id, cueId).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.Error.Type == ErrorType.NotFound ? NotFound(result.Error) : BadRequest(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Gets delivery-safe active cues for an enrolled learner and one video content item.
    /// </summary>
    [HttpGet("{assessmentId:guid}/interactive-video-cues/content/{contentId:guid}/enrollments/{enrollmentId:guid}")]
    public async Task<ActionResult<IEnumerable<LearnerInteractiveVideoAssessmentCueDto>>> GetLearnerInteractiveVideoCues(
        Guid assessmentId,
        Guid contentId,
        Guid enrollmentId)
    {
        var actorUserId = _actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (!actorUserId.HasValue) return Unauthorized();
        var assessment = await _assessmentService.GetAssessmentByIdAsync(assessmentId).ConfigureAwait(false);
        if (assessment == null) return NotFound();
        var enrollment = await _enrollmentService.GetAsync(enrollmentId).ConfigureAwait(false);
        if (enrollment == null || enrollment.CourseId != assessment.CourseId) return NotFound();
        if (enrollment.UserId != actorUserId.Value) return Forbid();

        var cues = await _assessmentService
            .GetInteractiveVideoCuesForContentAsync(assessmentId, contentId)
            .ConfigureAwait(false);
        return Ok(cues.Select(LearnerInteractiveVideoAssessmentCueDto.FromEntity));
    }

    /// <summary>
    /// Delete an assessment
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteAssessment(Guid id)
    {
        var assessment = await _assessmentService.GetAssessmentByIdAsync(id).ConfigureAwait(false);
        if (assessment == null) return NotFound();
        if (!await CanManageCourseAsync(assessment.CourseId).ConfigureAwait(false)) return Forbid();

        var result = await _assessmentService.DeleteAssessmentAsync(id).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.Error.Type == ErrorType.NotFound 
                ? NotFound(result.Error) 
                : BadRequest(result.Error);
        }

        return NoContent();
    }

    // ===== SUBMISSION MANAGEMENT =====

    /// <summary>
    /// Start a new assessment attempt
    /// </summary>
    [HttpPost("{assessmentId:guid}/submissions/start")]
    public async Task<ActionResult<LearnerAssessmentSubmissionDto>> StartSubmission(
        Guid assessmentId, 
        [FromBody] StartSubmissionRequest request)
    {
        var actor = _actorContextAccessor.ActorContext;
        if (actor.SubjectIdAsGuid == null)
        {
            return Unauthorized();
        }

        var assessment = await _assessmentService.GetAssessmentByIdAsync(assessmentId).ConfigureAwait(false);
        if (assessment == null) return NotFound();
        var enrollment = await _enrollmentService.GetAsync(request.EnrollmentId).ConfigureAwait(false);
        if (enrollment == null || enrollment.CourseId != assessment.CourseId) return BadRequest();
        var canManage = await CanManageCourseAsync(assessment.CourseId).ConfigureAwait(false);
        if (!canManage && enrollment.UserId != actor.SubjectIdAsGuid.Value) return Forbid();

        var result = await _assessmentService.StartSubmissionAsync(
            assessmentId, 
            request.EnrollmentId,
            enrollment.UserId).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(
            nameof(GetSubmission), 
            new { submissionId = result.Value.Id }, 
            LearnerAssessmentSubmissionDto.FromEntity(result.Value));
    }

    /// <summary>
    /// Submit a completed assessment
    /// </summary>
    [HttpPost("submissions/{submissionId:guid}/submit")]
    public async Task<ActionResult<LearnerAssessmentSubmissionDto>> SubmitAssessment(
        Guid submissionId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SubmitAssessmentRequest? request = null)
    {
        var actorUserId = _actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (!actorUserId.HasValue) return Unauthorized();
        var submission = await _assessmentService.GetSubmissionByIdAsync(submissionId).ConfigureAwait(false);
        if (submission == null) return NotFound();
        if (submission.UserId != actorUserId.Value) return Forbid();

        var result = await _assessmentService.SubmitAsync(submissionId, request).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(LearnerAssessmentSubmissionDto.FromEntity(result.Value));
    }

    /// <summary>
    /// Grade a submission
    /// </summary>
    [HttpPost("submissions/{submissionId:guid}/grade")]
    public async Task<ActionResult<AssessmentSubmissionDto>> GradeSubmission(
        Guid submissionId, 
        [FromBody] GradeSubmissionRequest request)
    {
        var submission = await _assessmentService.GetSubmissionByIdAsync(submissionId).ConfigureAwait(false);
        if (submission == null) return NotFound();
        var assessment = await _assessmentService.GetAssessmentByIdAsync(submission.AssessmentId).ConfigureAwait(false);
        if (assessment == null) return NotFound();
        if (!await CanManageCourseAsync(assessment.CourseId).ConfigureAwait(false)) return Forbid();

        var graderId = _actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (!graderId.HasValue) return Unauthorized();
        var result = await _assessmentService.GradeSubmissionAsync(
                submissionId,
                request with { GradedBy = graderId.Value })
            .ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(AssessmentSubmissionDto.FromEntity(result.Value));
    }

    /// <summary>
    /// Get a submission by ID
    /// </summary>
    [HttpGet("submissions/{submissionId:guid}")]
    public async Task<ActionResult<object>> GetSubmission(Guid submissionId)
    {
        var submission = await _assessmentService.GetSubmissionByIdAsync(submissionId).ConfigureAwait(false);
        if (submission == null)
        {
            return NotFound();
        }

        var actorUserId = _actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (!actorUserId.HasValue) return Unauthorized();
        var assessment = await _assessmentService.GetAssessmentByIdAsync(submission.AssessmentId).ConfigureAwait(false);
        if (assessment == null) return NotFound();
        if (submission.UserId == actorUserId.Value)
        {
            if (!await IsActorInProgramTenantAsync(assessment.CourseId).ConfigureAwait(false)) return Forbid();
            return Ok(LearnerAssessmentSubmissionDto.FromEntity(submission));
        }

        if (!await CanReviewCourseAsync(assessment.CourseId).ConfigureAwait(false)) return Forbid();
        return Ok(AssessmentSubmissionDto.FromEntity(submission));
    }

    /// <summary>
    /// Get all submissions for an assessment
    /// </summary>
    [HttpGet("{assessmentId:guid}/submissions")]
    public async Task<ActionResult<IEnumerable<AssessmentSubmissionDto>>> GetAssessmentSubmissions(Guid assessmentId)
    {
        var assessment = await _assessmentService.GetAssessmentByIdAsync(assessmentId).ConfigureAwait(false);
        if (assessment == null) return NotFound();
        if (!await CanReviewCourseAsync(assessment.CourseId).ConfigureAwait(false)) return Forbid();

        var submissions = await _assessmentService.GetAssessmentSubmissionsAsync(assessmentId).ConfigureAwait(false);
        return Ok(submissions.Select(AssessmentSubmissionDto.FromEntity));
    }

    /// <summary>
    /// Get my submissions for an enrollment
    /// </summary>
    [HttpGet("my-submissions/{enrollmentId:guid}")]
    public async Task<ActionResult<IEnumerable<LearnerAssessmentSubmissionDto>>> GetMySubmissions(Guid enrollmentId)
    {
        var actorUserId = _actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (!actorUserId.HasValue) return Unauthorized();
        var submissions = await _assessmentService.GetUserSubmissionsAsync(enrollmentId, actorUserId.Value).ConfigureAwait(false);
        var assessmentVisibility = new Dictionary<Guid, bool>();
        var visibleSubmissions = new List<LearnerAssessmentSubmissionDto>();
        foreach (var submission in submissions)
        {
            if (!assessmentVisibility.TryGetValue(submission.AssessmentId, out var isVisible))
            {
                var assessment = await _assessmentService.GetAssessmentByIdAsync(submission.AssessmentId).ConfigureAwait(false);
                isVisible = assessment != null &&
                            await IsActorInProgramTenantAsync(assessment.CourseId).ConfigureAwait(false);
                assessmentVisibility[submission.AssessmentId] = isVisible;
            }

            if (isVisible) visibleSubmissions.Add(LearnerAssessmentSubmissionDto.FromEntity(submission));
        }

        return Ok(visibleSubmissions);
    }

    /// <summary>
    /// Check if user can attempt an assessment
    /// </summary>
    [HttpGet("{assessmentId:guid}/can-attempt/{enrollmentId:guid}")]
    public async Task<ActionResult<CanAttemptResponse>> CanAttempt(Guid assessmentId, Guid enrollmentId)
    {
        var assessment = await _assessmentService.GetAssessmentByIdAsync(assessmentId).ConfigureAwait(false);
        if (assessment == null) return NotFound();
        var actorUserId = _actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (!actorUserId.HasValue) return Unauthorized();
        var enrollment = await _enrollmentService.GetAsync(enrollmentId).ConfigureAwait(false);
        if (enrollment == null || enrollment.CourseId != assessment.CourseId) return BadRequest();
        if (!await CanManageCourseAsync(assessment.CourseId).ConfigureAwait(false) && enrollment.UserId != actorUserId.Value) return Forbid();

        var result = await _assessmentService.CanAttemptAsync(assessmentId, enrollmentId).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        var attemptCount = await _assessmentService.GetAttemptCountAsync(assessmentId, enrollmentId).ConfigureAwait(false);
        return Ok(new CanAttemptResponse(result.Value, attemptCount));
    }

    private async Task<bool> CanManageCourseAsync(Guid courseId)
    {
        var actor = _actorContextAccessor.ActorContext;
        if (actor.IsSystemAdmin) return true;
        if (!actor.SubjectIdAsGuid.HasValue) return false;

        var program = await _programService.GetProgramByIdAsync(courseId).ConfigureAwait(false);
        if (program == null) return false;
        if (!actor.TenantId.HasValue) return false;
        if (program.TenantId.HasValue && program.TenantId != actor.TenantId) return false;
        if (program.CreatorId == actor.SubjectIdAsGuid.Value) return true;

        foreach (var permission in new[] { PermissionType.Edit, PermissionType.Create, PermissionType.Delete })
        {
            var permissionName = $"{nameof(Program)}.{courseId}.{permission}";
            if (await _permissionQueryService.HasTenantPermissionAsync(
                    actor.SubjectIdAsGuid.Value,
                    actor.TenantId,
                    permissionName).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> IsActorInProgramTenantAsync(Guid courseId)
    {
        var actor = _actorContextAccessor.ActorContext;
        var program = await _programService.GetProgramByIdAsync(courseId).ConfigureAwait(false);
        if (program == null) return false;
        if (actor.IsSystemAdmin) return true;

        return actor.TenantId.HasValue &&
               (!program.TenantId.HasValue || program.TenantId == actor.TenantId);
    }

    private async Task<bool> CanReviewCourseAsync(Guid courseId)
    {
        var actor = _actorContextAccessor.ActorContext;
        if (!actor.SubjectIdAsGuid.HasValue) return false;
        if (!await IsActorInProgramTenantAsync(courseId).ConfigureAwait(false)) return false;

        var permissionName = $"{nameof(Program)}.{courseId}.{PermissionType.Review}";
        return await _permissionQueryService.HasTenantPermissionAsync(
                actor.SubjectIdAsGuid.Value,
                actor.TenantId,
                permissionName)
            .ConfigureAwait(false);
    }
}

// ===== DTOs =====

public sealed record AssessmentDto(
    Guid Id,
    Guid CourseId,
    Guid? ContentId,
    string Title,
    string? Description,
    AssessmentType Type,
    int MaxScore,
    int PassingScore,
    int? TimeLimitMinutes,
    int? MaxAttempts,
    bool IsRequired,
    int Order,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    Guid? AssessmentGroupId,
    string? AssessmentGroupName,
    decimal? AssessmentGroupWeightPercent,
    int? AssessmentGroupOrder,
    bool IsAvailable,
    SubmissionModality SubmissionModalities = SubmissionModality.Text,
    AssessmentPresentationMode PresentationMode = AssessmentPresentationMode.SingleStep,
    DateTime? DueAt = null,
    bool AllowLateSubmissions = false,
    DateTime? LateSubmissionDeadline = null)
{
    public static AssessmentDto FromEntity(Assessment entity) => new(
        entity.Id,
        entity.CourseId,
        entity.ContentId,
        entity.Title,
        entity.Description,
        Assessment.NormalizeType(entity.Type),
        entity.MaxScore,
        entity.PassingScore,
        entity.TimeLimitMinutes,
        entity.MaxAttempts,
        entity.IsRequired,
        entity.Order,
        entity.AvailableFrom,
        entity.AvailableUntil,
        entity.AssessmentGroupId,
        entity.AssessmentGroup?.Name,
        entity.AssessmentGroup?.WeightPercent,
        entity.AssessmentGroup?.Order,
        entity.IsAvailable(),
        entity.SubmissionModalities,
        entity.PresentationMode,
        entity.DueAt,
        entity.AllowLateSubmissions,
        entity.LateSubmissionDeadline);
}

public sealed record InteractiveVideoAssessmentCueDto(
    Guid Id,
    Guid AssessmentId,
    Guid ContentId,
    string CueId,
    decimal? CuePositionSeconds)
{
    public static InteractiveVideoAssessmentCueDto FromEntity(InteractiveVideoAssessmentCue entity) => new(
        entity.Id,
        entity.AssessmentId,
        entity.ContentId,
        entity.CueId,
        entity.CuePositionSeconds);
}

public sealed record LearnerInteractiveVideoAssessmentCueDto(
    string CueId,
    decimal? CuePositionSeconds)
{
    public static LearnerInteractiveVideoAssessmentCueDto FromEntity(InteractiveVideoAssessmentCue entity) => new(
        entity.CueId,
        entity.CuePositionSeconds);
}

public sealed record AssessmentGroupDto(
    Guid Id,
    Guid CourseId,
    string Name,
    string? Description,
    decimal WeightPercent,
    int Order)
{
    public static AssessmentGroupDto FromEntity(AssessmentGroup entity) => new(
        entity.Id,
        entity.CourseId,
        entity.Name,
        entity.Description,
        entity.WeightPercent,
        entity.Order);
}

public sealed record AssessmentSubmissionDto(
    Guid Id,
    Guid AssessmentId,
    Guid EnrollmentId,
    Guid UserId,
    int AttemptNumber,
    int? Score,
    bool? Passed,
    DateTime StartedAt,
    DateTime? SubmittedAt,
    DateTime? GradedAt,
    Guid? GradedBy,
    string? Feedback,
    SubmissionStatus Status,
    bool IsLate = false,
    SubmissionModality SubmittedModalities = SubmissionModality.None,
    string? TextPayload = null,
    string? FilePayload = null,
    string? UrlPayload = null,
    string? CodePayload = null,
    string? MediaPayload = null,
    string? ProjectPayload = null,
    string? StructuredAnswerPayload = null)
{
    public static AssessmentSubmissionDto FromEntity(AssessmentSubmission entity) => new(
        entity.Id,
        entity.AssessmentId,
        entity.EnrollmentId,
        entity.UserId,
        entity.AttemptNumber,
        entity.Score,
        entity.Passed,
        entity.StartedAt,
        entity.SubmittedAt,
        entity.GradedAt,
        entity.GradedBy,
        entity.Feedback,
        entity.Status,
        entity.IsLate,
        entity.SubmittedModalities,
        entity.TextPayload,
        entity.FilePayload,
        entity.UrlPayload,
        entity.CodePayload,
        entity.MediaPayload,
        entity.ProjectPayload,
        entity.StructuredAnswerPayload);
}

public sealed record LearnerAssessmentSubmissionDto(
    Guid Id,
    Guid AssessmentId,
    Guid EnrollmentId,
    int AttemptNumber,
    int? Score,
    bool? Passed,
    DateTime StartedAt,
    DateTime? SubmittedAt,
    DateTime? GradedAt,
    string? Feedback,
    SubmissionStatus Status,
    bool IsLate,
    SubmissionModality SubmittedModalities,
    string? TextPayload,
    string? FilePayload,
    string? UrlPayload,
    string? CodePayload,
    string? MediaPayload,
    string? ProjectPayload,
    string? StructuredAnswerPayload)
{
    public static LearnerAssessmentSubmissionDto FromEntity(AssessmentSubmission entity) => new(
        entity.Id, entity.AssessmentId, entity.EnrollmentId, entity.AttemptNumber,
        entity.Score, entity.Passed, entity.StartedAt, entity.SubmittedAt, entity.GradedAt,
        entity.Feedback, entity.Status, entity.IsLate, entity.SubmittedModalities,
        entity.TextPayload, entity.FilePayload, entity.UrlPayload, entity.CodePayload,
        entity.MediaPayload, entity.ProjectPayload, entity.StructuredAnswerPayload);
}

public sealed record StartSubmissionRequest(Guid EnrollmentId);

public sealed record CanAttemptResponse(bool CanAttempt, int CurrentAttemptCount);
