using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Assessments;

/// <summary>
/// Controller for assessment management and submission handling
/// </summary>
[Route("api/assessments")]
[Authorize]
public class AssessmentsController : BaseApiController
{
    private readonly IAssessmentService _assessmentService;
    private readonly IActorContextAccessor _actorContextAccessor;
    private readonly ILogger<AssessmentsController> _logger;

    public AssessmentsController(
        IAssessmentService assessmentService,
        IActorContextAccessor actorContextAccessor,
        ILogger<AssessmentsController> logger)
    {
        _assessmentService = assessmentService;
        _actorContextAccessor = actorContextAccessor;
        _logger = logger;
    }

    // ===== ASSESSMENT MANAGEMENT =====

    /// <summary>
    /// Create a new assessment for a course
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AssessmentDto>> CreateAssessment([FromBody] CreateAssessmentRequest request)
    {
        var result = await _assessmentService.CreateAssessmentAsync(request);
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
        var assessment = await _assessmentService.GetAssessmentByIdAsync(id);
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
        var assessments = await _assessmentService.GetCourseAssessmentsAsync(courseId);
        return Ok(assessments.Select(AssessmentDto.FromEntity));
    }

    /// <summary>
    /// Update an assessment
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AssessmentDto>> UpdateAssessment(Guid id, [FromBody] UpdateAssessmentRequest request)
    {
        var result = await _assessmentService.UpdateAssessmentAsync(id, request);
        if (!result.IsSuccess)
        {
            return result.Error.Type == ErrorType.NotFound 
                ? NotFound(result.Error) 
                : BadRequest(result.Error);
        }

        return Ok(AssessmentDto.FromEntity(result.Value));
    }

    /// <summary>
    /// Delete an assessment
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteAssessment(Guid id)
    {
        var result = await _assessmentService.DeleteAssessmentAsync(id);
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
    public async Task<ActionResult<AssessmentSubmissionDto>> StartSubmission(
        Guid assessmentId, 
        [FromBody] StartSubmissionRequest request)
    {
        var actor = _actorContextAccessor.ActorContext;
        if (actor.SubjectIdAsGuid == null)
        {
            return Unauthorized();
        }

        var result = await _assessmentService.StartSubmissionAsync(
            assessmentId, 
            request.EnrollmentId, 
            actor.SubjectIdAsGuid.Value);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(
            nameof(GetSubmission), 
            new { submissionId = result.Value.Id }, 
            AssessmentSubmissionDto.FromEntity(result.Value));
    }

    /// <summary>
    /// Submit a completed assessment
    /// </summary>
    [HttpPost("submissions/{submissionId:guid}/submit")]
    public async Task<ActionResult<AssessmentSubmissionDto>> SubmitAssessment(Guid submissionId)
    {
        var result = await _assessmentService.SubmitAsync(submissionId);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(AssessmentSubmissionDto.FromEntity(result.Value));
    }

    /// <summary>
    /// Grade a submission
    /// </summary>
    [HttpPost("submissions/{submissionId:guid}/grade")]
    public async Task<ActionResult<AssessmentSubmissionDto>> GradeSubmission(
        Guid submissionId, 
        [FromBody] GradeSubmissionRequest request)
    {
        var result = await _assessmentService.GradeSubmissionAsync(submissionId, request);
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
    public async Task<ActionResult<AssessmentSubmissionDto>> GetSubmission(Guid submissionId)
    {
        var submission = await _assessmentService.GetSubmissionByIdAsync(submissionId);
        if (submission == null)
        {
            return NotFound();
        }

        return Ok(AssessmentSubmissionDto.FromEntity(submission));
    }

    /// <summary>
    /// Get all submissions for an assessment
    /// </summary>
    [HttpGet("{assessmentId:guid}/submissions")]
    public async Task<ActionResult<IEnumerable<AssessmentSubmissionDto>>> GetAssessmentSubmissions(Guid assessmentId)
    {
        var submissions = await _assessmentService.GetAssessmentSubmissionsAsync(assessmentId);
        return Ok(submissions.Select(AssessmentSubmissionDto.FromEntity));
    }

    /// <summary>
    /// Get my submissions for an enrollment
    /// </summary>
    [HttpGet("my-submissions/{enrollmentId:guid}")]
    public async Task<ActionResult<IEnumerable<AssessmentSubmissionDto>>> GetMySubmissions(Guid enrollmentId)
    {
        var submissions = await _assessmentService.GetUserSubmissionsAsync(enrollmentId);
        return Ok(submissions.Select(AssessmentSubmissionDto.FromEntity));
    }

    /// <summary>
    /// Check if user can attempt an assessment
    /// </summary>
    [HttpGet("{assessmentId:guid}/can-attempt/{enrollmentId:guid}")]
    public async Task<ActionResult<CanAttemptResponse>> CanAttempt(Guid assessmentId, Guid enrollmentId)
    {
        var result = await _assessmentService.CanAttemptAsync(assessmentId, enrollmentId);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        var attemptCount = await _assessmentService.GetAttemptCountAsync(assessmentId, enrollmentId);
        return Ok(new CanAttemptResponse(result.Value, attemptCount));
    }
}

// ===== DTOs =====

public record AssessmentDto(
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
    bool IsAvailable)
{
    public static AssessmentDto FromEntity(Assessment entity) => new(
        entity.Id,
        entity.CourseId,
        entity.ContentId,
        entity.Title,
        entity.Description,
        entity.Type,
        entity.MaxScore,
        entity.PassingScore,
        entity.TimeLimitMinutes,
        entity.MaxAttempts,
        entity.IsRequired,
        entity.Order,
        entity.AvailableFrom,
        entity.AvailableUntil,
        entity.IsAvailable());
}

public record AssessmentSubmissionDto(
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
    SubmissionStatus Status)
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
        entity.Status);
}

public record StartSubmissionRequest(Guid EnrollmentId);

public record CanAttemptResponse(bool CanAttempt, int CurrentAttemptCount);
