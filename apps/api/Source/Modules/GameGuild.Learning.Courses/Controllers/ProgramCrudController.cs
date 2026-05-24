using Asp.Versioning;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameGuild.Learning.Courses;

/// <summary>
/// REST API controller for program CRUD operations, search, filtering, analytics, monetization, and product integration.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/courses")]
[Authorize]
public class ProgramCrudController(IProgramCrudService programService) : BaseApiController
{
  // ===== CONTENT-TYPE LEVEL OPERATIONS =====

  /// <summary> Get all courses with optional filtering (content-type level read permission) </summary>
  [HttpGet]
  [RequireContentTypePermission<Program>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProgramDto>>> GetPrograms(
      [FromQuery] string? status = null,
      [FromQuery] ProgramCategory? category = null,
      [FromQuery] ProgramDifficulty? difficulty = null,
      [FromQuery] Guid? creatorId = null,
      [FromQuery] string? q = null,
      [FromQuery] string? sort = null,
      [FromQuery] int skip = 0,
      [FromQuery] int take = 50)
  {
    if (!string.IsNullOrEmpty(q))
    {
      var searchResults = await programService.SearchProgramsAsync(q, skip, take).ConfigureAwait(false);
      return Ok(searchResults.ToDtos());
    }

    if (status == "published")
    {
      var publishedPrograms = await programService.GetPublishedProgramsAsync(skip, take).ConfigureAwait(false);
      return Ok(publishedPrograms.ToDtos());
    }

    if (category.HasValue)
    {
      var categoryPrograms = await programService.GetProgramsByCategoryAsync(category.Value, skip, take).ConfigureAwait(false);
      return Ok(categoryPrograms.ToDtos());
    }

    if (difficulty.HasValue)
    {
      var difficultyPrograms = await programService.GetProgramsByDifficultyAsync(difficulty.Value, skip, take).ConfigureAwait(false);
      return Ok(difficultyPrograms.ToDtos());
    }

    if (creatorId.HasValue)
    {
      var creatorPrograms = await programService.GetProgramsByCreatorAsync(creatorId.Value, skip, take).ConfigureAwait(false);
      return Ok(creatorPrograms.ToDtos());
    }

    if (sort == "popular")
    {
      var popularPrograms = await programService.GetPopularProgramsAsync(take).ConfigureAwait(false);
      return Ok(popularPrograms.ToDtos());
    }

    if (sort == "recent")
    {
      var recentPrograms = await programService.GetRecentProgramsAsync(take).ConfigureAwait(false);
      return Ok(recentPrograms.ToDtos());
    }

    var programs = await programService.GetProgramsAsync(skip, take).ConfigureAwait(false);
    return Ok(programs.ToDtos());
  }

  /// <summary> Get published public courses for the public catalog. </summary>
  [HttpGet("public")]
  [AllowAnonymous]
  public async Task<ActionResult<IEnumerable<ProgramDto>>> GetPublicPrograms(
      [FromQuery] int skip = 0,
      [FromQuery] int take = 50)
  {
    var programs = await programService.GetPublicPublishedProgramsAsync(skip, take).ConfigureAwait(false);
    return Ok(programs.ToDtos());
  }

  /// <summary> Create a new program (content-type level draft permission) </summary>
  [HttpPost]
  [RequireContentTypePermission<Program>(PermissionType.Draft)]
  public async Task<ActionResult<ProgramDto>> CreateProgram([FromBody] CreateProgramDto createDto)
  {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var currentUserId = GetCurrentUserId();
    if (!currentUserId.HasValue) return Unauthorized();

    var program = await programService.CreateProgramAsync(createDto with { CreatorId = currentUserId.Value }).ConfigureAwait(false);

    return CreatedAtAction(nameof(GetProgram), new { id = program.Id }, program.ToDto());
  }

  // ===== RESOURCE-LEVEL OPERATIONS =====

  /// <summary> Get a specific program by ID (resource-level read permission) </summary>
  [HttpGet("{id}")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Read)]
  public async Task<ActionResult<ProgramDto>> GetProgram(Guid id)
  {
    var program = await programService.GetProgramByIdAsync(id).ConfigureAwait(false);

    if (program == null) return NotFound();

    return Ok(program.ToDto());
  }

