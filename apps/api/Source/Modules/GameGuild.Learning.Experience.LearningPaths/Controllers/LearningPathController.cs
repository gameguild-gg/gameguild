using Asp.Versioning;
using GameGuild.Enums;
using GameGuild.Identity.Authorization;
using GameGuild.Learning.Attributes;
using GameGuild.Learning.Filters;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Learning.Experience.LearningPaths;

/// <summary>
/// REST API controller for Learning Paths operations
/// </summary>
/// <remarks>
/// LearningPathController implements a complete API for curated learning path management:
/// - Learning path CRUD operations
/// - Course ordering within paths
/// - User enrollment and progress tracking
/// - Statistics and analytics
/// 
/// Public endpoints for discovery
/// Admin endpoints for path management
/// User endpoints for enrollment and progress
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/learning-paths")]
[LxpCapabilityFilter]
[LxpCapability(LxpCapabilities.LearningPaths)]
public class LearningPathController(ILearningPathService learningPathService) : ControllerBase
{
    // ===== PUBLIC DISCOVERY ENDPOINTS =====

    /// <summary>
    /// Get all published learning paths
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LearningPathDto>>> GetPublishedPaths(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] LearningPathDifficulty? difficulty = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var paths = await learningPathService.GetPublishedPathsAsync(tenantId, difficulty, skip, take);
        return Ok(paths.Select(p => p.ToDto()));
    }

    /// <summary>
    /// Search learning paths
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<LearningPathDto>>> SearchPaths(
        [FromQuery] string q,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] LearningPathDifficulty? difficulty = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Search query is required");
        }

        var paths = await learningPathService.SearchPathsAsync(q, tenantId, difficulty, skip, take);
        return Ok(paths.Select(p => p.ToDto()));
    }

    /// <summary>
    /// Get featured learning paths
    /// </summary>
    [HttpGet("featured")]
    public async Task<ActionResult<IEnumerable<LearningPathDto>>> GetFeaturedPaths(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] int take = 10)
    {
        var paths = await learningPathService.GetFeaturedPathsAsync(tenantId, take);
        return Ok(paths.Select(p => p.ToDto()));
    }

    /// <summary>
    /// Get popular learning paths
    /// </summary>
    [HttpGet("popular")]
    public async Task<ActionResult<IEnumerable<LearningPathDto>>> GetPopularPaths(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] int daysBack = 30,
        [FromQuery] int take = 10)
    {
        var paths = await learningPathService.GetPopularPathsAsync(tenantId, daysBack, take);
        return Ok(paths.Select(p => p.ToDto()));
    }

    /// <summary>
    /// Get a learning path by slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<LearningPathDetailDto>> GetPathBySlug(
        string slug,
        [FromQuery] Guid? tenantId = null)
    {
        var path = await learningPathService.GetPathBySlugAsync(slug, tenantId);
        if (path == null) return NotFound();
        return Ok(path.ToDetailDto());
    }

    /// <summary>
    /// Get a learning path by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<LearningPathDetailDto>> GetPathById(Guid id)
    {
        var path = await learningPathService.GetPathByIdAsync(id, includeCourses: true);
        if (path == null) return NotFound();
        return Ok(path.ToDetailDto());
    }

    // ===== CREATOR/ADMIN PATH MANAGEMENT ENDPOINTS =====

    /// <summary>
    /// Get learning paths by creator
    /// </summary>
    [HttpGet("creator/{creatorId}")]
    public async Task<ActionResult<IEnumerable<LearningPathDto>>> GetPathsByCreator(
        Guid creatorId,
        [FromQuery] bool includeUnpublished = false,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var paths = await learningPathService.GetPathsByCreatorAsync(creatorId, includeUnpublished, skip, take);
        return Ok(paths.Select(p => p.ToDto()));
    }

    /// <summary>
    /// Create a new learning path
    /// </summary>
    [HttpPost]
    [RequireContentTypePermission<LearningPath>(PermissionType.Create)]
    public async Task<ActionResult<LearningPathDto>> CreatePath(
        [FromBody] CreateLearningPathDto dto,
        [FromQuery] Guid creatorId,
        [FromQuery] Guid? tenantId = null)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var path = await learningPathService.CreatePathAsync(dto, creatorId, tenantId);
        return CreatedAtAction(nameof(GetPathById), new { id = path.Id }, path.ToDto());
    }

    /// <summary>
    /// Update a learning path
    /// </summary>
    [HttpPut("{id}")]
    [RequireResourcePermission<PermissionType, LearningPath>(PermissionType.Edit)]
    public async Task<ActionResult<LearningPathDto>> UpdatePath(
        Guid id,
        [FromBody] UpdateLearningPathDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var path = await learningPathService.UpdatePathAsync(id, dto);
        if (path == null) return NotFound();
        return Ok(path.ToDto());
    }

    /// <summary>
    /// Delete a learning path
    /// </summary>
    [HttpDelete("{id}")]
    [RequireResourcePermission<PermissionType, LearningPath>(PermissionType.Delete)]
    public async Task<IActionResult> DeletePath(Guid id)
    {
        var success = await learningPathService.DeletePathAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Publish a learning path
    /// </summary>
    [HttpPost("{id}/publish")]
    [RequireResourcePermission<PermissionType, LearningPath>(PermissionType.Publish)]
    public async Task<ActionResult<LearningPathDto>> PublishPath(Guid id)
    {
        try
        {
            var path = await learningPathService.PublishPathAsync(id);
            if (path == null) return NotFound();
            return Ok(path.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Unpublish a learning path
    /// </summary>
    [HttpPost("{id}/unpublish")]
    [RequireResourcePermission<PermissionType, LearningPath>(PermissionType.Unpublish)]
    public async Task<ActionResult<LearningPathDto>> UnpublishPath(Guid id)
    {
        var path = await learningPathService.UnpublishPathAsync(id);
        if (path == null) return NotFound();
        return Ok(path.ToDto());
    }

    // ===== COURSE MANAGEMENT ENDPOINTS =====

    /// <summary>
    /// Add a course to a learning path
    /// </summary>
    [HttpPost("{id}/courses")]
    [RequireResourcePermission<PermissionType, LearningPath>(PermissionType.Edit)]
    public async Task<ActionResult<LearningPathDetailDto>> AddCourseToPath(
        Guid id,
        [FromBody] AddCourseToPathDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var path = await learningPathService.AddCourseToPathAsync(id, dto);
            if (path == null) return NotFound();
            return Ok(path.ToDetailDto());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Remove a course from a learning path
    /// </summary>
    [HttpDelete("{id}/courses/{courseId}")]
    [RequireResourcePermission<PermissionType, LearningPath>(PermissionType.Edit)]
    public async Task<IActionResult> RemoveCourseFromPath(Guid id, Guid courseId)
    {
        var success = await learningPathService.RemoveCourseFromPathAsync(id, courseId);
        if (!success) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Reorder courses in a learning path
    /// </summary>
    [HttpPut("{id}/courses/order")]
    [RequireResourcePermission<PermissionType, LearningPath>(PermissionType.Edit)]
    public async Task<ActionResult<LearningPathDetailDto>> ReorderCourses(
        Guid id,
        [FromBody] ReorderCoursesDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var path = await learningPathService.ReorderCoursesAsync(id, dto);
        if (path == null) return NotFound();
        return Ok(path.ToDetailDto());
    }

    // ===== USER ENROLLMENT ENDPOINTS =====

    /// <summary>
    /// Enroll current user in a learning path
    /// </summary>
    [HttpPost("{id}/enroll")]
    public async Task<ActionResult<LearningPathEnrollmentDto>> EnrollInPath(
        Guid id,
        [FromQuery] Guid userId)
    {
        try
        {
            var enrollment = await learningPathService.EnrollAsync(id, userId);
            return CreatedAtAction(nameof(GetUserEnrollment), new { id, userId }, enrollment.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Unenroll user from a learning path
    /// </summary>
    [HttpPost("{id}/unenroll")]
    public async Task<IActionResult> UnenrollFromPath(
        Guid id,
        [FromQuery] Guid userId)
    {
        var success = await learningPathService.UnenrollAsync(id, userId);
        if (!success) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Get user's enrollment in a specific path
    /// </summary>
    [HttpGet("{id}/enrollment/{userId}")]
    public async Task<ActionResult<LearningPathEnrollmentDto>> GetUserEnrollment(Guid id, Guid userId)
    {
        var enrollment = await learningPathService.GetEnrollmentAsync(id, userId);
        if (enrollment == null) return NotFound();
        return Ok(enrollment.ToDto());
    }

    /// <summary>
    /// Check if user is enrolled in a path
    /// </summary>
    [HttpGet("{id}/enrollment/{userId}/check")]
    public async Task<ActionResult<bool>> CheckEnrollment(Guid id, Guid userId)
    {
        var isEnrolled = await learningPathService.IsEnrolledAsync(id, userId);
        return Ok(isEnrolled);
    }

    /// <summary>
    /// Update user's progress in a learning path
    /// </summary>
    [HttpPut("{id}/progress")]
    public async Task<ActionResult<LearningPathEnrollmentDto>> UpdateProgress(
        Guid id,
        [FromQuery] Guid userId,
        [FromBody] UpdatePathProgressDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var enrollment = await learningPathService.UpdateProgressAsync(id, userId, dto);
        if (enrollment == null) return NotFound();
        return Ok(enrollment.ToDto());
    }

    /// <summary>
    /// Mark a learning path as completed
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<ActionResult<LearningPathEnrollmentDto>> CompletePath(
        Guid id,
        [FromQuery] Guid userId)
    {
        var enrollment = await learningPathService.CompletePathAsync(id, userId);
        if (enrollment == null) return NotFound();
        return Ok(enrollment.ToDto());
    }

    /// <summary>
    /// Abandon a learning path enrollment
    /// </summary>
    [HttpPost("{id}/abandon")]
    public async Task<IActionResult> AbandonPath(
        Guid id,
        [FromQuery] Guid userId)
    {
        var success = await learningPathService.AbandonPathAsync(id, userId);
        if (!success) return NotFound();
        return NoContent();
    }

    // ===== USER ENROLLMENT LIST ENDPOINTS =====

    /// <summary>
    /// Get all enrolled paths for a user
    /// </summary>
    [HttpGet("user/{userId}/enrollments")]
    public async Task<ActionResult<IEnumerable<LearningPathEnrollmentDto>>> GetUserEnrollments(
        Guid userId,
        [FromQuery] LearningPathEnrollmentStatus? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var enrollments = await learningPathService.GetUserEnrollmentsAsync(userId, status, skip, take);
        return Ok(enrollments.Select(e => e.ToDto()));
    }

    /// <summary>
    /// Get completed paths for a user
    /// </summary>
    [HttpGet("user/{userId}/completed")]
    public async Task<ActionResult<IEnumerable<LearningPathEnrollmentDto>>> GetUserCompletedPaths(
        Guid userId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        var enrollments = await learningPathService.GetUserCompletedPathsAsync(userId, skip, take);
        return Ok(enrollments.Select(e => e.ToDto()));
    }

    // ===== STATISTICS ENDPOINTS =====

    /// <summary>
    /// Get statistics for a learning path
    /// </summary>
    [HttpGet("{id}/statistics")]
    [RequireResourcePermission<PermissionType, LearningPath>(PermissionType.Read)]
    public async Task<ActionResult<LearningPathStatisticsDto>> GetPathStatistics(Guid id)
    {
        var statistics = await learningPathService.GetPathStatisticsAsync(id);
        if (statistics == null) return NotFound();
        return Ok(statistics);
    }

    /// <summary>
    /// Get all enrollments for a learning path (admin)
    /// </summary>
    [HttpGet("{id}/enrollments")]
    [RequireResourcePermission<PermissionType, LearningPath>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<LearningPathEnrollmentDto>>> GetPathEnrollments(
        Guid id,
        [FromQuery] LearningPathEnrollmentStatus? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var enrollments = await learningPathService.GetPathEnrollmentsAsync(id, status, skip, take);
        return Ok(enrollments.Select(e => e.ToDto()));
    }
}
