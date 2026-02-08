using Asp.Versioning;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Learning.Courses;

/// <summary>
/// REST API controller for program CRUD operations, search, filtering, analytics, monetization, and product integration.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/courses")]
[Authorize]
public class ProgramCrudController(IProgramCrudService programService) : BaseApiController {
  // ===== CONTENT-TYPE LEVEL OPERATIONS =====

  /// <summary> Get all courses with optional filtering (content-type level read permission) </summary>
  [HttpGet]
  [RequireContentTypePermission<Program>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<Program>>> GetPrograms(
      [FromQuery] string? status = null,
      [FromQuery] ProgramCategory? category = null,
      [FromQuery] ProgramDifficulty? difficulty = null,
      [FromQuery] Guid? creatorId = null,
      [FromQuery] string? q = null,
      [FromQuery] string? sort = null,
      [FromQuery] int skip = 0,
      [FromQuery] int take = 50) {
    if (!string.IsNullOrEmpty(q)) {
      var searchResults = await programService.SearchProgramsAsync(q, skip, take);
      return Ok(searchResults);
    }

    if (status == "published") {
      var publishedPrograms = await programService.GetPublishedProgramsAsync(skip, take);
      return Ok(publishedPrograms);
    }

    if (category.HasValue) {
      var categoryPrograms = await programService.GetProgramsByCategoryAsync(category.Value, skip, take);
      return Ok(categoryPrograms);
    }

    if (difficulty.HasValue) {
      var difficultyPrograms = await programService.GetProgramsByDifficultyAsync(difficulty.Value, skip, take);
      return Ok(difficultyPrograms);
    }

    if (creatorId.HasValue) {
      var creatorPrograms = await programService.GetProgramsByCreatorAsync(creatorId.Value, skip, take);
      return Ok(creatorPrograms);
    }

    if (sort == "popular") {
      var popularPrograms = await programService.GetPopularProgramsAsync(take);
      return Ok(popularPrograms);
    }

    if (sort == "recent") {
      var recentPrograms = await programService.GetRecentProgramsAsync(take);
      return Ok(recentPrograms);
    }

    var programs = await programService.GetProgramsAsync(skip, take);
    return Ok(programs);
  }

  /// <summary> Create a new program (content-type level draft permission) </summary>
  [HttpPost]
  [RequireContentTypePermission<Program>(PermissionType.Draft)]
  public async Task<ActionResult<Program>> CreateProgram([FromBody] CreateProgramDto createDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var program = await programService.CreateProgramAsync(createDto);

    return CreatedAtAction(nameof(GetProgram), new { id = program.Id }, program);
  }

  // ===== RESOURCE-LEVEL OPERATIONS =====

  /// <summary> Get a specific program by ID (resource-level read permission) </summary>
  [HttpGet("{id}")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Read)]
  public async Task<ActionResult<Program>> GetProgram(Guid id) {
    var program = await programService.GetProgramByIdAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary> Get a specific program with all content included (resource-level read permission) </summary>
  [HttpGet("{id}/with-content")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Read)]
  public async Task<ActionResult<Program>> GetProgramWithContent(Guid id) {
    var program = await programService.GetProgramWithContentAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary> Update a program (resource-level edit permission) </summary>
  [HttpPut("{id}")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult<Program>> UpdateProgram(Guid id, [FromBody] UpdateProgramDto updateDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var program = await programService.UpdateProgramAsync(id, updateDto);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary> Delete a program (resource-level delete permission) </summary>
  [HttpDelete("{id}")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Delete)]
  public async Task<ActionResult> DeleteProgram(Guid id) {
    var existingProgram = await programService.GetProgramByIdAsync(id);

    if (existingProgram == null) return NotFound();

    await programService.DeleteProgramAsync(id);

    return NoContent();
  }

  /// <summary> Clone/duplicate a program (resource-level clone permission) </summary>
  [HttpPost("{id}:clone")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Clone)]
  public async Task<ActionResult<Program>> CloneProgram(Guid id, [FromBody] CloneProgramDto cloneDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var program = await programService.CloneProgramAsync(id, cloneDto.NewTitle);

    if (program == null) return NotFound();

    return CreatedAtAction(nameof(GetProgram), new { id = program.Id }, program);
  }

  /// <summary> Get a specific program by slug (public access for published programs) </summary>
  [HttpGet("slug/{slug}")]
  public async Task<ActionResult<Program>> GetProgramBySlug(string slug) {
    var isAuthenticated = HttpContext.User.Identity?.IsAuthenticated == true;

    Program? program;

    if (isAuthenticated) {
      program = await programService.GetProgramBySlugAsync(slug);
    }
    else {
      program = await programService.GetProgramBySlugAsync(slug);

      if (program != null) {
        if (program.Status != ContentStatus.Published || program.Visibility != ContentVisibility.Public) { return Unauthorized("Authentication required to access this program"); }
      }
    }

    if (program == null) return NotFound();

    return Ok(program);
  }

  // ===== CONTENT MANAGEMENT ENDPOINTS =====

  /// <summary> Add content to a program (resource-level edit permission) </summary>
  [HttpPost("{id}/content")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult<ProgramContent>> AddContent(Guid id, [FromBody] CreateContentDto contentDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var content = await programService.AddContentAsync(id, contentDto);

    if (content == null) return NotFound("Program not found");

    return Ok(content);
  }

  /// <summary> Update program content (resource-level edit permission) </summary>
  [HttpPut("{id}/content/{contentId}")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult<ProgramContent>> UpdateContent(Guid id, Guid contentId, [FromBody] UpdateContentDto contentDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var content = await programService.UpdateContentAsync(id, contentId, contentDto);

    if (content == null) return NotFound();

    return Ok(content);
  }

  /// <summary> Remove content from a program (resource-level edit permission) </summary>
  [HttpDelete("{id}/content/{contentId}")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult> RemoveContent(Guid id, Guid contentId) {
    var success = await programService.RemoveContentAsync(id, contentId);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary> Reorder content in a program (resource-level edit permission) </summary>
  [HttpPost("{id}/content:reorder")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult> ReorderContent(Guid id, [FromBody] ReorderContentDto reorderDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var program = await programService.ReorderContentAsync(id, reorderDto.ContentIds);

    if (program == null) return NotFound();

    return NoContent();
  }

  // ===== USER PARTICIPATION ENDPOINTS =====

  /// <summary> Add a user to a program (resource-level edit permission) </summary>
  [HttpPost("{id}/users/{userId}")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult<UserProgressDto>> AddUserToProgram(Guid id, Guid userId) {
    var progress = await programService.AddUserToProgramAsync(id, userId);

    if (progress == null) return NotFound();

    return Ok(progress);
  }

  /// <summary> Remove a user from a program (resource-level edit permission) </summary>
  [HttpDelete("{id}/users/{userId}")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult> RemoveUserFromProgram(Guid id, Guid userId) {
    var success = await programService.RemoveUserFromProgramAsync(id, userId);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary> Get all users in a program (resource-level read permission) </summary>
  [HttpGet("{id}/users")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<UserProgressDto>>> GetProgramUsers(Guid id, [FromQuery] int skip = 0, [FromQuery] int take = 50) {
    var users = await programService.GetProgramUsersAsync(id, skip, take);

    return Ok(users);
  }

  /// <summary> Get a specific user's progress in a program (resource-level read permission) </summary>
  [HttpGet("{id}/users/{userId}/progress")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Read)]
  public async Task<ActionResult<UserProgressDto>> GetUserProgress(Guid id, Guid userId) {
    var progress = await programService.GetUserProgressDtoAsync(id, userId);

    if (progress == null) return NotFound();

    return Ok(progress);
  }

  /// <summary> Update a user's progress in a program (resource-level edit permission) </summary>
  [HttpPut("{id}/users/{userId}/progress")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult<UserProgressDto>> UpdateUserProgress(Guid id, Guid userId, [FromBody] UpdateProgressDto progressDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var progress = await programService.UpdateUserProgressAsync(id, userId, progressDto);

    if (progress == null) return NotFound();

    return Ok(progress);
  }

  /// <summary> Mark content as completed for a user (resource-level edit permission) </summary>
  [HttpPost("{id}/users/{userId}/content/{contentId}:complete")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult> MarkContentCompleted(Guid id, Guid userId, Guid contentId) {
    var success = await programService.MarkContentCompletedAsync(id, userId, contentId);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary> Reset user progress in a program (resource-level edit permission) </summary>
  [HttpPost("{id}/users/{userId}:reset")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult> ResetUserProgress(Guid id, Guid userId) {
    var success = await programService.ResetUserProgressAsync(id, userId);

    if (!success) return NotFound();

    return NoContent();
  }

  // ===== MONETIZATION ENDPOINTS =====

  /// <summary> Enable monetization for a program (resource-level monetize permission) </summary>
  [HttpPost("{id}:monetize")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult<Program>> EnableMonetization(Guid id, [FromBody] MonetizationDto monetizationDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var program = await programService.EnableMonetizationAsync(id, monetizationDto);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary> Disable monetization for a program (resource-level monetize permission) </summary>
  [HttpPost("{id}:disable-monetization")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult<Program>> DisableMonetization(Guid id) {
    var program = await programService.DisableMonetizationAsync(id);

    if (program == null) return NotFound();

    return Ok(program);
  }

  /// <summary> Get program pricing information (resource-level read permission) </summary>
  [HttpGet("{id}/pricing")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Read)]
  public async Task<ActionResult<PricingDto>> GetProgramPricing(Guid id) {
    var pricing = await programService.GetProgramPricingAsync(id);

    if (pricing == null) return NotFound();

    return Ok(pricing);
  }

  /// <summary> Update program pricing (resource-level pricing permission) </summary>
  [HttpPut("{id}/pricing")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult<PricingDto>> UpdateProgramPricing(Guid id, [FromBody] UpdatePricingDto pricingDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var pricing = await programService.UpdateProgramPricingAsync(id, pricingDto);

    if (pricing == null) return NotFound();

    return Ok(pricing);
  }

  // ===== ANALYTICS ENDPOINTS =====

  /// <summary> Get program analytics (resource-level analytics permission) </summary>
  [HttpGet("{id}/analytics")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Analytics)]
  public async Task<ActionResult<ProgramAnalyticsDto>> GetProgramAnalytics(Guid id) {
    var analytics = await programService.GetProgramAnalyticsAsync(id);

    if (analytics == null) return NotFound();

    return Ok(analytics);
  }

  /// <summary> Get user completion rates for a program (resource-level analytics permission) </summary>
  [HttpGet("{id}/analytics/completion-rates")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Analytics)]
  public async Task<ActionResult<CompletionRatesDto>> GetCompletionRates(Guid id) {
    var rates = await programService.GetCompletionRatesAsync(id);

    if (rates == null) return NotFound();

    return Ok(rates);
  }

  /// <summary> Get program engagement metrics (resource-level analytics permission) </summary>
  [HttpGet("{id}/analytics/engagement")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Analytics)]
  public async Task<ActionResult<EngagementMetricsDto>> GetEngagementMetrics(Guid id) {
    var metrics = await programService.GetEngagementMetricsAsync(id);

    if (metrics == null) return NotFound();

    return Ok(metrics);
  }

  /// <summary> Get program revenue analytics (resource-level revenue permission) </summary>
  [HttpGet("{id}/analytics/revenue")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Read)]
  public async Task<ActionResult<RevenueAnalyticsDto>> GetRevenueAnalytics(Guid id) {
    var revenue = await programService.GetRevenueAnalyticsAsync(id);

    if (revenue == null) return NotFound();

    return Ok(revenue);
  }

  // ===== PRODUCT INTEGRATION ENDPOINTS =====

  /// <summary> Create a product from a program (resource-level edit permission for program, content-type level draft permission for product) </summary>
  [HttpPost("{id}:create-product")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult<Guid>> CreateProductFromProgram(Guid id, [FromBody] CreateProductFromProgramDto productDto) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var productId = await programService.CreateProductFromProgramAsync(id, productDto);

    if (productId == null) return NotFound();

    return Ok(new { ProductId = productId });
  }

  /// <summary> Link a program to an existing product (resource-level edit permission) </summary>
  [HttpPost("{id}:link-product/{productId}")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult> LinkProgramToProduct(Guid id, Guid productId) {
    var success = await programService.LinkProgramToProductAsync(id, productId);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary> Unlink a program from a product (resource-level edit permission) </summary>
  [HttpDelete("{id}:unlink-product/{productId}")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit)]
  public async Task<ActionResult> UnlinkProgramFromProduct(Guid id, Guid productId) {
    var success = await programService.UnlinkProgramFromProductAsync(id, productId);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary> Get all products linked to a program (resource-level read permission) </summary>
  [HttpGet("{id}/products")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<Guid>>> GetLinkedProducts(Guid id) {
    var productIds = await programService.GetLinkedProductsAsync(id);

    return Ok(productIds);
  }
}