  /// <summary> Get a specific program with all content included (resource-level read permission) </summary>
  [HttpGet("{id}/with-content")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Read)]
  public async Task<ActionResult<ProgramDto>> GetProgramWithContent(Guid id)
  {
    var program = await programService.GetProgramWithContentAsync(id).ConfigureAwait(false);

    if (program == null) return NotFound();

    return Ok(program.ToDto());
  }

  /// <summary> Update a program (resource-level edit permission) </summary>
  [HttpPut("{id}")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult<ProgramDto>> UpdateProgram(Guid id, [FromBody] UpdateProgramDto updateDto)
  {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var program = await programService.UpdateProgramAsync(id, updateDto).ConfigureAwait(false);

    if (program == null) return NotFound();

    return Ok(program.ToDto());
  }

  /// <summary> Delete a program (resource-level delete permission) </summary>
  [HttpDelete("{id}")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Delete)]
  public async Task<ActionResult> DeleteProgram(Guid id)
  {
    var existingProgram = await programService.GetProgramByIdAsync(id).ConfigureAwait(false);

    if (existingProgram == null) return NotFound();

    await programService.DeleteProgramAsync(id).ConfigureAwait(false);

    return NoContent();
  }

  /// <summary> Clone/duplicate a program (resource-level clone permission) </summary>
  [HttpPost("{id}:clone")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Clone)]
  public async Task<ActionResult<ProgramDto>> CloneProgram(Guid id, [FromBody] CloneProgramDto cloneDto)
  {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var program = await programService.CloneProgramAsync(id, cloneDto.NewTitle).ConfigureAwait(false);

    if (program == null) return NotFound();

    return CreatedAtAction(nameof(GetProgram), new { id = program.Id }, program.ToDto());
  }

  /// <summary> Get a specific program by slug (public access for published programs) </summary>
  [HttpGet("slug/{slug}")]
  [AllowAnonymous]
  public async Task<ActionResult<ProgramDto>> GetProgramBySlug(string slug)
  {
    var isAuthenticated = HttpContext.User.Identity?.IsAuthenticated == true;

    Program? program;

    if (isAuthenticated)
    {
      program = await programService.GetProgramBySlugAsync(slug).ConfigureAwait(false);
    }
    else
    {
      program = await programService.GetPublishedProgramBySlugAsync(slug).ConfigureAwait(false);
    }

    if (program == null) return NotFound();

    return Ok(program.ToDto());
  }

  /// <summary> Self-enroll the current authenticated user in a published public course. </summary>
  [HttpPost("{id}:self-enroll")]
  public async Task<ActionResult<UserProgressDto>> SelfEnroll(Guid id)
  {
    var userId = GetCurrentUserId();
    if (!userId.HasValue) return Unauthorized();

    var program = await programService.GetProgramByIdAsync(id).ConfigureAwait(false);

    if (program == null || program.Status != ContentStatus.Published || program.Visibility != ContentVisibility.Public)
    {
      return NotFound();
    }

    if (!program.IsEnrollmentOpen)
    {
      return Conflict(new ProblemDetails
      {
        Title = "Enrollment closed",
        Detail = "This course is not currently open for self-enrollment."
      });
    }

    var progress = await programService.AddUserToProgramAsync(id, userId.Value).ConfigureAwait(false);

    if (progress == null) return NotFound();

    return Ok(progress);
  }

  // ===== CONTENT MANAGEMENT ENDPOINTS =====
  // NOTE: POST/PUT/DELETE for content are in ProgramContentController to avoid route conflicts.
  // Only reorder endpoint is kept here since it uses a unique action-style route.

  /// <summary> Reorder content in a program (resource-level edit permission) </summary>
  [HttpPost("{id}/content:reorder")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult> ReorderContent(Guid id, [FromBody] ReorderContentDto reorderDto)
  {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var program = await programService.ReorderContentAsync(id, reorderDto.ContentIds).ConfigureAwait(false);

    if (program == null) return NotFound();

    return NoContent();
  }

  // ===== USER PARTICIPATION ENDPOINTS =====

  /// <summary> Add a user to a program (resource-level edit permission) </summary>
  [HttpPost("{id}/users/{userId}")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult<UserProgressDto>> AddUserToProgram(Guid id, Guid userId)
  {
    var progress = await programService.AddUserToProgramAsync(id, userId).ConfigureAwait(false);

    if (progress == null) return NotFound();

    return Ok(progress);
  }

  /// <summary> Remove a user from a program (resource-level edit permission) </summary>
  [HttpDelete("{id}/users/{userId}")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult> RemoveUserFromProgram(Guid id, Guid userId)
  {
    var success = await programService.RemoveUserFromProgramAsync(id, userId).ConfigureAwait(false);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary> Get all users in a program (resource-level read permission) </summary>
  [HttpGet("{id}/users")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<UserProgressDto>>> GetProgramUsers(Guid id, [FromQuery] int skip = 0, [FromQuery] int take = 50)
  {
    var users = await programService.GetProgramUsersAsync(id, skip, take).ConfigureAwait(false);

    return Ok(users);
  }

  /// <summary> Get a specific user's progress in a program (resource-level read permission) </summary>
  [HttpGet("{id}/users/{userId}/progress")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Read)]
  public async Task<ActionResult<UserProgressDto>> GetUserProgress(Guid id, Guid userId)
  {
    var progress = await programService.GetUserProgressDtoAsync(id, userId).ConfigureAwait(false);

    if (progress == null) return NotFound();

    return Ok(progress);
  }

  /// <summary> Get the current learner's progress in a program. </summary>
  [HttpGet("{id}/me/progress")]
  public async Task<ActionResult<UserProgressDto>> GetMyProgress(Guid id)
  {
    var currentUserId = GetCurrentUserId();
    if (currentUserId == null) return Unauthorized();

    var progress = await programService.GetUserProgressDtoAsync(id, currentUserId.Value).ConfigureAwait(false);

    if (progress == null) return NotFound();

    return Ok(progress);
  }

  /// <summary> Update a user's progress in a program (resource-level edit permission) </summary>
  [HttpPut("{id}/users/{userId}/progress")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult<UserProgressDto>> UpdateUserProgress(Guid id, Guid userId, [FromBody] UpdateProgressDto progressDto)
  {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var progress = await programService.UpdateUserProgressAsync(id, userId, progressDto).ConfigureAwait(false);

    if (progress == null) return NotFound();

    return Ok(progress);
  }

  /// <summary> Update the current learner's progress in a program. </summary>
  [HttpPut("{id}/me/progress")]
  public async Task<ActionResult<UserProgressDto>> UpdateMyProgress(Guid id, [FromBody] UpdateProgressDto progressDto)
  {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var currentUserId = GetCurrentUserId();
    if (currentUserId == null) return Unauthorized();

    var progress = await programService.UpdateUserProgressAsync(id, currentUserId.Value, progressDto).ConfigureAwait(false);

    if (progress == null) return NotFound();

    return Ok(progress);
  }

  /// <summary> Mark content as completed for a user (resource-level edit permission) </summary>
  [HttpPost("{id}/users/{userId}/content/{contentId}:complete")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult> MarkContentCompleted(Guid id, Guid userId, Guid contentId)
  {
    var success = await programService.MarkContentCompletedAsync(id, userId, contentId).ConfigureAwait(false);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary> Mark content as completed for the current learner. </summary>
  [HttpPost("{id}/me/content/{contentId}:complete")]
  public async Task<ActionResult> MarkMyContentCompleted(Guid id, Guid contentId)
  {
    var currentUserId = GetCurrentUserId();
    if (currentUserId == null) return Unauthorized();

    var success = await programService.MarkContentCompletedAsync(id, currentUserId.Value, contentId).ConfigureAwait(false);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary> Reset user progress in a program (resource-level edit permission) </summary>
  [HttpPost("{id}/users/{userId}:reset")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult> ResetUserProgress(Guid id, Guid userId)
  {
    var success = await programService.ResetUserProgressAsync(id, userId).ConfigureAwait(false);

    if (!success) return NotFound();

    return NoContent();
  }

  private Guid? GetCurrentUserId()
  {
    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirst("sub")?.Value
        ?? User.FindFirst("userId")?.Value;

    return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
  }

  // ===== MONETIZATION ENDPOINTS =====

  /// <summary> Enable monetization for a program (resource-level monetize permission) </summary>
  [HttpPost("{id}:monetize")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult<ProgramDto>> EnableMonetization(Guid id, [FromBody] MonetizationDto monetizationDto)
  {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var program = await programService.EnableMonetizationAsync(id, monetizationDto).ConfigureAwait(false);

    if (program == null) return NotFound();

    return Ok(program.ToDto());
  }

  /// <summary> Disable monetization for a program (resource-level monetize permission) </summary>
  [HttpPost("{id}:disable-monetization")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult<ProgramDto>> DisableMonetization(Guid id)
  {
    var program = await programService.DisableMonetizationAsync(id).ConfigureAwait(false);

    if (program == null) return NotFound();

    return Ok(program.ToDto());
  }

  /// <summary> Get program pricing information (resource-level read permission) </summary>
  [HttpGet("{id}/pricing")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Read)]
  public async Task<ActionResult<PricingDto>> GetProgramPricing(Guid id)
  {
    var pricing = await programService.GetProgramPricingAsync(id).ConfigureAwait(false);

    if (pricing == null) return NotFound();

    return Ok(pricing);
  }

  /// <summary> Update program pricing (resource-level pricing permission) </summary>
  [HttpPut("{id}/pricing")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult<PricingDto>> UpdateProgramPricing(Guid id, [FromBody] UpdatePricingDto pricingDto)
  {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var pricing = await programService.UpdateProgramPricingAsync(id, pricingDto).ConfigureAwait(false);

    if (pricing == null) return NotFound();

    return Ok(pricing);
  }

  // ===== ANALYTICS ENDPOINTS =====

  /// <summary> Get program analytics (resource-level analytics permission) </summary>
  [HttpGet("{id}/analytics")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Analytics)]
  public async Task<ActionResult<ProgramAnalyticsDto>> GetProgramAnalytics(Guid id)
  {
    var analytics = await programService.GetProgramAnalyticsAsync(id).ConfigureAwait(false);

    if (analytics == null) return NotFound();

    return Ok(analytics);
  }

  /// <summary> Get user completion rates for a program (resource-level analytics permission) </summary>
  [HttpGet("{id}/analytics/completion-rates")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Analytics)]
  public async Task<ActionResult<CompletionRatesDto>> GetCompletionRates(Guid id)
  {
    var rates = await programService.GetCompletionRatesAsync(id).ConfigureAwait(false);

    if (rates == null) return NotFound();

    return Ok(rates);
  }

  /// <summary> Get program engagement metrics (resource-level analytics permission) </summary>
  [HttpGet("{id}/analytics/engagement")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Analytics)]
  public async Task<ActionResult<EngagementMetricsDto>> GetEngagementMetrics(Guid id)
  {
    var metrics = await programService.GetEngagementMetricsAsync(id).ConfigureAwait(false);

    if (metrics == null) return NotFound();

    return Ok(metrics);
  }

  /// <summary> Get program revenue analytics (resource-level revenue permission) </summary>
  [HttpGet("{id}/analytics/revenue")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Read)]
  public async Task<ActionResult<RevenueAnalyticsDto>> GetRevenueAnalytics(Guid id)
  {
    var revenue = await programService.GetRevenueAnalyticsAsync(id).ConfigureAwait(false);

    if (revenue == null) return NotFound();

    return Ok(revenue);
  }

  // ===== PRODUCT INTEGRATION ENDPOINTS =====

  /// <summary> Create a product from a program (resource-level edit permission for program, content-type level draft permission for product) </summary>
  [HttpPost("{id}:create-product")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult<Guid>> CreateProductFromProgram(Guid id, [FromBody] CreateProductFromProgramDto productDto)
  {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var productId = await programService.CreateProductFromProgramAsync(id, productDto).ConfigureAwait(false);

    if (productId == null) return NotFound();

    return Ok(new { ProductId = productId });
  }

  /// <summary> Link a program to an existing product (resource-level edit permission) </summary>
  [HttpPost("{id}:link-product/{productId}")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult> LinkProgramToProduct(Guid id, Guid productId)
  {
    var success = await programService.LinkProgramToProductAsync(id, productId).ConfigureAwait(false);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary> Unlink a program from a product (resource-level edit permission) </summary>
  [HttpDelete("{id}:unlink-product/{productId}")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult> UnlinkProgramFromProduct(Guid id, Guid productId)
  {
    var success = await programService.UnlinkProgramFromProductAsync(id, productId).ConfigureAwait(false);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary> Get all products linked to a program (resource-level read permission) </summary>
  [HttpGet("{id}/products")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<Guid>>> GetLinkedProducts(Guid id)
  {
    var productIds = await programService.GetLinkedProductsAsync(id).ConfigureAwait(false);

    return Ok(productIds);
  }
}
