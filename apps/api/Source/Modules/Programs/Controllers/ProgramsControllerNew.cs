using GameGuild.Common;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Programs.Commands;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Programs.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Programs.Controllers;

/// <summary>
/// CQRS-based REST API controller for Program management
/// Uses MediatR for command and query handling with proper separation of concerns
/// Implements comprehensive CRUD operations with authorization
/// </summary>
[ApiController]
[Route("api/v2/[controller]")]
public class ProgramsControllerNew(IMediator mediator, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    private string? UserId => httpContextAccessor.HttpContext?.User?.Identity?.Name;

    // ===== BASIC CRUD OPERATIONS =====

    /// <summary>
    /// Get all programs with filtering and pagination
    /// </summary>
    [HttpGet]
    [Authorize]
    [RequireContentTypePermission<Program>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<Program>>> GetPrograms(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? search = null,
        [FromQuery] ProgramCategory? category = null,
        [FromQuery] ProgramDifficulty? difficulty = null,
        [FromQuery] ContentStatus? status = null,
        [FromQuery] AccessLevel? visibility = null,
        [FromQuery] EnrollmentStatus? enrollmentStatus = null,
        [FromQuery] string? creatorId = null,
        [FromQuery] bool includeArchived = false,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] bool sortDescending = true)
    {
        var query = new GetAllProgramsQuery(
            skip, take, search, category, difficulty, status, visibility,
            enrollmentStatus, creatorId, includeArchived, sortBy, sortDescending
        );

        var programs = await mediator.Send(query);
        return Ok(programs);
    }

    /// <summary>
    /// Get program by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "id")]
    public async Task<ActionResult<Program>> GetProgram(
        Guid id,
        [FromQuery] bool includeContent = false,
        [FromQuery] bool includeEnrollments = false,
        [FromQuery] bool includeRatings = false)
    {
        var query = new GetProgramByIdQuery(id, includeContent, includeEnrollments, includeRatings);
        var program = await mediator.Send(query);

        if (program == null)
            return NotFound($"Program with ID {id} not found");

        return Ok(program);
    }

    /// <summary>
    /// Get program by slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    [Authorize]
    [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "slug")]
    public async Task<ActionResult<Program>> GetProgramBySlug(
        string slug,
        [FromQuery] bool includeContent = false,
        [FromQuery] bool includeEnrollments = false,
        [FromQuery] bool includeRatings = false)
    {
        var query = new GetProgramBySlugQuery(slug, includeContent, includeEnrollments, includeRatings);
        var program = await mediator.Send(query);

        if (program == null)
            return NotFound($"Program with slug '{slug}' not found");

        return Ok(program);
    }

    /// <summary>
    /// Get published program by slug (public access)
    /// </summary>
    [HttpGet("public/slug/{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<Program>> GetPublishedProgram(
        string slug,
        [FromQuery] bool includeContent = false)
    {
        var query = new GetPublishedProgramBySlugQuery(slug, includeContent);
        var program = await mediator.Send(query);

        if (program == null)
            return NotFound($"Published program with slug '{slug}' not found");

        return Ok(program);
    }

    /// <summary>
    /// Create a new program
    /// </summary>
    [HttpPost]
    [Authorize]
    [RequireTenantPermission(PermissionType.Create)]
    public async Task<ActionResult<Program>> CreateProgram([FromBody] CreateProgramRequest request)
    {
        var command = new CreateProgramCommand(
            request.Title,
            request.Description,
            request.Summary,
            request.Thumbnail,
            request.VideoShowcaseUrl,
            request.EstimatedHours,
            request.Category,
            request.Difficulty,
            request.EnrollmentStatus,
            request.MaxEnrollments,
            request.EnrollmentDeadline,
            UserId
        );

        var program = await mediator.Send(command);
        return CreatedAtAction(nameof(GetProgram), new { id = program.Id }, program);
    }

    /// <summary>
    /// Update an existing program
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Edit, "id")]
    public async Task<ActionResult<Program>> UpdateProgram(Guid id, [FromBody] UpdateProgramRequest request)
    {
        var command = new UpdateProgramCommand(
            id,
            request.Title,
            request.Description,
            request.Summary,
            request.Thumbnail,
            request.VideoShowcaseUrl,
            request.EstimatedHours,
            request.Category,
            request.Difficulty,
            request.EnrollmentStatus,
            request.MaxEnrollments,
            request.EnrollmentDeadline
        );

        try
        {
            var program = await mediator.Send(command);
            return Ok(program);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Delete a program
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Delete, "id")]
    public async Task<ActionResult> DeleteProgram(Guid id)
    {
        var command = new DeleteProgramCommand(id);
        var result = await mediator.Send(command);

        if (!result)
            return NotFound($"Program with ID {id} not found");

        return NoContent();
    }

    // ===== STATUS OPERATIONS =====

    /// <summary>
    /// Publish a program
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize]
    [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Edit, "id")]
    public async Task<ActionResult<Program>> PublishProgram(Guid id)
    {
        var command = new PublishProgramCommand(id);
        
        try
        {
            var program = await mediator.Send(command);
            return Ok(program);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Unpublish a program
    /// </summary>
    [HttpPost("{id:guid}/unpublish")]
    [Authorize]
    [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Edit, "id")]
    public async Task<ActionResult<Program>> UnpublishProgram(Guid id)
    {
        var command = new UnpublishProgramCommand(id);
        
        try
        {
            var program = await mediator.Send(command);
            return Ok(program);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Archive a program
    /// </summary>
    [HttpPost("{id:guid}/archive")]
    [Authorize]
    [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Edit, "id")]
    public async Task<ActionResult<Program>> ArchiveProgram(Guid id)
    {
        var command = new ArchiveProgramCommand(id);
        
        try
        {
            var program = await mediator.Send(command);
            return Ok(program);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Restore an archived program
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    [Authorize]
    [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Edit, "id")]
    public async Task<ActionResult<Program>> RestoreProgram(Guid id)
    {
        var command = new RestoreProgramCommand(id);
        
        try
        {
            var program = await mediator.Send(command);
            return Ok(program);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    // ===== SEARCH AND DISCOVERY =====

    /// <summary>
    /// Search programs (public access)
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Program>>> SearchPrograms(
        [FromQuery] string searchTerm,
        [FromQuery] ProgramCategory? category = null,
        [FromQuery] ProgramDifficulty? difficulty = null,
        [FromQuery] float? minEstimatedHours = null,
        [FromQuery] float? maxEstimatedHours = null,
        [FromQuery] decimal? minRating = null,
        [FromQuery] bool availableForEnrollment = false,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = new SearchProgramsQuery(
            searchTerm, category, difficulty, minEstimatedHours, maxEstimatedHours,
            minRating, availableForEnrollment, skip, take
        );

        var programs = await mediator.Send(query);
        return Ok(programs);
    }

    /// <summary>
    /// Get programs by category (public access)
    /// </summary>
    [HttpGet("category/{category}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Program>>> GetProgramsByCategory(
        ProgramCategory category,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] bool onlyPublished = true)
    {
        var query = new GetProgramsByCategoryQuery(category, skip, take, onlyPublished);
        var programs = await mediator.Send(query);
        return Ok(programs);
    }

    /// <summary>
    /// Get popular programs (public access)
    /// </summary>
    [HttpGet("popular")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Program>>> GetPopularPrograms(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10,
        [FromQuery] int daysBack = 30)
    {
        var query = new GetPopularProgramsQuery(skip, take, daysBack);
        var programs = await mediator.Send(query);
        return Ok(programs);
    }

    /// <summary>
    /// Get recent programs (public access)
    /// </summary>
    [HttpGet("recent")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Program>>> GetRecentPrograms(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10,
        [FromQuery] int daysBack = 7)
    {
        var query = new GetRecentProgramsQuery(skip, take, daysBack);
        var programs = await mediator.Send(query);
        return Ok(programs);
    }

    /// <summary>
    /// Get featured programs (public access)
    /// </summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Program>>> GetFeaturedPrograms(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10)
    {
        var query = new GetFeaturedProgramsQuery(skip, take);
        var programs = await mediator.Send(query);
        return Ok(programs);
    }

    // ===== ENROLLMENT OPERATIONS =====

    /// <summary>
    /// Enroll current user in a program
    /// </summary>
    [HttpPost("{id:guid}/enroll")]
    [Authorize]
    [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "id")]
    public async Task<ActionResult<ProgramUser>> EnrollInProgram(Guid id)
    {
        if (string.IsNullOrEmpty(UserId))
            return Unauthorized("User must be authenticated");

        var command = new EnrollUserCommand(id, UserId);
        
        try
        {
            var enrollment = await mediator.Send(command);
            return Ok(enrollment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Unenroll current user from a program
    /// </summary>
    [HttpPost("{id:guid}/unenroll")]
    [Authorize]
    [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "id")]
    public async Task<ActionResult> UnenrollFromProgram(Guid id)
    {
        if (string.IsNullOrEmpty(UserId))
            return Unauthorized("User must be authenticated");

        var command = new UnenrollUserCommand(id, UserId);
        var result = await mediator.Send(command);

        if (!result)
            return NotFound("User is not enrolled in this program");

        return NoContent();
    }

    /// <summary>
    /// Get current user's enrolled programs
    /// </summary>
    [HttpGet("my-enrollments")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Program>>> GetMyEnrolledPrograms(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] bool onlyActive = true)
    {
        if (string.IsNullOrEmpty(UserId))
            return Unauthorized("User must be authenticated");

        var query = new GetUserEnrolledProgramsQuery(UserId, skip, take, onlyActive);
        var programs = await mediator.Send(query);
        return Ok(programs);
    }

    /// <summary>
    /// Get program enrollments
    /// </summary>
    [HttpGet("{id:guid}/enrollments")]
    [Authorize]
    [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "id")]
    public async Task<ActionResult<IEnumerable<ProgramUser>>> GetProgramEnrollments(
        Guid id,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] bool onlyActive = true)
    {
        var query = new GetProgramEnrollmentsQuery(id, skip, take, onlyActive);
        var enrollments = await mediator.Send(query);
        return Ok(enrollments);
    }

    // ===== RATINGS =====

    /// <summary>
    /// Rate a program
    /// </summary>
    [HttpPost("{id:guid}/rate")]
    [Authorize]
    [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "id")]
    public async Task<ActionResult<ProgramRating>> RateProgram(Guid id, [FromBody] RateProgramRequest request)
    {
        if (string.IsNullOrEmpty(UserId))
            return Unauthorized("User must be authenticated");

        var command = new RateProgramCommand(id, UserId, request.Rating, request.Review);
        
        try
        {
            var rating = await mediator.Send(command);
            return Ok(rating);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get program ratings (public access)
    /// </summary>
    [HttpGet("{id:guid}/ratings")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProgramRating>>> GetProgramRatings(
        Guid id,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = new GetProgramRatingsQuery(id, skip, take);
        var ratings = await mediator.Send(query);
        return Ok(ratings);
    }

    // ===== WISHLIST =====

    /// <summary>
    /// Add program to wishlist
    /// </summary>
    [HttpPost("{id:guid}/wishlist")]
    [Authorize]
    public async Task<ActionResult<ProgramWishlist>> AddToWishlist(Guid id)
    {
        if (string.IsNullOrEmpty(UserId))
            return Unauthorized("User must be authenticated");

        var command = new AddToWishlistCommand(id, UserId);
        
        try
        {
            var wishlist = await mediator.Send(command);
            return Ok(wishlist);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Remove program from wishlist
    /// </summary>
    [HttpDelete("{id:guid}/wishlist")]
    [Authorize]
    public async Task<ActionResult> RemoveFromWishlist(Guid id)
    {
        if (string.IsNullOrEmpty(UserId))
            return Unauthorized("User must be authenticated");

        var command = new RemoveFromWishlistCommand(id, UserId);
        var result = await mediator.Send(command);

        if (!result)
            return NotFound("Program is not in user's wishlist");

        return NoContent();
    }

    /// <summary>
    /// Get current user's wishlist
    /// </summary>
    [HttpGet("my-wishlist")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Program>>> GetMyWishlist(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        if (string.IsNullOrEmpty(UserId))
            return Unauthorized("User must be authenticated");

        var query = new GetUserWishlistQuery(UserId, skip, take);
        var programs = await mediator.Send(query);
        return Ok(programs);
    }

    // ===== STATISTICS =====

    /// <summary>
    /// Get program statistics
    /// </summary>
    [HttpGet("{id:guid}/statistics")]
    [Authorize]
    [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "id")]
    public async Task<ActionResult<ProgramStatistics>> GetProgramStatistics(Guid id)
    {
        var query = new GetProgramStatisticsQuery(id);
        var statistics = await mediator.Send(query);
        return Ok(statistics);
    }

    /// <summary>
    /// Get global program statistics
    /// </summary>
    [HttpGet("statistics")]
    [Authorize]
    [RequireTenantPermission(PermissionType.Read)]
    public async Task<ActionResult<GlobalProgramStatistics>> GetGlobalStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var query = new GetGlobalProgramStatisticsQuery(fromDate, toDate);
        var statistics = await mediator.Send(query);
        return Ok(statistics);
    }
}

// ===== REQUEST DTOS =====

public record CreateProgramRequest(
    string Title,
    string Description,
    string? Summary = null,
    string? Thumbnail = null,
    string? VideoShowcaseUrl = null,
    float? EstimatedHours = null,
    ProgramCategory Category = ProgramCategory.Other,
    ProgramDifficulty Difficulty = ProgramDifficulty.Beginner,
    EnrollmentStatus EnrollmentStatus = EnrollmentStatus.Open,
    int? MaxEnrollments = null,
    DateTime? EnrollmentDeadline = null
);

public record UpdateProgramRequest(
    string? Title = null,
    string? Description = null,
    string? Summary = null,
    string? Thumbnail = null,
    string? VideoShowcaseUrl = null,
    float? EstimatedHours = null,
    ProgramCategory? Category = null,
    ProgramDifficulty? Difficulty = null,
    EnrollmentStatus? EnrollmentStatus = null,
    int? MaxEnrollments = null,
    DateTime? EnrollmentDeadline = null
);

public record RateProgramRequest(
    decimal Rating,
    string? Review = null
);
