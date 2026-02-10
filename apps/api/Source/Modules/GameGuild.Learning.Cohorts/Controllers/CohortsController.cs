using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Cohorts;

/// <summary>
/// Controller for cohort management
/// </summary>
[Route("api/cohorts")]
[Authorize]
public class CohortsController : BaseApiController
{
    private readonly ICohortService _cohortService;
    private readonly IActorContextAccessor _actorContextAccessor;
    private readonly ILogger<CohortsController> _logger;

    public CohortsController(
        ICohortService cohortService,
        IActorContextAccessor actorContextAccessor,
        ILogger<CohortsController> logger)
    {
        _cohortService = cohortService;
        _actorContextAccessor = actorContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Create a new cohort
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CohortDto>> CreateCohort([FromBody] CreateCohortRequest request)
    {
        var actor = _actorContextAccessor.ActorContext;
        
        // Use tenant from actor if not specified in request
        var effectiveRequest = request with { TenantId = request.TenantId ?? actor.TenantId };

        var result = await _cohortService.CreateCohortAsync(effectiveRequest).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetCohort), new { id = result.Value.Id }, CohortDto.FromEntity(result.Value));
    }

    /// <summary>
    /// Get a cohort by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CohortDto>> GetCohort(Guid id)
    {
        var cohort = await _cohortService.GetCohortByIdAsync(id).ConfigureAwait(false);
        if (cohort == null)
        {
            return NotFound();
        }

        return Ok(CohortDto.FromEntity(cohort));
    }

    /// <summary>
    /// Get all cohorts for a course
    /// </summary>
    [HttpGet("course/{courseId:guid}")]
    public async Task<ActionResult<IEnumerable<CohortDto>>> GetCourseCohorts(Guid courseId)
    {
        var actor = _actorContextAccessor.ActorContext;
        var cohorts = await _cohortService.GetCoursCohortsAsync(courseId, actor.TenantId).ConfigureAwait(false);
        return Ok(cohorts.Select(CohortDto.FromEntity));
    }

    /// <summary>
    /// Get active cohorts for a course
    /// </summary>
    [HttpGet("course/{courseId:guid}/active")]
    public async Task<ActionResult<IEnumerable<CohortDto>>> GetActiveCohorts(Guid courseId)
    {
        var actor = _actorContextAccessor.ActorContext;
        var cohorts = await _cohortService.GetActiveCohortsAsync(courseId, actor.TenantId).ConfigureAwait(false);
        return Ok(cohorts.Select(CohortDto.FromEntity));
    }

    /// <summary>
    /// Get enrollable cohorts for a course (open with capacity)
    /// </summary>
    [HttpGet("course/{courseId:guid}/enrollable")]
    public async Task<ActionResult<IEnumerable<CohortDto>>> GetEnrollableCohorts(Guid courseId)
    {
        var actor = _actorContextAccessor.ActorContext;
        var cohorts = await _cohortService.GetEnrollableCohortsAsync(courseId, actor.TenantId).ConfigureAwait(false);
        return Ok(cohorts.Select(CohortDto.FromEntity));
    }

    /// <summary>
    /// Update a cohort
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CohortDto>> UpdateCohort(Guid id, [FromBody] UpdateCohortRequest request)
    {
        var result = await _cohortService.UpdateCohortAsync(id, request).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.Error.Type == ErrorType.NotFound 
                ? NotFound(result.Error) 
                : BadRequest(result.Error);
        }

        return Ok(CohortDto.FromEntity(result.Value));
    }

    /// <summary>
    /// Open a cohort for enrollment
    /// </summary>
    [HttpPost("{id:guid}/open")]
    public async Task<ActionResult<CohortDto>> OpenCohort(Guid id)
    {
        var result = await _cohortService.OpenCohortAsync(id).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.Error.Type == ErrorType.NotFound 
                ? NotFound(result.Error) 
                : BadRequest(result.Error);
        }

        return Ok(CohortDto.FromEntity(result.Value));
    }

    /// <summary>
    /// Close a cohort for enrollment
    /// </summary>
    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<CohortDto>> CloseCohort(Guid id)
    {
        var result = await _cohortService.CloseCohortAsync(id).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.Error.Type == ErrorType.NotFound 
                ? NotFound(result.Error) 
                : BadRequest(result.Error);
        }

        return Ok(CohortDto.FromEntity(result.Value));
    }

    /// <summary>
    /// Mark a cohort as completed
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<CohortDto>> CompleteCohort(Guid id)
    {
        var result = await _cohortService.CompleteCohortAsync(id).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.Error.Type == ErrorType.NotFound 
                ? NotFound(result.Error) 
                : BadRequest(result.Error);
        }

        return Ok(CohortDto.FromEntity(result.Value));
    }

    /// <summary>
    /// Cancel a cohort
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<CohortDto>> CancelCohort(Guid id)
    {
        var result = await _cohortService.CancelCohortAsync(id).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.Error.Type == ErrorType.NotFound 
                ? NotFound(result.Error) 
                : BadRequest(result.Error);
        }

        return Ok(CohortDto.FromEntity(result.Value));
    }

    /// <summary>
    /// Delete a cohort
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteCohort(Guid id)
    {
        var result = await _cohortService.DeleteCohortAsync(id).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.Error.Type == ErrorType.NotFound 
                ? NotFound(result.Error) 
                : BadRequest(result.Error);
        }

        return NoContent();
    }
}

// ===== DTOs =====

public sealed record CohortDto(
    Guid Id,
    Guid CourseId,
    Guid? TenantId,
    string Name,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    int MaxCapacity,
    int CurrentEnrollmentCount,
    int AvailableSpots,
    CohortStatus Status,
    bool IsOpen,
    bool CanEnroll,
    Guid? InstructorId,
    string? MeetingSchedule,
    DateTime CreatedAt)
{
    public static CohortDto FromEntity(Cohort entity) => new(
        entity.Id,
        entity.CourseId,
        entity.TenantId,
        entity.Name,
        entity.Description,
        entity.StartDate,
        entity.EndDate,
        entity.MaxCapacity,
        entity.CurrentEnrollmentCount,
        entity.MaxCapacity - entity.CurrentEnrollmentCount,
        entity.Status,
        entity.IsOpen,
        entity.CanEnroll(),
        entity.InstructorId,
        entity.MeetingSchedule,
        entity.CreatedAt);
}
